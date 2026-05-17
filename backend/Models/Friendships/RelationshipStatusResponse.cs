using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Models.Friendships
{
    public class RelationshipStatusResponse
    {
        public FriendshipStatus? Status { get; set; }

        public bool IsCurrentUserSender { get; set; }
        public bool IsSelf { get; set; }
    }
}