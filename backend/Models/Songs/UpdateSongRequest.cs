using System.ComponentModel.DataAnnotations;
using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Models.Songs
{
    public class UpdateSongRequest
    {
        [Required]
        public string Id { get; set; }

        public string? Name { get; set; }
        public string? Artist { get; set; }
        public Genre? Genre { get; set; }
    }
}