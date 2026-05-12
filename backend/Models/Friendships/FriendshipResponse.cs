namespace SongAppApi.Models.Friendships
{
    public class FriendshipResponse
    {
        public string Id { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public int Status { get; set; }      // FriendshipStatus enum value
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

}
