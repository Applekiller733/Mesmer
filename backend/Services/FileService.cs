using AutoMapper;
using Microsoft.Extensions.Options;
using SongAppApi.Authorization;
using SongAppApi.Helpers;
using SongAppApi.Models.Files;
using File = SongAppApi.Entities.File;
using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Services
{
    public interface IFileService
    {
        string Create(FileModel file);
        string Create(FileModel file, string subfolderpath);
        File CreateFromFormFile(IFormFile formFile, string subfolderpath, FileCategory category);
        File GetFileById(string id);
        bool VerifyExistingDirectory(string path);
    }

    public class FileService : IFileService
    {
        private readonly DataContext _context;
        private readonly IJwtUtils _jwtUtils;
        private readonly IMapper _mapper;
        private readonly AppSettings _settings;
        private readonly FileUploadSettings _uploadSettings;

        // cached lookup for case-insensitive extension matching
        // built once at construction; appsettings.json reload would require restart
        private readonly IReadOnlyDictionary<FileCategory, HashSet<string>> _allowedExtensions;
        private readonly IReadOnlyDictionary<FileCategory, long> _maxSizeBytes;

        public FileService(DataContext context, IJwtUtils jwtUtils,
            IMapper mapper, IOptions<AppSettings> settings,
            IOptions<FileUploadSettings> uploadSettings)
        {
            _context = context;
            _jwtUtils = jwtUtils;
            _mapper = mapper;
            _settings = settings.Value;
            _uploadSettings = uploadSettings.Value;

            // build lookup tables from config
            var asDict = _uploadSettings.AsDictionary();

            _allowedExtensions = asDict.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<string>(
                    kvp.Value.AllowedExtensions ?? new List<string>(),
                    StringComparer.OrdinalIgnoreCase));

            _maxSizeBytes = asDict.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.MaxSizeBytes);

            foreach (var (category, exts) in _allowedExtensions)
            {
                if (exts.Count == 0)
                    throw new InvalidOperationException(
                        $"FileUpload:{category}:AllowedExtensions is empty in configuration.");
            }
        }

        public string Create(FileModel file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            string path = Path.Combine(Directory.GetCurrentDirectory(),
                "Resources", file.FileName);

            using (Stream stream = new FileStream(path, FileMode.Create))
            {
                file.FormFile.CopyTo(stream);
            }

            File fileEF = new File
            {
                FileName = file.FileName,
                Extension = file.Extension,
                FilePath = path
            };

            _context.Files.Add(fileEF);
            _context.SaveChanges();

            return fileEF.Id.ToString();
        }

        public string Create(FileModel file, string subfolderpath)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            string path = Path.Combine(Directory.GetCurrentDirectory(),
                "Resources", subfolderpath, file.FileName);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using (Stream stream = new FileStream(path, FileMode.Create))
            {
                file.FormFile.CopyTo(stream);
            }

            File fileEF = new File
            {
                FileName = file.FileName,
                Extension = file.Extension,
                FilePath = path
            };

            _context.Files.Add(fileEF);
            _context.SaveChanges();

            return fileEF.Id.ToString();
        }

        public File CreateFromFormFile(IFormFile formFile, string subfolderpath, FileCategory category)
        {
            if (formFile == null || formFile.Length == 0)
                throw new ArgumentException("File is empty.", nameof(formFile));

            // check size
            var maxSize = _maxSizeBytes[category];
            if (formFile.Length > maxSize)
                throw new InvalidOperationException(
                    $"File too large. Maximum {maxSize / (1024 * 1024)} MB for {category}.");

            // chk extension
            var originalName = formFile.FileName;
            var extension = Path.GetExtension(originalName);
            if (string.IsNullOrEmpty(extension) || !_allowedExtensions[category].Contains(extension))
                throw new InvalidOperationException(
                    $"Extension '{extension}' is not allowed for {category}.");

            // sanitize file name
            var safeFileName = $"{Guid.NewGuid():N}{extension}";

            var directory = Path.Combine(Directory.GetCurrentDirectory(), "Resources", subfolderpath);
            Directory.CreateDirectory(directory);
            var fullPath = Path.Combine(directory, safeFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                formFile.CopyTo(stream);
            }

            var entity = new File
            {
                FileName = originalName,
                Extension = extension.TrimStart('.'),
                FilePath = fullPath
            };

            _context.Files.Add(entity);
            _context.SaveChanges();

            return entity;
        }

        public File GetFileById(string id)
        {
            var file = _context.Files.Find(Guid.Parse(id));
            if (file == null)
            {
                throw new KeyNotFoundException("Could not find file");
            }
            return file;
        }

        public bool VerifyExistingDirectory(string path)
        {
            return Directory.Exists(path);
        }

        public void createSubFolders(string path)
        {
            Directory.CreateDirectory(path);
        }
    }
}