namespace SongAppApi.Models.Friendships
{
    public class RelationshipStatusResponse
    {
        public int? Status { get; set; }
        public bool IsCurrentUserSender { get; set; }
        public bool IsSelf { get; set; }
    }

}
