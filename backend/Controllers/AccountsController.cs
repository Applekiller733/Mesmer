namespace SongAppApi.Controllers
{
    using Microsoft.AspNetCore.Identity.Data;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.StaticFiles;
    using SongAppApi.Authorization;
    using SongAppApi.Entities;
    using SongAppApi.Models.Accounts;
    using SongAppApi.Services;

    [Authorization.Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AccountsController : BaseController
    {
        private readonly IAccountService _accountService;
        private readonly IFileService _fileService;

        public AccountsController(IAccountService accountService, IFileService fileService)
        {
            _accountService = accountService;
            _fileService = fileService;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        {
            try
            {
                var response = _accountService.Authenticate(model, ipAddress());
                setTokenCookie(response.RefreshToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public ActionResult<AuthenticateResponse> RefreshToken()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];
                var response = _accountService.RefreshToken(refreshToken, ipAddress());
                setTokenCookie(response.RefreshToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("revoke-token")]
        public IActionResult RevokeToken()
        {
            try
            {
                // accept token from request body or cookie
                var token = Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(token))
                    return BadRequest(new { message = "Token is required" });

                // users can revoke their own tokens and admins can revoke any tokens
                if (!Account.OwnsToken(token) && Account.Role != Role.Admin)
                    return Unauthorized(new { message = "Unauthorized" });

                _accountService.RevokeToken(token, ipAddress());
                return Ok(new { message = "Token revoked" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register(Models.Accounts.RegisterRequest model)
        {
            try
            {
                _accountService.Register(model, Request.Headers["origin"]);
                return Ok(new
                    { message = "Registration successful, please check your email for verification instructions" });

            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("verify-email")]
        public IActionResult VerifyEmail(VerifyEmailRequest model)
        {
            try
            {
                _accountService.VerifyEmail(model.Token);
                return Ok(new { message = "Verification successful, you can now login" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword(Models.Accounts.ForgotPasswordRequest model)
        {
            try
            {
                _accountService.ForgotPassword(model, Request.Headers["origin"]);
                return Ok(new { message = "Please check your email for password reset instructions" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("validate-reset-token")]
        public IActionResult ValidateResetToken(ValidateResetTokenRequest model)
        {
            try
            {
                _accountService.ValidateResetToken(model);
                return Ok(new { message = "Token is valid" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public IActionResult ResetPassword(Models.Accounts.ResetPasswordRequest model)
        {
            try
            {
                _accountService.ResetPassword(model);
                return Ok(new { message = "Password reset successful, you can now login" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Role.Admin)]
        [HttpGet]
        public ActionResult<IEnumerable<AccountResponse>> GetAll()
        {
            try
            {
                var accounts = _accountService.GetAll();
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public ActionResult<AccountResponse> GetById(string id)
        {
            try
            {
                // users can get their own account and admins can get any account
                if (id != Account.Id.ToString() && Account.Role != Role.Admin)
                    return Unauthorized(new { message = "Unauthorized" });

                var account = _accountService.GetById(id);
                return Ok(account);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("profile/{id}")]
        public ActionResult<AccountProfileResponse> GetProfileById(string id)
        {
            try
            {
                var profile = _accountService.GetProfileById(id);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("profile/{id}/picture")]
        public ActionResult<AccountProfilePictureResponse> GetProfilePictureById(string id)
        {
            try
            {
                Console.WriteLine(id);

                var fileid = _accountService.GetProfilePictureId(id);
                if (fileid == null)
                {
                    Console.WriteLine("File ID IS NULL");
                    return NotFound();
                }

                var profilepicture = _fileService.GetFileById((string)fileid);
                //Console.WriteLine(System.IO.File.Exists(profilepicture.FilePath));
                if (profilepicture == null || !System.IO.File.Exists(profilepicture.FilePath))
                    return NotFound();

                var stream = new FileStream(profilepicture.FilePath, FileMode.Open, FileAccess.Read);
                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(profilepicture.FileName, out var contentType))
                    contentType = "application/octet-stream";
                return File(stream, contentType, profilepicture.FileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Role.Admin)]
        [HttpPost]
        public ActionResult<AccountResponse> Create(CreateRequest model)
        {
            try
            {
                var account = _accountService.Create(model);
                return Ok(account);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        public ActionResult<AccountResponse> Update([FromForm] UpdateRequest model)
        {
            try
            {
                //todo ensure new GUID name is created for each file
                // users can update their own account and admins can update any account

                //todo ensure the function works with the changed UpdateRequest
                if (model.Id != Account.Id.ToString() && Account.Role != Role.Admin)
                    return Unauthorized(new { message = "Unauthorized" });

                //if (model.Id == null)
                //    return BadRequest(new { message = "Id cannot be null"});

                // only admins can update role
                if (Account.Role != Role.Admin)
                    model.Role = null;

                if (model.ProfilePicture != null)
                {
                    //Console.WriteLine("MODEL.PROFILEPICTURE IS -NOT- NULL");
                    var filenameGUID = Guid.NewGuid().ToString();
                    model.ProfilePicture.FileName = filenameGUID;
                    var subfolderpath = Path.Combine("ProfilePictures",
                        Account.Id.ToString());
                    //_fileService.CreateSubFolders(subfolderpath);
                    //model.ProfilePicture.FileName = newfilename;
                    var fileid = _fileService.Create(model.ProfilePicture, subfolderpath);
                    var file = _fileService.GetFileById(fileid);
                    var account = _accountService.Update(model.Id, model, file);
                    return Ok(account);
                }
                else
                {
                    Console.WriteLine("MODEL.PROFILEPICTURE IS NULL");
                    var account = _accountService.Update(model.Id, model);
                    return Ok(account);
                }
                //todo maybe refactor

                //return Ok(account);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            try
            {
                // users can delete their own account and admins can delete any account
                if (id != Account.Id.ToString() && Account.Role != Role.Admin)
                    return Unauthorized(new { message = "Unauthorized" });

                _accountService.Delete(id);
                return Ok(new { message = "Account deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // helper methods

        private void setTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string ipAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
    }
}
