using FluentAssertions;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Services
{
    public class FriendshipServiceTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly FriendshipService _service;

        public FriendshipServiceTests()
        {
            _context = TestDbContextFactory.Create();
            var mapper = TestMapperFactory.Create(_context);
            _service = new FriendshipService(_context, mapper);
        }

        public void Dispose() => _context.Dispose();


        [Fact]
        public void SendRequest_ToSelf_Throws()
        {
            var user = EntityBuilder.NewAccount();
            _context.Accounts.Add(user);
            _context.SaveChanges();

            var act = () => _service.SendRequest(user.Id.ToString(), user.Id.ToString());

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void SendRequest_ToNonexistentUser_Throws()
        {
            var sender = EntityBuilder.NewAccount();
            _context.Accounts.Add(sender);
            _context.SaveChanges();

            var act = () => _service.SendRequest(sender.Id.ToString(), Guid.NewGuid().ToString());

            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void SendRequest_CreatesPendingRow()
        {
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(sender, receiver);
            _context.SaveChanges();

            var result = _service.SendRequest(sender.Id.ToString(), receiver.Id.ToString());

            result.Status.Should().Be(FriendshipStatus.Pending);
            _context.Friendships.Should().ContainSingle();
        }

        [Fact]
        public void SendRequest_AlreadyFriends_Throws()
        {
            var a = EntityBuilder.NewAccount();
            var b = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(a, b);
            _context.Friendships.Add(new Friendship
            {
                SenderId = a.Id, ReceiverId = b.Id,
                Status = FriendshipStatus.Accepted,
            });
            _context.SaveChanges();

            var act = () => _service.SendRequest(a.Id.ToString(), b.Id.ToString());

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void SendRequest_AlreadySentBySender_Idempotent()
        {
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var existing = new Friendship
            {
                SenderId = sender.Id, ReceiverId = receiver.Id,
                Status = FriendshipStatus.Pending,
            };
            _context.Accounts.AddRange(sender, receiver);
            _context.Friendships.Add(existing);
            _context.SaveChanges();

            var result = _service.SendRequest(sender.Id.ToString(), receiver.Id.ToString());

            result.Id.Should().Be(existing.Id.ToString());
            _context.Friendships.Count().Should().Be(1);
        }

        [Fact]
        public void SendRequest_ReceiverAlreadySent_TellsToAccept()
        {
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            // receiver sent to sender first
            _context.Accounts.AddRange(sender, receiver);
            _context.Friendships.Add(new Friendship
            {
                SenderId = receiver.Id, ReceiverId = sender.Id,
                Status = FriendshipStatus.Pending,
            });
            _context.SaveChanges();

            var act = () => _service.SendRequest(sender.Id.ToString(), receiver.Id.ToString());

            act.Should().Throw<AppException>().WithMessage("*Accept it instead*");
        }

        [Fact]
        public void SendRequest_SenderHasBlockedReceiver_Throws()
        {
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(sender, receiver);
            _context.Friendships.Add(new Friendship
            {
                SenderId = sender.Id, ReceiverId = receiver.Id,
                Status = FriendshipStatus.Blocked,
            });
            _context.SaveChanges();

            var act = () => _service.SendRequest(sender.Id.ToString(), receiver.Id.ToString());

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void SendRequest_ReceiverHasBlockedSender_SyntheticSuccess()
        {
            // shadow-block: the sender thinks it worked, no row inserted.
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(sender, receiver);
            _context.Friendships.Add(new Friendship
            {
                SenderId = receiver.Id, ReceiverId = sender.Id,
                Status = FriendshipStatus.Blocked,
            });
            _context.SaveChanges();

            var result = _service.SendRequest(sender.Id.ToString(), receiver.Id.ToString());

            result.Status.Should().Be(FriendshipStatus.Pending);
            _context.Friendships.Count(f => f.Status == FriendshipStatus.Pending).Should().Be(0);
        }


        [Fact]
        public void AcceptRequest_ReceiverAccepts_Succeeds()
        {
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var row = new Friendship
            {
                SenderId = sender.Id, ReceiverId = receiver.Id,
                Status = FriendshipStatus.Pending,
            };
            _context.Accounts.AddRange(sender, receiver);
            _context.Friendships.Add(row);
            _context.SaveChanges();

            var result = _service.AcceptRequest(receiver.Id.ToString(), row.Id.ToString());

            result.Status.Should().Be(FriendshipStatus.Accepted);
        }

        [Fact]
        public void AcceptRequest_SenderTriesToAccept_Throws()
        {
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var row = new Friendship
            {
                SenderId = sender.Id, ReceiverId = receiver.Id,
                Status = FriendshipStatus.Pending,
            };
            _context.Accounts.AddRange(sender, receiver);
            _context.Friendships.Add(row);
            _context.SaveChanges();

            var act = () => _service.AcceptRequest(sender.Id.ToString(), row.Id.ToString());

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void AcceptRequest_MissingRow_Throws()
        {
            var receiver = EntityBuilder.NewAccount();
            _context.Accounts.Add(receiver);
            _context.SaveChanges();

            var act = () => _service.AcceptRequest(
                receiver.Id.ToString(), Guid.NewGuid().ToString());

            act.Should().Throw<KeyNotFoundException>();
        }


        [Fact]
        public void DeclineRequest_RemovesRow()
        {
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var row = new Friendship
            {
                SenderId = sender.Id, ReceiverId = receiver.Id,
                Status = FriendshipStatus.Pending,
            };
            _context.Accounts.AddRange(sender, receiver);
            _context.Friendships.Add(row);
            _context.SaveChanges();

            _service.DeclineRequest(receiver.Id.ToString(), row.Id.ToString());

            _context.Friendships.Any(f => f.Id == row.Id).Should().BeFalse();
        }

        [Fact]
        public void DeclineRequest_SenderTriesToDecline_Throws()
        {
            var sender = EntityBuilder.NewAccount();
            var receiver = EntityBuilder.NewAccount();
            var row = new Friendship
            {
                SenderId = sender.Id, ReceiverId = receiver.Id,
                Status = FriendshipStatus.Pending,
            };
            _context.Accounts.AddRange(sender, receiver);
            _context.Friendships.Add(row);
            _context.SaveChanges();

            var act = () => _service.DeclineRequest(sender.Id.ToString(), row.Id.ToString());

            act.Should().Throw<AppException>();
        }


        [Fact]
        public void RemoveFriend_EitherPartyCanRemove()
        {
            var a = EntityBuilder.NewAccount();
            var b = EntityBuilder.NewAccount();
            var row = new Friendship
            {
                SenderId = a.Id, ReceiverId = b.Id,
                Status = FriendshipStatus.Accepted,
            };
            _context.Accounts.AddRange(a, b);
            _context.Friendships.Add(row);
            _context.SaveChanges();

            _service.RemoveFriend(b.Id.ToString(), a.Id.ToString());

            _context.Friendships.Any(f => f.Id == row.Id).Should().BeFalse();
        }

        [Fact]
        public void RemoveFriend_NotFriends_Throws()
        {
            var a = EntityBuilder.NewAccount();
            var b = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(a, b);
            _context.SaveChanges();

            var act = () => _service.RemoveFriend(a.Id.ToString(), b.Id.ToString());

            act.Should().Throw<AppException>();
        }


        [Fact]
        public void Block_InsertsBlockedRow_AndRemovesPriorRelationship()
        {
            var blocker = EntityBuilder.NewAccount();
            var blocked = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(blocker, blocked);
            _context.Friendships.Add(new Friendship
            {
                SenderId = blocker.Id, ReceiverId = blocked.Id,
                Status = FriendshipStatus.Accepted,
            });
            _context.SaveChanges();

            var result = _service.Block(blocker.Id.ToString(), blocked.Id.ToString());

            result.Status.Should().Be(FriendshipStatus.Blocked);
            _context.Friendships.Count().Should().Be(1);
            _context.Friendships.Single().Status.Should().Be(FriendshipStatus.Blocked);
        }

        [Fact]
        public void Block_Self_Throws()
        {
            var user = EntityBuilder.NewAccount();
            _context.Accounts.Add(user);
            _context.SaveChanges();

            var act = () => _service.Block(user.Id.ToString(), user.Id.ToString());

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void Block_AlreadyBlocked_Idempotent()
        {
            var blocker = EntityBuilder.NewAccount();
            var blocked = EntityBuilder.NewAccount();
            var existing = new Friendship
            {
                SenderId = blocker.Id, ReceiverId = blocked.Id,
                Status = FriendshipStatus.Blocked,
            };
            _context.Accounts.AddRange(blocker, blocked);
            _context.Friendships.Add(existing);
            _context.SaveChanges();

            var result = _service.Block(blocker.Id.ToString(), blocked.Id.ToString());

            result.Id.Should().Be(existing.Id.ToString());
            _context.Friendships.Count().Should().Be(1);
        }


        [Fact]
        public void Unblock_RemovesBlockedRow()
        {
            var blocker = EntityBuilder.NewAccount();
            var blocked = EntityBuilder.NewAccount();
            var row = new Friendship
            {
                SenderId = blocker.Id, ReceiverId = blocked.Id,
                Status = FriendshipStatus.Blocked,
            };
            _context.Accounts.AddRange(blocker, blocked);
            _context.Friendships.Add(row);
            _context.SaveChanges();

            _service.Unblock(blocker.Id.ToString(), blocked.Id.ToString());

            _context.Friendships.Any(f => f.Id == row.Id).Should().BeFalse();
        }

        [Fact]
        public void Unblock_NotBlocked_Throws()
        {
            var a = EntityBuilder.NewAccount();
            var b = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(a, b);
            _context.SaveChanges();

            var act = () => _service.Unblock(a.Id.ToString(), b.Id.ToString());

            act.Should().Throw<AppException>();
        }


        [Fact]
        public void GetRelationship_Self_ReturnsIsSelf()
        {
            var user = EntityBuilder.NewAccount();
            _context.Accounts.Add(user);
            _context.SaveChanges();

            var result = _service.GetRelationship(user.Id.ToString(), user.Id.ToString());

            result.IsSelf.Should().BeTrue();
        }

        [Fact]
        public void GetRelationship_NoRelationship_ReturnsNullStatus()
        {
            var a = EntityBuilder.NewAccount();
            var b = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(a, b);
            _context.SaveChanges();

            var result = _service.GetRelationship(a.Id.ToString(), b.Id.ToString());

            result.Status.Should().BeNull();
            result.IsSelf.Should().BeFalse();
        }

        [Fact]
        public void GetRelationship_ShadowBlock_HidesBlock()
        {
            // other blocked current, current must not see the block.
            var current = EntityBuilder.NewAccount();
            var other = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(current, other);
            _context.Friendships.Add(new Friendship
            {
                SenderId = other.Id, ReceiverId = current.Id,
                Status = FriendshipStatus.Blocked,
            });
            _context.SaveChanges();

            var result = _service.GetRelationship(current.Id.ToString(), other.Id.ToString());

            result.Status.Should().BeNull();
        }

        [Fact]
        public void GetRelationship_AcceptedFriend_ReturnsAccepted()
        {
            var a = EntityBuilder.NewAccount();
            var b = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(a, b);
            _context.Friendships.Add(new Friendship
            {
                SenderId = a.Id, ReceiverId = b.Id,
                Status = FriendshipStatus.Accepted,
            });
            _context.SaveChanges();

            var result = _service.GetRelationship(a.Id.ToString(), b.Id.ToString());

            result.Status.Should().Be(FriendshipStatus.Accepted);
            result.IsCurrentUserSender.Should().BeTrue();
        }


        [Fact]
        public void GetFriends_ReturnsOnlyAccepted()
        {
            var u = EntityBuilder.NewAccount();
            var friend = EntityBuilder.NewAccount();
            var pendingPartner = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(u, friend, pendingPartner);
            _context.Friendships.AddRange(
                new Friendship { SenderId = u.Id, ReceiverId = friend.Id, Status = FriendshipStatus.Accepted },
                new Friendship { SenderId = u.Id, ReceiverId = pendingPartner.Id, Status = FriendshipStatus.Pending });
            _context.SaveChanges();

            var result = _service.GetFriends(u.Id.ToString()).ToList();

            result.Should().ContainSingle();
        }

        [Fact]
        public void GetIncomingRequests_OnlyPendingToUser()
        {
            var u = EntityBuilder.NewAccount();
            var other = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(u, other);
            _context.Friendships.AddRange(
                new Friendship { SenderId = other.Id, ReceiverId = u.Id, Status = FriendshipStatus.Pending },
                new Friendship { SenderId = u.Id, ReceiverId = other.Id, Status = FriendshipStatus.Pending });
            _context.SaveChanges();

            var result = _service.GetIncomingRequests(u.Id.ToString()).ToList();

            result.Should().ContainSingle();
        }

        [Fact]
        public void GetOutgoingRequests_OnlyPendingFromUser()
        {
            var u = EntityBuilder.NewAccount();
            var other = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(u, other);
            _context.Friendships.AddRange(
                new Friendship { SenderId = other.Id, ReceiverId = u.Id, Status = FriendshipStatus.Pending },
                new Friendship { SenderId = u.Id, ReceiverId = other.Id, Status = FriendshipStatus.Pending });
            _context.SaveChanges();

            var result = _service.GetOutgoingRequests(u.Id.ToString()).ToList();

            result.Should().ContainSingle();
        }

        [Fact]
        public void GetBlocked_OnlyBlockedFromUser()
        {
            var u = EntityBuilder.NewAccount();
            var other = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(u, other);
            _context.Friendships.Add(new Friendship
            {
                SenderId = u.Id, ReceiverId = other.Id, Status = FriendshipStatus.Blocked
            });
            _context.SaveChanges();

            _service.GetBlocked(u.Id.ToString()).Should().ContainSingle();
        }

        [Fact]
        public void CountIncomingRequests_Returns()
        {
            var u = EntityBuilder.NewAccount();
            var a = EntityBuilder.NewAccount();
            var b = EntityBuilder.NewAccount();
            _context.Accounts.AddRange(u, a, b);
            _context.Friendships.AddRange(
                new Friendship { SenderId = a.Id, ReceiverId = u.Id, Status = FriendshipStatus.Pending },
                new Friendship { SenderId = b.Id, ReceiverId = u.Id, Status = FriendshipStatus.Pending });
            _context.SaveChanges();

            _service.CountIncomingRequests(u.Id.ToString()).Should().Be(2);
        }
    }
}
