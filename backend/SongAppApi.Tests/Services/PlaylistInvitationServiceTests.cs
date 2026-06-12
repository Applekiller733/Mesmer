using FluentAssertions;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Services
{
    public class PlaylistInvitationServiceTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly PlaylistInvitationService _service;

        public PlaylistInvitationServiceTests()
        {
            _context = TestDbContextFactory.Create();
            var mapper = TestMapperFactory.Create(_context);
            _service = new PlaylistInvitationService(_context, mapper);
        }

        public void Dispose() => _context.Dispose();


        [Fact]
        public void Invite_PrivatePlaylist_Throws()
        {
            var owner = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Private);
            _context.Accounts.AddRange(owner, receiver);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Invite(
                owner.Id.ToString(), playlist.Id.ToString(), receiver.Id.ToString());

            act.Should().Throw<AppException>().WithMessage("*Private*");
        }

        [Fact]
        public void Invite_UnlistedPlaylist_OnlyOwner()
        {
            var owner = EntityBuilder.NewAccount();
            var notOwner = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Unlisted);
            _context.Accounts.AddRange(owner, notOwner, receiver);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Invite(
                notOwner.Id.ToString(), playlist.Id.ToString(), receiver.Id.ToString());

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void Invite_UnlistedPlaylist_OwnerSucceeds()
        {
            var owner = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Unlisted);
            _context.Accounts.AddRange(owner, receiver);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.Invite(
                owner.Id.ToString(), playlist.Id.ToString(), receiver.Id.ToString());

            result.Should().NotBeNull();
            _context.PlaylistInvitations.Should().ContainSingle();
        }

        [Fact]
        public void Invite_PublicPlaylist_AnyVisibleUserCanInvite()
        {
            var owner = EntityBuilder.NewAccount();
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            _context.Accounts.AddRange(owner, sender, receiver);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = _service.Invite(
                sender.Id.ToString(), playlist.Id.ToString(), receiver.Id.ToString());

            result.Should().NotBeNull();
        }


        [Fact]
        public void Invite_ToSelf_Throws()
        {
            var user = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(user, visibility: PlaylistVisibility.Public);
            _context.Accounts.Add(user);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Invite(
                user.Id.ToString(), playlist.Id.ToString(), user.Id.ToString());

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void Invite_ToOwner_Throws()
        {
            var owner = EntityBuilder.NewAccount();
            var sender = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            _context.Accounts.AddRange(owner, sender);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Invite(
                sender.Id.ToString(), playlist.Id.ToString(), owner.Id.ToString());

            act.Should().Throw<AppException>().WithMessage("*already have it*");
        }

        [Fact]
        public void Invite_ReceiverAlreadySaved_Throws()
        {
            var owner = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            playlist.SavedByAccounts.Add(receiver);
            _context.Accounts.AddRange(owner, receiver);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var act = () => _service.Invite(
                owner.Id.ToString(), playlist.Id.ToString(), receiver.Id.ToString());

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void Invite_MissingPlaylist_Throws()
        {
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(sender, receiver);
            _context.SaveChanges();

            var act = () => _service.Invite(
                sender.Id.ToString(), Guid.NewGuid().ToString(), receiver.Id.ToString());

            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void Invite_Idempotent_ReturnsExisting()
        {
            var owner = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            var existing = new PlaylistInvitation
            {
                PlaylistId = playlist.Id,
                SenderId = owner.Id,
                ReceiverId = receiver.Id,
            };
            _context.Accounts.AddRange(owner, receiver);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.Add(existing);
            _context.SaveChanges();

            var result = _service.Invite(
                owner.Id.ToString(), playlist.Id.ToString(), receiver.Id.ToString());

            result.Id.Should().Be(existing.Id.ToString());
            _context.PlaylistInvitations.Count().Should().Be(1);
        }

        [Fact]
        public void Invite_SenderBlockedReceiver_Throws()
        {
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(sender, visibility: PlaylistVisibility.Public);
            _context.Accounts.AddRange(sender, receiver);
            _context.Playlists.Add(playlist);
            _context.Friendships.Add(new Friendship
            {
                SenderId = sender.Id, ReceiverId = receiver.Id,
                Status = FriendshipStatus.Blocked,
            });
            _context.SaveChanges();

            var act = () => _service.Invite(
                sender.Id.ToString(), playlist.Id.ToString(), receiver.Id.ToString());

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void Invite_ReceiverBlockedSender_SyntheticSuccess()
        {
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(sender, visibility: PlaylistVisibility.Public);
            _context.Accounts.AddRange(sender, receiver);
            _context.Playlists.Add(playlist);
            _context.Friendships.Add(new Friendship
            {
                SenderId = receiver.Id, ReceiverId = sender.Id,
                Status = FriendshipStatus.Blocked,
            });
            _context.SaveChanges();

            var result = _service.Invite(
                sender.Id.ToString(), playlist.Id.ToString(), receiver.Id.ToString());

            result.Should().NotBeNull();
            _context.PlaylistInvitations.Should().BeEmpty();
        }


        [Fact]
        public void Accept_ReceiverAccepts_AddsToSavedAndRemovesRow()
        {
            var owner = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            var row = new PlaylistInvitation
            {
                PlaylistId = playlist.Id,
                SenderId = owner.Id,
                ReceiverId = receiver.Id,
            };
            _context.Accounts.AddRange(owner, receiver);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.Add(row);
            _context.SaveChanges();

            _service.Accept(receiver.Id.ToString(), row.Id.ToString());

            _context.PlaylistInvitations.Any(i => i.Id == row.Id).Should().BeFalse();
            var reloaded = _context.Playlists.Single(p => p.Id == playlist.Id);
            reloaded.SavedByAccounts.Should().Contain(a => a.Id == receiver.Id);
        }

        [Fact]
        public void Accept_OtherUserAccepts_Throws()
        {
            var owner = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var stranger = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            var row = new PlaylistInvitation
            {
                PlaylistId = playlist.Id,
                SenderId = owner.Id,
                ReceiverId = receiver.Id,
            };
            _context.Accounts.AddRange(owner, receiver, stranger);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.Add(row);
            _context.SaveChanges();

            var act = () => _service.Accept(stranger.Id.ToString(), row.Id.ToString());

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void Accept_AlreadySaved_DropsRowOnly()
        {
            var owner = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            playlist.SavedByAccounts.Add(receiver);
            var row = new PlaylistInvitation
            {
                PlaylistId = playlist.Id,
                SenderId = owner.Id,
                ReceiverId = receiver.Id,
            };
            _context.Accounts.AddRange(owner, receiver);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.Add(row);
            _context.SaveChanges();

            _service.Accept(receiver.Id.ToString(), row.Id.ToString());

            var reloaded = _context.Playlists.Single(p => p.Id == playlist.Id);
            reloaded.SavedByAccounts.Count(a => a.Id == receiver.Id).Should().Be(1);
        }


        [Fact]
        public void Decline_ReceiverDeclines_RemovesRow()
        {
            var owner = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            var row = new PlaylistInvitation
            {
                PlaylistId = playlist.Id,
                SenderId = owner.Id,
                ReceiverId = receiver.Id,
            };
            _context.Accounts.AddRange(owner, receiver);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.Add(row);
            _context.SaveChanges();

            _service.Decline(receiver.Id.ToString(), row.Id.ToString());

            _context.PlaylistInvitations.Any(i => i.Id == row.Id).Should().BeFalse();
        }

        [Fact]
        public void Decline_SenderTriesToDecline_Throws()
        {
            var owner = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            var row = new PlaylistInvitation
            {
                PlaylistId = playlist.Id,
                SenderId = owner.Id,
                ReceiverId = receiver.Id,
            };
            _context.Accounts.AddRange(owner, receiver);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.Add(row);
            _context.SaveChanges();

            var act = () => _service.Decline(owner.Id.ToString(), row.Id.ToString());

            act.Should().Throw<AppException>();
        }


        [Fact]
        public void Cancel_SenderCancels_RemovesRow()
        {
            var owner = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            var row = new PlaylistInvitation
            {
                PlaylistId = playlist.Id,
                SenderId = owner.Id,
                ReceiverId = receiver.Id,
            };
            _context.Accounts.AddRange(owner, receiver);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.Add(row);
            _context.SaveChanges();

            _service.Cancel(owner.Id.ToString(), row.Id.ToString());

            _context.PlaylistInvitations.Any(i => i.Id == row.Id).Should().BeFalse();
        }

        [Fact]
        public void Cancel_ReceiverTriesToCancel_Throws()
        {
            var owner = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(owner, visibility: PlaylistVisibility.Public);
            var row = new PlaylistInvitation
            {
                PlaylistId = playlist.Id,
                SenderId = owner.Id,
                ReceiverId = receiver.Id,
            };
            _context.Accounts.AddRange(owner, receiver);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.Add(row);
            _context.SaveChanges();

            var act = () => _service.Cancel(receiver.Id.ToString(), row.Id.ToString());

            act.Should().Throw<AppException>();
        }


        [Fact]
        public void GetIncoming_OnlyForUser()
        {
            var u = EntityBuilder.NewAccount();
            var other = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(other, visibility: PlaylistVisibility.Public);
            _context.Accounts.AddRange(u, other);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.AddRange(
                new PlaylistInvitation { PlaylistId = playlist.Id, SenderId = other.Id, ReceiverId = u.Id },
                new PlaylistInvitation { PlaylistId = playlist.Id, SenderId = u.Id, ReceiverId = other.Id });
            _context.SaveChanges();

            _service.GetIncoming(u.Id.ToString()).Should().ContainSingle();
        }

        [Fact]
        public void GetOutgoing_OnlyForUser()
        {
            var u = EntityBuilder.NewAccount();
            var other = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(u, visibility: PlaylistVisibility.Public);
            _context.Accounts.AddRange(u, other);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.AddRange(
                new PlaylistInvitation { PlaylistId = playlist.Id, SenderId = u.Id, ReceiverId = other.Id },
                new PlaylistInvitation { PlaylistId = playlist.Id, SenderId = other.Id, ReceiverId = u.Id });
            _context.SaveChanges();

            _service.GetOutgoing(u.Id.ToString()).Should().ContainSingle();
        }

        [Fact]
        public void CountIncoming_Returns()
        {
            var u = EntityBuilder.NewAccount();
            var s1 = EntityBuilder.NewAccount();
            var s2 = EntityBuilder.NewAccount();
            var playlist = EntityBuilder.NewPlaylist(s1, visibility: PlaylistVisibility.Public);
            _context.Accounts.AddRange(u, s1, s2);
            _context.Playlists.Add(playlist);
            _context.PlaylistInvitations.AddRange(
                new PlaylistInvitation { PlaylistId = playlist.Id, SenderId = s1.Id, ReceiverId = u.Id },
                new PlaylistInvitation { PlaylistId = playlist.Id, SenderId = s2.Id, ReceiverId = u.Id });
            _context.SaveChanges();

            _service.CountIncoming(u.Id.ToString()).Should().Be(2);
        }
    }
}
