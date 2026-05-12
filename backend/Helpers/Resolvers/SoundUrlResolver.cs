using AutoMapper;
using SongAppApi.Entities;
using SongAppApi.Models.Songs;

namespace SongAppApi.Helpers.Resolvers
{
    public class SoundUrlResolver : IValueResolver<Song, SongResponse, string?>
    {
        public string? Resolve(Song source, SongResponse destination,
            string? destMember, ResolutionContext context)
        {
            if (source.SoundId.HasValue)
            {
                //relative path to stream endpoint
                return $"/Songs/{source.Id}/audio";
            }

            return source.SoundUrl;
        }
    }

}
