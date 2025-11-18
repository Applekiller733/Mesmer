using AutoMapper;
using Microsoft.Extensions.Options;
using SongAppApi.Authorization;
using SongAppApi.Helpers;
using SongAppApi.Models.Files;
using File = SongAppApi.Entities.File;

namespace SongAppApi.Services
{
    public interface IFileService
    {
        int Create(FileModel file);
        int Create(FileModel file, string subfolderpath);
        File GetFileById(int id);
        bool VerifyExistingDirectory(string path);
    }
    public class FileService : IFileService
    {
        
        private readonly DataContext _context;
        private readonly IJwtUtils _jwtUtils;
        private readonly IMapper _mapper;
        private readonly AppSettings _settings;

        public FileService(DataContext context, IJwtUtils jwtUtils,
            IMapper mapper, IOptions<AppSettings> settings)
        {
            _context = context;
            _jwtUtils = jwtUtils;
            _mapper = mapper;
            _settings = settings.Value;
        }


        public int Create(FileModel file)
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

            return fileEF.Id;
        }

        public int Create(FileModel file, string subfolderpath)
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

            return fileEF.Id;
        }


        public File GetFileById(int id)
        {
            var file = _context.Files.Find(id);
            if (file == null)
            {
                throw new KeyNotFoundException("Could not find file");
            }
            return file;
        }

        public bool VerifyExistingDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return true;
            }

            return false;
        }

        public void createSubFolders(string path)
        {
            Directory.CreateDirectory(path);
        }
    }
}
