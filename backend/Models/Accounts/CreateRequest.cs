namespace SongAppApi.Models.Accounts
{
    using SongAppApi.Entities;
    using System.ComponentModel.DataAnnotations;
    public class CreateRequest
    {

        [Required]
        public string UserName { get; set; }

        //[Required]
        //public string LastName { get; set; }

        [Required]
        [EnumDataType(typeof(Role))]
        public string Role { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        //include file profilepicture?
    }
}
