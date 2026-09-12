using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Npgsql;

// One-off migration: copy media that used to live on local disk (the backend's
// Resources/ folder) into the S3 media bucket, and rewrite each Files row's
// FilePath from a local path to the corresponding S3 object key.
//
// Safe to re-run: objects already in S3 are not re-uploaded, and rows already
// pointing at a key are left alone. Use --dry-run first to preview.
//
// Usage:
//   dotnet run --project tools/MediaMigration -- \
//       --connection "Host=...;Database=...;Username=...;Password=..." \
//       --bucket mesmer-media-dev-<acct>-<region> \
//       [--resources-dir /path/to/backend/Resources] \
//       [--region eu-north-1] [--dry-run]
//
// Connection string and bucket also fall back to the env vars
// ConnectionStrings__SongAppApiDatabase and MEDIA_BUCKET_NAME. AWS credentials
// are resolved from the standard chain (env vars, shared profile, SSO, role).

string? GetArg(string name)
{
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == name) return args[i + 1];
    return null;
}
bool HasFlag(string name) => Array.IndexOf(args, name) >= 0;

var connectionString = GetArg("--connection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__SongAppApiDatabase");
var bucket = GetArg("--bucket")
    ?? Environment.GetEnvironmentVariable("MEDIA_BUCKET_NAME");
var resourcesDir = GetArg("--resources-dir");
var region = GetArg("--region") ?? Environment.GetEnvironmentVariable("AWS_REGION");
var dryRun = HasFlag("--dry-run");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        "Missing DB connection. Pass --connection or set ConnectionStrings__SongAppApiDatabase.");
    return 1;
}
if (string.IsNullOrWhiteSpace(bucket))
{
    Console.Error.WriteLine(
        "Missing bucket. Pass --bucket or set MEDIA_BUCKET_NAME.");
    return 1;
}

Console.WriteLine($"Media migration{(dryRun ? " (DRY RUN)" : "")}");
Console.WriteLine($"  bucket        : {bucket}");
Console.WriteLine($"  resources-dir : {resourcesDir ?? "(use stored paths as-is)"}");
Console.WriteLine();

IAmazonS3 s3 = region is null
    ? new AmazonS3Client()
    : new AmazonS3Client(RegionEndpoint.GetBySystemName(region));

// Read all file rows up front so the DB connection isn't held open during uploads.
var rows = new List<(Guid Id, string FileName, string FilePath)>();
await using (var conn = new NpgsqlConnection(connectionString))
{
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand(
        "SELECT \"Id\", \"FileName\", \"FilePath\" FROM \"Files\"", conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        rows.Add((reader.GetGuid(0), reader.GetString(1), reader.GetString(2)));
}

Console.WriteLine($"Found {rows.Count} file row(s).");

int migrated = 0, updatedOnly = 0, alreadyPresent = 0, missing = 0, errors = 0;

foreach (var row in rows)
{
    try
    {
        var key = ToObjectKey(row.FilePath);
        var localFile = ResolveLocalFile(row.FilePath, key, resourcesDir);
        var existsInS3 = await ObjectExistsAsync(s3, bucket!, key);

        if (existsInS3)
        {
            // Object is already in S3: only the DB pointer might need updating.
            if (row.FilePath != key)
            {
                Console.WriteLine($"  [update]  {row.Id}  {row.FilePath} -> {key}");
                if (!dryRun) await UpdateKeyAsync(connectionString!, row.Id, key);
                updatedOnly++;
            }
            else
            {
                alreadyPresent++;
            }
            continue;
        }

        if (localFile is null)
        {
            Console.Error.WriteLine(
                $"  [MISSING] {row.Id}  no local source and nothing in S3: {row.FilePath}");
            missing++;
            continue;
        }

        Console.WriteLine($"  [migrate] {row.Id}  {localFile} -> s3://{bucket}/{key}");
        if (!dryRun)
        {
            await UploadAsync(s3, bucket!, key, localFile);
            await UpdateKeyAsync(connectionString!, row.Id, key);
        }
        migrated++;
    }
    catch (Exception ex)
    {
        errors++;
        Console.Error.WriteLine($"  [ERROR]   {row.Id}  {row.FilePath}: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine(
    $"Done. migrated={migrated}, key-updated={updatedOnly}, " +
    $"already-present={alreadyPresent}, missing={missing}, errors={errors}");
return errors == 0 ? 0 : 2;

// --- helpers ---

// Derive the S3 object key from a stored path: everything after the last
// "Resources/" segment (so "/app/Resources/Songs/Audio/x.mp3" -> "Songs/Audio/x.mp3").
// Paths without that segment fall back to a "Migrated/<filename>" key.
static string ToObjectKey(string storedPath)
{
    var normalized = storedPath.Replace('\\', '/');
    const string marker = "Resources/";
    var idx = normalized.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
    var key = idx >= 0
        ? normalized[(idx + marker.Length)..]
        : $"Migrated/{Path.GetFileName(normalized)}";
    return key.TrimStart('/');
}

// Find the file on disk: the stored path if it still exists, otherwise under
// --resources-dir joined with the derived key.
static string? ResolveLocalFile(string storedPath, string key, string? resourcesDir)
{
    if (File.Exists(storedPath)) return storedPath;
    if (resourcesDir is not null)
    {
        var candidate = Path.Combine(
            resourcesDir, key.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(candidate)) return candidate;
    }
    return null;
}

static async Task<bool> ObjectExistsAsync(IAmazonS3 s3, string bucket, string key)
{
    try
    {
        await s3.GetObjectMetadataAsync(bucket, key);
        return true;
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return false;
    }
}

static async Task UploadAsync(IAmazonS3 s3, string bucket, string key, string localFile)
{
    await using var stream = File.OpenRead(localFile);
    await s3.PutObjectAsync(new PutObjectRequest
    {
        BucketName = bucket,
        Key = key,
        InputStream = stream,
        ContentType = GuessContentType(localFile),
    });
}

static async Task UpdateKeyAsync(string connectionString, Guid id, string key)
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand(
        "UPDATE \"Files\" SET \"FilePath\" = @key WHERE \"Id\" = @id", conn);
    cmd.Parameters.AddWithValue("key", key);
    cmd.Parameters.AddWithValue("id", id);
    await cmd.ExecuteNonQueryAsync();
}

static string GuessContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
{
    ".jpg" or ".jpeg" => "image/jpeg",
    ".png" => "image/png",
    ".gif" => "image/gif",
    ".webp" => "image/webp",
    ".mp3" => "audio/mpeg",
    ".wav" => "audio/wav",
    ".flac" => "audio/flac",
    ".m4a" => "audio/mp4",
    ".ogg" => "audio/ogg",
    ".mp4" => "video/mp4",
    ".webm" => "video/webm",
    ".mov" => "video/quicktime",
    _ => "application/octet-stream",
};
