namespace SongAppApi.Models.Friendships
{
    public class FriendshipResponse
    {
        public string Id { get; set; }

        public string SenderId { get; set; }
        public string SenderUserName { get; set; }
        public string SenderFriendCode { get; set; }

        public string ReceiverId { get; set; }
        public string ReceiverUserName { get; set; }
        public string ReceiverFriendCode { get; set; }

        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

}