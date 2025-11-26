namespace SongAppApi.Models.Accounts
{
    using SongAppApi.Entities;
    public class AccountProfileResponse
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        //public File? ProfilePicture { get; set; }
    }
}
