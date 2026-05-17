using SongAppApi.Helpers.Resolvers;
using SongAppApi.Helpers.Enumerators;
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

            // Visibility is set explicitly by the service (PlaylistService.Create)
            // rather than copied through AutoMapper. The request type is nullable
            // and a missing value should default to Private — easier to make that
            // intent obvious in the service than to rely on AutoMapper's
            // nullable-to-non-nullable conventions. Ignoring it here ensures
            // AutoMapper can't accidentally overwrite a service-set value if the
            // order of operations changes later.
            CreateMap<CreatePlaylistRequest, Playlist>()
                .ForMember(dest => dest.Songs,
                    opt
                        => opt.MapFrom<CreatePlaylistSongResolver>())
                .ForMember(dest => dest.Visibility, opt => opt.Ignore());

            // UpdatePlaylistRequest deliberately has no Visibility field — see
            // the DTO doc for the reason. Be defensive: explicitly ignore so a
            // future addition to the DTO can't silently flip visibility on
            // every name-edit.
            CreateMap<UpdatePlaylistRequest, Playlist>()
                .ForMember(dest => dest.Songs,
                    opt
                        => opt.MapFrom<UpdatePlaylistSongResolver>())
                .ForMember(dest => dest.Visibility, opt => opt.Ignore());

            // Playlist → PlaylistResponse: the Visibility enum maps over
            // automatically by name and matching int values, so no explicit
            // configuration is needed for that field.
            CreateMap<Playlist, PlaylistResponse>();

            CreateMap<Entities.Friendship, Models.Friendships.FriendshipResponse>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.ToString()))
                .ForMember(d => d.SenderId, opt => opt.MapFrom(s => s.SenderId.ToString()))
                .ForMember(d => d.SenderUserName,
                    opt => opt.MapFrom(s => s.Sender != null ? s.Sender.UserName : null))
                .ForMember(d => d.SenderFriendCode,
                    opt => opt.MapFrom(s => s.Sender != null ?
                        s.Sender.FriendCode : null))
                .ForMember(d => d.ReceiverId, opt => opt.MapFrom(s => s.ReceiverId.ToString()))
                .ForMember(d => d.ReceiverUserName,
                    opt => opt.MapFrom(s => s.Receiver != null ? s.Receiver.UserName : null))
                .ForMember(d => d.ReceiverFriendCode,
                    opt => opt.MapFrom(s => s.Receiver != null ?
                        s.Receiver.FriendCode : null));
            // Status is mapped automatically: same name, same type
            // (FriendshipStatus) on both sides. The previous `(int)`
            // cast was removed when FriendshipResponse.Status was
            // retyped from int to FriendshipStatus — see the wire-
            // format change note in FriendshipResponse.cs.

            // PlaylistInvitation → PlaylistInvitationResponse: same overall
            // shape as the Friendship mapping (Sender/Receiver denormalisation),
            // plus three playlist fields pulled from the navigation. Null
            // guards everywhere because EF returns navigation properties as
            // null when the Include() is missing — bugs that produce
            // null-laden DTOs are easier to debug than a NRE inside AutoMapper.
            CreateMap<Entities.PlaylistInvitation, Models.PlaylistInvitations.PlaylistInvitationResponse>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.ToString()))

                .ForMember(d => d.PlaylistId,
                    opt => opt.MapFrom(s => s.PlaylistId.ToString()))
                .ForMember(d => d.PlaylistName,
                    opt => opt.MapFrom(s => s.Playlist != null ? s.Playlist.Name : null))
                .ForMember(d => d.PlaylistVisibility,
                    // Both sides are PlaylistVisibility now; AutoMapper
                    // would auto-map by name, but we keep the explicit
                    // mapping so the null-guard on Playlist (which can
                    // be null when the navigation isn't Included) stays
                    // visible. Falls back to Private — the safest
                    // sentinel — when Playlist is missing.
                    opt => opt.MapFrom(s => s.Playlist != null
                        ? s.Playlist.Visibility
                        : PlaylistVisibility.Private))

                .ForMember(d => d.SenderId,
                    opt => opt.MapFrom(s => s.SenderId.ToString()))
                .ForMember(d => d.SenderUserName,
                    opt => opt.MapFrom(s => s.Sender != null ? s.Sender.UserName : null))
                .ForMember(d => d.SenderFriendCode,
                    opt => opt.MapFrom(s => s.Sender != null ? s.Sender.FriendCode : null))

                .ForMember(d => d.ReceiverId,
                    opt => opt.MapFrom(s => s.ReceiverId.ToString()))
                .ForMember(d => d.ReceiverUserName,
                    opt => opt.MapFrom(s => s.Receiver != null ? s.Receiver.UserName : null))
                .ForMember(d => d.ReceiverFriendCode,
                    opt => opt.MapFrom(s => s.Receiver != null ? s.Receiver.FriendCode : null));
        }
    }
}