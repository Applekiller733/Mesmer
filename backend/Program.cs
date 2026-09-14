using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SongAppApi.Authorization;
using SongAppApi.Helpers;
using SongAppApi.Services;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.AspNetCore.Http.Features;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using Amazon.S3;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
{
    var services = builder.Services;
    var env = builder.Environment;

    // In AWS, pull the JWT signing secret + SMTP credentials from Secrets Manager
    // into configuration before AppSettings is bound. No-op locally.
    builder.Configuration.AddAppSecrets();

    // Run as a Lambda behind an API Gateway HTTP API when hosted in Lambda,
    // and as normal Kestrel locally. The adapter is a no-op outside Lambda,
    // so `dotnet run` behaviour is unchanged.
    services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

    // Database. DbConnection decides Aurora-over-IAM (DB_HOST set) vs the local
    // connection string. The Aurora data source is built once here and reused by
    // every DbContext instance; the options are applied via AddDbContext so the
    // DbContextOptions constructor picks them up.
    var auroraDataSource = DbConnection.BuildAuroraDataSource(builder.Configuration);
    if (auroraDataSource is not null)
        services.AddSingleton(auroraDataSource);
    services.AddDbContext<DataContext>(options =>
        DbConnection.Apply(options, builder.Configuration, auroraDataSource));

    services.AddCors();
    services.AddControllers().AddJsonOptions(x =>
    {
        // serialize enums as strings in api responses (e.g. Role)
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
    services.AddAutoMapper(cfg => { }, typeof(AutoMapperProfile));
    services.AddSwaggerGen();

    // configure strongly typed settings object
    services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

    builder.Services.AddHttpClient<IRecommendationService, RecommendationService>(client =>
    {
        var baseUrl = builder.Configuration["RecommendationService:BaseUrl"]
                      ?? "http://localhost:8000";
        client.BaseAddress = new Uri(baseUrl);

        // Reasonable timeout � the Python query is normally <100ms, but if
        // the service is hung we don't want to block .NET requests forever.
        client.Timeout = TimeSpan.FromSeconds(10);
    });


    builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection("FileUpload"));

    // file upload config
    var uploadConfig = builder.Configuration.GetSection("FileUpload").Get<FileUploadSettings>()
    ?? throw new InvalidOperationException("FileUpload section missing from configuration.");

    var maxCategoryBytes = new[]
    {
        uploadConfig.Image.MaxSizeBytes,
        uploadConfig.Audio.MaxSizeBytes,
        uploadConfig.Video.MaxSizeBytes
    }.Max();

    var transportLimit = maxCategoryBytes + (5L * 1024 * 1024); // +5 MB headroom

    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = transportLimit;
        options.ValueLengthLimit = int.MaxValue;
        options.MultipartHeadersLengthLimit = int.MaxValue;
    });

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = transportLimit;
    });


    // AWS S3 client for media storage. The default constructor resolves the
    // region and credentials from the environment (AWS_REGION + the execution
    // role in Lambda; your profile/SSO locally).
    services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());

    // configure DI for application services
    services.AddScoped<IJwtUtils, JwtUtils>();
    services.AddScoped<IAccountService, AccountService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IFileService, FileService>();
    services.AddScoped<ISongService, SongService>();
    services.AddScoped<IPlaylistService, PlaylistService>();
    services.AddScoped<IFriendshipService, FriendshipService>();
    services.AddScoped<IPlaylistInvitationService, PlaylistInvitationService>();
}

var app = builder.Build();

// migrate any database changes on startup (includes initial db creation)
// Disabled for AWS Lambda: running migrations during startup executes on every
// cold start, requires the database to be reachable at init, can race across
// concurrent cold starts, and a failed migration would brick all invocations.
// Migrations are applied out-of-band instead (e.g. an EF migration bundle in CI
// or a one-off task). Re-enable for local development if desired.
//using (var scope = app.Services.CreateScope())
//{
//    var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
//    dataContext.Database.Migrate();
//}

// configure HTTP request pipeline
{
    // generated swagger json and swagger ui middleware
    app.UseSwagger();
    app.UseSwaggerUI(x => x.SwaggerEndpoint("/swagger/v1/swagger.json", ".NET Sign-up and Verification API"));

    // global cors policy
    app.UseCors(x => x
        .SetIsOriginAllowed(origin => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());

    // global error handler
    app.UseMiddleware<ErrorHandlerMiddleware>();

    // custom jwt auth middleware
    app.UseMiddleware<JwtMiddleware>();

    app.MapControllers();
}

app.Run("http://localhost:4000");