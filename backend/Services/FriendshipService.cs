using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Friendships;

namespace SongAppApi.Services
{
    public interface IFriendshipService
    {
        FriendshipResponse SendRequest(string senderId, string receiverId);
        FriendshipResponse AcceptRequest(string currentUserId, string friendshipId);
        void DeclineRequest(string currentUserId, string friendshipId);
        void RemoveFriend(string currentUserId, string otherUserId);
        FriendshipResponse Block(string blockerId, string blockedId);
        void Unblock(string blockerId, string blockedId);

        RelationshipStatusResponse GetRelationship(string currentUserId, string otherUserId);
        IEnumerable<FriendshipResponse> GetFriends(string userId);
        IEnumerable<FriendshipResponse> GetIncomingRequests(string userId);
        IEnumerable<FriendshipResponse> GetOutgoingRequests(string userId);
        IEnumerable<FriendshipResponse> GetBlocked(string userId);
        int CountIncomingRequests(string userId);
    }

    public class FriendshipService : IFriendshipService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public FriendshipService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }


        public FriendshipResponse SendRequest(string senderId, string receiverId)
        {
            var (senderGuid, receiverGuid) = ParsePair(senderId, receiverId);

            if (senderGuid == receiverGuid)
                throw new AppException("You cannot send a friend request to yourself.");

            // Confirm both users actually exist. Catches typos /
            // crafted requests that would otherwise insert a row with
            // dangling foreign-key intent.
            EnsureUserExists(receiverGuid, "The recipient does not exist.");

            // Check for an existing row in either direction.
            var existing = FindAnyRow(senderGuid, receiverGuid);
            if (existing != null)
            {
                switch (existing.Status)
                {
                    case FriendshipStatus.Accepted:
                        throw new AppException("You are already friends.");

                    case FriendshipStatus.Pending:
                        // Two cases:
                        //  - Sender already sent a request: idempotent, return it.
                        //  - Receiver already sent a request to sender: tell
                        //    them to accept rather than sending a new one.
                        if (existing.SenderId == senderGuid)
                            return _mapper.Map<FriendshipResponse>(existing);
                        throw new AppException(
                            "This user has already sent you a request. Accept it instead.");

                    case FriendshipStatus.Blocked:
                        // Two sub-cases:
                        //  - The sender did the blocking → fail with a clear error.
                        //  - The receiver did the blocking → silent success
                        //    (shadow block; don't reveal the block).
                        if (existing.SenderId == senderGuid)
                            throw new AppException(
                                "You have blocked this user. Unblock them first.");
                        // Receiver blocked the sender. Pretend the request
                        // went through but don't actually create a row.
                        return new FriendshipResponse
                        {
                            SenderId = senderId,
                            ReceiverId = receiverId,
                            Status = FriendshipStatus.Pending,
                            CreatedAt = DateTime.UtcNow,
                        };
                }
            }

            var friendship = new Friendship
            {
                SenderId = senderGuid,
                ReceiverId = receiverGuid,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Friendships.Add(friendship);
            _context.SaveChanges();

            return _mapper.Map<FriendshipResponse>(friendship);
        }

        public FriendshipResponse AcceptRequest(string currentUserId, string friendshipId)
        {
            var currentGuid = ParseGuid(currentUserId, "currentUserId");
            var fGuid = ParseGuid(friendshipId, "friendshipId");

            var row = _context.Friendships.FirstOrDefault(f => f.Id == fGuid)
                      ?? throw new KeyNotFoundException("Friend request not found.");

            // Authority: only the receiver can accept.
            if (row.ReceiverId != currentGuid)
                throw new AppException("Only the request recipient can accept it.");

            if (row.Status != FriendshipStatus.Pending)
                throw new AppException(
                    $"This request is in state {row.Status} and cannot be accepted.");

            row.Status = FriendshipStatus.Accepted;
            row.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return _mapper.Map<FriendshipResponse>(row);
        }

        public void DeclineRequest(string currentUserId, string friendshipId)
        {
            var currentGuid = ParseGuid(currentUserId, "currentUserId");
            var fGuid = ParseGuid(friendshipId, "friendshipId");

            var row = _context.Friendships.FirstOrDefault(f => f.Id == fGuid)
                      ?? throw new KeyNotFoundException("Friend request not found.");

            if (row.ReceiverId != currentGuid)
                throw new AppException("Only the request recipient can decline it.");

            if (row.Status != FriendshipStatus.Pending)
                throw new AppException(
                    "Only pending requests can be declined.");

            _context.Friendships.Remove(row);
            _context.SaveChanges();
        }

        public void RemoveFriend(string currentUserId, string otherUserId)
        {
            var (a, b) = ParsePair(currentUserId, otherUserId);

            // The friendship row could be in either direction. We don't
            // care who originally sent the request — if we're friends,
            // either party can dissolve it.
            var row = FindAnyRow(a, b);
            if (row == null || row.Status != FriendshipStatus.Accepted)
                throw new AppException("You are not friends with this user.");

            _context.Friendships.Remove(row);
            _context.SaveChanges();
        }

        public FriendshipResponse Block(string blockerId, string blockedId)
        {
            var (blocker, blocked) = ParsePair(blockerId, blockedId);

            if (blocker == blocked)
                throw new AppException("You cannot block yourself.");

            EnsureUserExists(blocked, "The user to block does not exist.");

            // Remove ANY existing row between the two users — pending
            // requests, accepted friendship, or a stale block in the other
            // direction. Block is the highest-precedence state, so we
            // wipe the slate before inserting.
            var existing = _context.Friendships
                .Where(f =>
                    (f.SenderId == blocker && f.ReceiverId == blocked) ||
                    (f.SenderId == blocked && f.ReceiverId == blocker))
                .ToList();

            // If the blocker already has a Blocked row in their direction,
            // it's idempotent — return what's there.
            var alreadyBlockedByMe = existing.FirstOrDefault(f =>
                f.SenderId == blocker && f.Status == FriendshipStatus.Blocked);
            if (alreadyBlockedByMe != null)
                return _mapper.Map<FriendshipResponse>(alreadyBlockedByMe);

            // Otherwise, remove all existing rows and insert the new
            // Blocked row.
            _context.Friendships.RemoveRange(existing);

            var newBlock = new Friendship
            {
                SenderId = blocker,
                ReceiverId = blocked,
                Status = FriendshipStatus.Blocked,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Friendships.Add(newBlock);
            _context.SaveChanges();

            return _mapper.Map<FriendshipResponse>(newBlock);
        }

        public void Unblock(string blockerId, string blockedId)
        {
            var (blocker, blocked) = ParsePair(blockerId, blockedId);

            var row = _context.Friendships.FirstOrDefault(f =>
                f.SenderId == blocker &&
                f.ReceiverId == blocked &&
                f.Status == FriendshipStatus.Blocked);

            if (row == null)
                throw new AppException("You have not blocked this user.");

            _context.Friendships.Remove(row);
            _context.SaveChanges();

            // Note: we intentionally do NOT restore any prior friendship.
            // If A blocked B and now unblocks, B is just back to "stranger"
            // — they'd have to re-send a friend request. This matches the
            // expected behavior of every social platform.
        }

        // -------------------- Queries --------------------

        public RelationshipStatusResponse GetRelationship(string currentUserId, string otherUserId)
        {
            var currentGuid = ParseGuid(currentUserId, "currentUserId");
            var otherGuid = ParseGuid(otherUserId, "otherUserId");

            if (currentGuid == otherGuid)
                return new RelationshipStatusResponse { IsSelf = true };

            // From the current user's perspective, we want the LOGICAL
            // relationship — including "I am the receiver of a pending
            // request" — not just rows where they're the Sender. So
            // we look for any row in either direction.
            //
            // Important: we must filter out rows where the OTHER user
            // has blocked the current user. Returning Status=Blocked
            // would reveal the block; we instead return null (= no
            // relationship), making it look like a stranger.
            var row = FindAnyRow(currentGuid, otherGuid);

            if (row == null)
                return new RelationshipStatusResponse();

            // Shadow-block visibility rule. If the OTHER user is the
            // sender of a Blocked row, current user shouldn't see the
            // block — pretend no relationship exists.
            if (row.Status == FriendshipStatus.Blocked && row.SenderId == otherGuid)
                return new RelationshipStatusResponse();

            return new RelationshipStatusResponse
            {
                Status = row.Status,
                IsCurrentUserSender = row.SenderId == currentGuid,
                IsSelf = false,
            };
        }

        public IEnumerable<FriendshipResponse> GetFriends(string userId)
        {
            var guid = ParseGuid(userId, "userId");

            var rows = _context.Friendships
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .Where(f => f.Status == FriendshipStatus.Accepted &&
                            (f.SenderId == guid || f.ReceiverId == guid))
                .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
                .ToList();

            return _mapper.Map<IEnumerable<FriendshipResponse>>(rows);
        }

        public IEnumerable<FriendshipResponse> GetIncomingRequests(string userId)
        {
            var guid = ParseGuid(userId, "userId");

            var rows = _context.Friendships
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .Where(f => f.Status == FriendshipStatus.Pending && f.ReceiverId == guid)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            return _mapper.Map<IEnumerable<FriendshipResponse>>(rows);
        }

        public IEnumerable<FriendshipResponse> GetOutgoingRequests(string userId)
        {
            var guid = ParseGuid(userId, "userId");

            var rows = _context.Friendships
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .Where(f => f.Status == FriendshipStatus.Pending && f.SenderId == guid)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            return _mapper.Map<IEnumerable<FriendshipResponse>>(rows);
        }

        public IEnumerable<FriendshipResponse> GetBlocked(string userId)
        {
            var guid = ParseGuid(userId, "userId");

            var rows = _context.Friendships
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .Where(f => f.Status == FriendshipStatus.Blocked && f.SenderId == guid)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            return _mapper.Map<IEnumerable<FriendshipResponse>>(rows);
        }


        public int CountIncomingRequests(string userId)
        {
            var guid = ParseGuid(userId, "userId");
            return _context.Friendships.Count(f =>
                f.Status == FriendshipStatus.Pending && f.ReceiverId == guid);
        }

        // helpers

        /// <summary>
        /// Parses two user IDs into Guids. Throws AppException with a
        /// friendly message on parse failure rather than letting Guid.Parse
        /// propagate FormatException.
        /// </summary>
        private static (Guid, Guid) ParsePair(string first, string second)
        {
            return (ParseGuid(first, "first"), ParseGuid(second, "second"));
        }

        private static Guid ParseGuid(string value, string paramName)
        {
            if (!Guid.TryParse(value, out var guid))
                throw new AppException($"{paramName} is not a valid id.");
            return guid;
        }

        private void EnsureUserExists(Guid id, string notFoundMessage)
        {
            if (!_context.Accounts.Any(a => a.Id == id))
                throw new KeyNotFoundException(notFoundMessage);
        }

        /// <summary>
        /// Finds a Friendship row between two users in either direction.
        /// Returns null if none exists. Used by all the
        /// "is there any relationship" checks.
        /// </summary>
        private Friendship? FindAnyRow(Guid a, Guid b)
        {
            return _context.Friendships.FirstOrDefault(f =>
                (f.SenderId == a && f.ReceiverId == b) ||
                (f.SenderId == b && f.ReceiverId == a));
        }
    }
}