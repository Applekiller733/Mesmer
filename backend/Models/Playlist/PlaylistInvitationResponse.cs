using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Models.PlaylistInvitations
{
    public class PlaylistInvitationResponse
    {
        public string Id { get; set; }

        public string PlaylistId { get; set; }
        public string PlaylistName { get; set; }
        public PlaylistVisibility PlaylistVisibility { get; set; }

        public string SenderId { get; set; }
        public string SenderUserName { get; set; }
        public string SenderFriendCode { get; set; }

        public string ReceiverId { get; set; }
        public string ReceiverUserName { get; set; }
        public string ReceiverFriendCode { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}