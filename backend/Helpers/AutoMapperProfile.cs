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
                        => opt.MapFrom(src => src.LikedByAccounts.Select(a => a.Id).ToList()))
                .ForMember(
                            dest => dest.SoundUrl,
                            opt => opt.MapFrom<SoundUrlResolver>()
                );
            

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

            CreateMap<Entities.Friendship, Models.Friendships.FriendshipResponse>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.ToString()))
                .ForMember(d => d.SenderId, opt => opt.MapFrom(s => s.SenderId.ToString()))
                .ForMember(d => d.SenderUserName,
                    opt => opt.MapFrom(s => s.Sender != null ? s.Sender.UserName : null))
                .ForMember(d => d.SenderFriendCode,
                    opt => opt.MapFrom(s => s.Sender != null ? s.Sender.FriendCode : null))
                .ForMember(d => d.ReceiverId, opt => opt.MapFrom(s => s.ReceiverId.ToString()))
                .ForMember(d => d.ReceiverUserName,
                    opt => opt.MapFrom(s => s.Receiver != null ? s.Receiver.UserName : null))
                .ForMember(d => d.ReceiverFriendCode,
                    opt => opt.MapFrom(s => s.Receiver != null ? s.Receiver.FriendCode : null))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => (int)s.Status));

        }
    }
}
