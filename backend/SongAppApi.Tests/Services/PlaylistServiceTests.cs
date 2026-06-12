using FluentAssertions;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Playlist;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Services
{
    public class PlaylistServiceTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly PlaylistService _service;

        public PlaylistServiceTests()
        {
            _context = TestDbContextFactory.Create();
            var mapper = TestMapperFactory.Create(_context);
            _service = new PlaylistService(_context, jwtUtils: null!, mapper);
        }

        public void Dispose() => _context.Dispose();


        [Fact]
        public void Get_PublicPlaylist_AnyUserCanRead()
        {
            var owner = EntityBuilder.NewAccount();
            var stranger = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            _context.Accounts.AddRange(owner, stranger);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.Get(playlist.Id.ToString(), stranger.Id.ToString());

            result.Should().NotBeNull();
            result.Id.Should().Be(playlist.Id.ToString());
        }

        [Fact]
        public void Get_PrivatePlaylist_OwnerCanRead()
        {
            var owner = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Private);
            _context.Accounts.Add(owner);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.Get(playlist.Id.ToString(), owner.Id.ToString());

            result.Should().NotBeNull();
        }

        [Fact]
        public void Get_PrivatePlaylist_StrangerGets404NotForbidden()
        {
            // the interface explicitly documents this anti-enumeration behaviour:
            // unauthorised reads throw KeyNotFoundException, not a forbidden-style
            // error, so private playlist IDs can't be probed.
            var owner = EntityBuilder.NewAccount();
            var stranger = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Private);
            _context.Accounts.AddRange(owner, stranger);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Get(playlist.Id.ToString(), stranger.Id.ToString());

            act.Should().Throw<KeyNotFoundException>()
                .WithMessage("Playlist could not be found");
        }

        [Fact]
        public void Get_UnlistedPlaylist_StrangerCannotRead()
        {
            var owner = EntityBuilder.NewAccount();
            var stranger = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Unlisted);
            _context.Accounts.AddRange(owner, stranger);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Get(playlist.Id.ToString(), stranger.Id.ToString());

            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void Get_UnlistedPlaylist_SaverCanRead()
        {
            var owner = EntityBuilder.NewAccount();
            var saver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Unlisted);
            playlist.SavedByAccounts.Add(saver);
            _context.Accounts.AddRange(owner, saver);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.Get(playlist.Id.ToString(), saver.Id.ToString());

            result.Should().NotBeNull();
        }

        [Fact]
        public void Get_UnlistedPlaylist_InviteeCanRead()
        {
            var owner = EntityBuilder.NewAccount();
            var invitee = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Unlisted);
            _context.Accounts.AddRange(owner, invitee);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.Add(new PlaylistInvitation
            {
                PlaylistId = playlist.Id,
                SenderId = owner.Id,
                ReceiverId = invitee.Id,
            });
            _context.SaveChanges();

            var result = _service.Get(playlist.Id.ToString(), invitee.Id.ToString());

            result.Should().NotBeNull();
        }

        [Fact]
        public void Get_MissingPlaylist_Throws()
        {
            var act = () => _service.Get(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void Get_AnonymousCaller_PublicVisible()
        {
            var owner = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            _context.Accounts.Add(owner);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.Get(playlist.Id.ToString(), currentUserId: null);

            result.Should().NotBeNull();
        }

        [Fact]
        public void Get_AnonymousCaller_PrivateHidden()
        {
            var owner = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Private);
            _context.Accounts.Add(owner);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Get(playlist.Id.ToString(), currentUserId: null);
            act.Should().Throw<KeyNotFoundException>();
        }


        [Fact]
        public void GetInternal_PrivatePlaylist_Returns()
        {
            var owner = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Private);
            _context.Accounts.Add(owner);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.GetInternal(playlist.Id.ToString());

            result.Should().NotBeNull();
        }


        [Fact]
        public void GetCreatedByAccount_SelfView_ReturnsAllVisibilities()
        {
            var owner = EntityBuilder.NewAccount();
            _context.Accounts.Add(owner);
            _context.Playlists.AddRange(
                EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Private),
                EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Unlisted),
                EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public));
            _context.SaveChanges();

            var result = _service.GetCreatedByAccount(
                owner.Id.ToString(),
                currentUserId: owner.Id.ToString()).ToList();

            result.Should().HaveCount(3);
        }

        [Fact]
        public void GetCreatedByAccount_OtherView_OnlyPublic()
        {
            var owner = EntityBuilder.NewAccount();
            var viewer = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(owner, viewer);
            _context.Playlists.AddRange(
                EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Private),
                EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Unlisted),
                EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public));
            _context.SaveChanges();

            var result = _service.GetCreatedByAccount(
                owner.Id.ToString(),
                currentUserId: viewer.Id.ToString()).ToList();

            result.Should().ContainSingle();
            result[0].Visibility.Should().Be(PlaylistVisibility.Public);
        }


        [Fact]
        public void GetSavedByAccount_OtherUser_Throws()
        {
            var owner = EntityBuilder.NewAccount();
            var stranger = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(owner, stranger);
            _context.SaveChanges();

            var act = () => _service.GetSavedByAccount(
                owner.Id.ToString(), stranger.Id.ToString(), isAdmin: false);

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void GetSavedByAccount_AdminCanViewAnyone()
        {
            var owner = EntityBuilder.NewAccount();
            var admin = EntityBuilder.NewAccount(role: Role.Admin);
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            _context.Accounts.AddRange(owner, admin);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.GetSavedByAccount(
                owner.Id.ToString(), admin.Id.ToString(), isAdmin: true).ToList();

            result.Should().HaveCount(1);
        }

        [Fact]
        public void GetSavedByAccount_Self_Returns()
        {
            var owner = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner);
            _context.Accounts.Add(owner);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.GetSavedByAccount(
                owner.Id.ToString(), owner.Id.ToString(), isAdmin: false).ToList();

            result.Should().HaveCount(1);
        }


        [Fact]
        public void Create_DefaultsToPrivateWhenVisibilityOmitted()
        {
            var owner = EntityBuilder.NewAccount();
            _context.Accounts.Add(owner);
            _context.SaveChanges();

            var result = _service.Create(new CreatePlaylistRequest
            {
                Name = "New",
                SongIds = new List<string>(),
                Visibility = null,
            }, owner);

            result.Visibility.Should().Be(PlaylistVisibility.Private);
        }

        [Fact]
        public void Create_RespectsRequestedVisibility()
        {
            var owner = EntityBuilder.NewAccount();
            _context.Accounts.Add(owner);
            _context.SaveChanges();

            var result = _service.Create(new CreatePlaylistRequest
            {
                Name = "Public one",
                SongIds = new List<string>(),
                Visibility = PlaylistVisibility.Public,
            }, owner);

            result.Visibility.Should().Be(PlaylistVisibility.Public);
        }

        [Fact]
        public void Create_AddsOwnerToSavedByAccounts()
        {
            var owner = EntityBuilder.NewAccount();
            _context.Accounts.Add(owner);
            _context.SaveChanges();

            var result = _service.Create(new CreatePlaylistRequest
            {
                Name = "My playlist",
                SongIds = new List<string>(),
            }, owner);

            var stored = _context.Playlists
                .Single(p => p.Id.ToString() == result.Id);
            stored.SavedByAccounts.Should().ContainSingle(a => a.Id == owner.Id);
        }


        [Fact]
        public void Update_OwnerCanEdit()
        {
            var owner = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner);
            _context.Accounts.Add(owner);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.Update(
                playlist.Id.ToString(),
                new UpdatePlaylistRequest
                {
                    Id = playlist.Id.ToString(),
                    Name = "Renamed",
                    SongIds = new List<string>(),
                },
                owner.Id.ToString(),
                isAdmin: false);

            result.Name.Should().Be("Renamed");
        }

        [Fact]
        public void Update_StrangerThrows()
        {
            var owner = EntityBuilder.NewAccount();
            var stranger = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner);
            _context.Accounts.AddRange(owner, stranger);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Update(
                playlist.Id.ToString(),
                new UpdatePlaylistRequest
                {
                    Id = playlist.Id.ToString(),
                    Name = "Hijack",
                    SongIds = new List<string>(),
                },
                stranger.Id.ToString(),
                isAdmin: false);

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void Update_AdminCanEditAnyone()
        {
            var owner = EntityBuilder.NewAccount();
            var admin = EntityBuilder.NewAccount(role: Role.Admin);
            var playlist = EntityBuilder.NewPlaylist(owner);
            _context.Accounts.AddRange(owner, admin);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.Update(
                playlist.Id.ToString(),
                new UpdatePlaylistRequest
                {
                    Id = playlist.Id.ToString(),
                    Name = "Admin-edited",
                    SongIds = new List<string>(),
                },
                admin.Id.ToString(),
                isAdmin: true);

            result.Name.Should().Be("Admin-edited");
        }


        [Fact]
        public void Delete_OwnerCanDelete()
        {
            var owner = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner);
            _context.Accounts.Add(owner);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            _service.Delete(playlist.Id.ToString(), owner.Id.ToString(), isAdmin: false);

            _context.Playlists.Any(p => p.Id == playlist.Id).Should().BeFalse();
        }

        [Fact]
        public void Delete_StrangerThrows()
        {
            var owner = EntityBuilder.NewAccount();
            var stranger = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner);
            _context.Accounts.AddRange(owner, stranger);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Delete(
                playlist.Id.ToString(), stranger.Id.ToString(), isAdmin: false);

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void Save_PublicPlaylist_Succeeds()
        {
            var owner = EntityBuilder.NewAccount();
            var saver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            _context.Accounts.AddRange(owner, saver);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            _service.Save(playlist.Id.ToString(), saver.Id.ToString());

            var reloaded = _context.Playlists.Single(p => p.Id == playlist.Id);
            reloaded.SavedByAccounts.Should().Contain(a => a.Id == saver.Id);
        }

        [Fact]
        public void Save_PrivatePlaylist_StrangerGets404()
        {
            var owner = EntityBuilder.NewAccount();
            var stranger = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Private);
            _context.Accounts.AddRange(owner, stranger);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Save(playlist.Id.ToString(), stranger.Id.ToString());

            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void Save_IsIdempotent()
        {
            var owner = EntityBuilder.NewAccount();
            var saver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            playlist.SavedByAccounts.Add(saver);
            _context.Accounts.AddRange(owner, saver);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Save(playlist.Id.ToString(), saver.Id.ToString());

            act.Should().NotThrow();
            var reloaded = _context.Playlists.Single(p => p.Id == playlist.Id);
            reloaded.SavedByAccounts.Count(a => a.Id == saver.Id).Should().Be(1);
        }

        [Fact]
        public void Save_DropsPendingInvitation()
        {
            var owner = EntityBuilder.NewAccount();
            var invitee = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Unlisted);
            _context.Accounts.AddRange(owner, invitee);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.Add(new PlaylistInvitation
            {
                PlaylistId = playlist.Id,
                SenderId = owner.Id,
                ReceiverId = invitee.Id,
            });
            _context.SaveChanges();

            _service.Save(playlist.Id.ToString(), invitee.Id.ToString());

            _context.PlaylistInvitations
                .Any(i => i.PlaylistId == playlist.Id && i.ReceiverId == invitee.Id)
                .Should().BeFalse();
        }


        [Fact]
        public void Unsave_OwnerThrows()
        {
            var owner = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner);
            _context.Accounts.Add(owner);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Unsave(playlist.Id.ToString(), owner.Id.ToString());

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void Unsave_NotInLibrary_Throws()
        {
            var owner = EntityBuilder.NewAccount();
            var stranger = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            _context.Accounts.AddRange(owner, stranger);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Unsave(playlist.Id.ToString(), stranger.Id.ToString());

            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void Unsave_RemovesUserFromSavers()
        {
            var owner = EntityBuilder.NewAccount();
            var saver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            playlist.SavedByAccounts.Add(saver);
            _context.Accounts.AddRange(owner, saver);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            _service.Unsave(playlist.Id.ToString(), saver.Id.ToString());

            var reloaded = _context.Playlists.Single(p => p.Id == playlist.Id);
            reloaded.SavedByAccounts.Should().NotContain(a => a.Id == saver.Id);
        }


        [Fact]
        public void UpdateVisibility_OwnerCanChange()
        {
            var owner = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Private);
            _context.Accounts.Add(owner);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.UpdateVisibility(
                playlist.Id.ToString(),
                PlaylistVisibility.Public,
                owner.Id.ToString(),
                isAdmin: false);

            result.Visibility.Should().Be(PlaylistVisibility.Public);
        }

        [Fact]
        public void UpdateVisibility_StrangerThrows()
        {
            var owner = EntityBuilder.NewAccount();
            var stranger = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Private);
            _context.Accounts.AddRange(owner, stranger);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.UpdateVisibility(
                playlist.Id.ToString(),
                PlaylistVisibility.Public,
                stranger.Id.ToString(),
                isAdmin: false);

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void UpdateVisibility_ToPrivate_RemovesPendingInvitations()
        {
            var owner = EntityBuilder.NewAccount();
            var invitee = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Unlisted);
            _context.Accounts.AddRange(owner, invitee);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.Add(new PlaylistInvitation
            {
                PlaylistId = playlist.Id,
                SenderId = owner.Id,
                ReceiverId = invitee.Id,
            });
            _context.SaveChanges();

            _service.UpdateVisibility(
                playlist.Id.ToString(),
                PlaylistVisibility.Private,
                owner.Id.ToString(),
                isAdmin: false);

            _context.PlaylistInvitations
                .Any(i => i.PlaylistId == playlist.Id).Should().BeFalse();
        }

        [Fact]
        public void UpdateVisibility_PublicToUnlisted_KeepsInvitations()
        {
            var owner = EntityBuilder.NewAccount();
            var invitee = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            _context.Accounts.AddRange(owner, invitee);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.Add(new PlaylistInvitation
            {
                PlaylistId = playlist.Id,
                SenderId = owner.Id,
                ReceiverId = invitee.Id,
            });
            _context.SaveChanges();

            _service.UpdateVisibility(
                playlist.Id.ToString(),
                PlaylistVisibility.Unlisted,
                owner.Id.ToString(),
                isAdmin: false);

            _context.PlaylistInvitations
                .Any(i => i.PlaylistId == playlist.Id).Should().BeTrue();
        }

        [Fact]
        public void UpdateVisibility_NoOpWhenUnchanged()
        {
            var owner = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            var before = playlist.UpdatedAt;
            _context.Accounts.Add(owner);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.UpdateVisibility(
                playlist.Id.ToString(),
                PlaylistVisibility.Public,
                owner.Id.ToString(),
                isAdmin: false);

            result.Visibility.Should().Be(PlaylistVisibility.Public);
            // UpdatedAt should not have moved on the no-op fast path.
            var reloaded = _context.Playlists.Single(p => p.Id == playlist.Id);
            reloaded.UpdatedAt.Should().Be(before);
        }
    }
}
