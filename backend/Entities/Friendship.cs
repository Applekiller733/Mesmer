using System.ComponentModel.DataAnnotations;
using MassTransit;
using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Entities
{
    /// <summary>
    /// One directional edge in the friendship graph, from Sender to Receiver.
    ///
    /// Cardinality:
    ///   - For pending and accepted friendships, one row exists per pair —
    ///     whoever sent the request is the Sender, the other is the
    ///     Receiver. This matters for displaying "X sent you a friend
    ///     request" semantically.
    ///   - For blocking, there can be one or two rows depending on whether
    ///     the block is mutual. We don't auto-create reciprocal Blocked
    ///     rows; that's a separate user action.
    ///
    /// Constraints (enforced via composite unique index in the migration):
    ///   - At most one row per (SenderId, ReceiverId) pair.
    ///   - SenderId != ReceiverId. (Self-friendship is meaningless.)
    /// </summary>
    public class Friendship
    {
        [Key]
        public Guid Id { get; set; } = NewId.NextSequentialGuid();

        /// <summary>
        /// The user who initiated this row. For Pending and Accepted, this
        /// is the user who sent the friend request. For Blocked, this is
        /// the user doing the blocking.
        /// </summary>
        public Guid SenderId { get; set; }
        public Account Sender { get; set; }

        /// <summary>
        /// The user on the other end of the relationship.
        /// </summary>
        public Guid ReceiverId { get; set; }
        public Account Receiver { get; set; }

        /// <summary>
        /// Current state. See FriendshipStatus for transition rules.
        /// </summary>
        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        /// <summary>
        /// When this row was created (request sent, or block applied).
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the status last changed — e.g. when a Pending request
        /// became Accepted. Null until the first transition.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}