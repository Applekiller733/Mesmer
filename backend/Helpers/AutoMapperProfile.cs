using SongAppApi.Helpers.Resolvers;
using SongAppApi.Models.Playlist;
using SongAppApi.Models.Songs;

namespace SongAppApi.Helpers
{
    using AutoMapper;
    using Microsoft.AspNetCore.Identity.Data;
    using SongAppApi.Entities;
    using SongAppApi.Models.Accounts;

    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Account, AccountResponse>();
            CreateMap<Account, AccountProfileResponse>();
            CreateMap<Account, AccountProfilePictureResponse>();

            CreateMap<Account, AuthenticateResponse>();

            CreateMap<Models.Accounts.RegisterRequest, Account>();

            CreateMap<CreateRequest, Account>();

            CreateMap<UpdateRequest, Account>()
                .ForMember(dest => dest.ProfilePicture, opt => opt.Ignore())
                .ForAllMembers(x => x.Condition(
                    (src, dest, prop) =>
                    {
                        // ignore null & empty string properties
                        if (prop == null) return false;
                        if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return false;

                        // ignore null role
                        if (x.DestinationMember.Name == "Role" && src.Role == null) return false;

                        return true;
                    }
                ));

            CreateMap<CreateSongRequest, Song>();
            CreateMap<Song, SongResponse>()
                .ForMember(dest => dest.LikedByAccountIds,
                    opt 
                        => opt.MapFrom(src => src.LikedByAccounts.Select(a => a.Id).ToList()));
            CreateMap<Song, UpvotesResponse>();

            CreateMap<CreatePlaylistRequest, Playlist>()
                .ForMember(dest => dest.Songs,
                    opt 
                        => opt.MapFrom<CreatePlaylistSongResolver>());
            CreateMap<UpdatePlaylistRequest, Playlist>()
                .ForMember(dest => dest.Songs,
                    opt
                        => opt.MapFrom<UpdatePlaylistSongResolver>());
            CreateMap<Playlist, PlaylistResponse>();
        }
    }
}
