using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Repositories;
using Repositories.Implementations;
using Repositories.Interfaces;
using Repositories.Models;

namespace API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserApiController : ControllerBase
    {
        private readonly IUserInterface _user;
        private readonly IEmailInterface _email;
        private readonly IOtpInterface _otp;
        private readonly IConfiguration _configuration;




        private readonly IWebHostEnvironment webHost;
        public UserApiController(IUserInterface userInterface, IWebHostEnvironment webHostEnvironment, IEmailInterface email, IOtpInterface otpInterface, IConfiguration configuration)
        {
            _user = userInterface;
            webHost = webHostEnvironment;
            _email = email;
            _otp = otpInterface;
            _configuration = configuration;
        }

        // =========================
        // LOGIN API
        // =========================
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromForm] t_user user)
        {

            if (user.imageform != null && user.imageform.Length > 0)
            {
                var filename = user.c_email + Path.GetExtension(user.imageform.FileName);
                Console.WriteLine(filename);

                string mvcJdPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "..",        // move out of API
                        "MVC",       // MVC project folder name
                        "wwwroot",
                        "uploads"
                    );
                var filefolder = Path.Combine(mvcJdPath, "profile_images");
                if (!Directory.Exists(filefolder))
                {
                    Directory.CreateDirectory(filefolder);
                }
                string filepath = Path.Combine(filefolder, filename);
                Console.WriteLine(filepath);
                using (var stream = new FileStream(filepath, FileMode.Create))
                {
                    await user.imageform.CopyToAsync(stream);
                }
                user.c_profile_image = filename;
            }
            else
            {
                user.c_profile_image = "default.jpg";
            }
            int newUserId = await _user.RegisterUser(user);

            if (newUserId <= 0)
            {
                return BadRequest(new { success = false, message = "Registration failed" });
            }

            // 🔥 SET THE USER ID
            user.c_userid = newUserId;

            var jwtSection = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["Key"])
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
                    {
                        new Claim("userid", newUserId.ToString()),
                        new Claim(ClaimTypes.Name, user.c_name ?? ""),
                        new Claim(ClaimTypes.Email, user.c_email),
                        new Claim(ClaimTypes.Role, user.c_role ?? "")
                    };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: creds
            );

            return Ok(new
            {
                success = true,
                token = new JwtSecurityTokenHandler().WriteToken(token),
                user = new
                {
                    userid = newUserId,
                    name = user.c_name,
                    email = user.c_email,
                    role = user.c_role
                }
            });


        }

        [HttpGet]
        [Route("SendOtp")]
        public async Task<IActionResult> SendOtp(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email is required");

            await _otp.GenerateAndSendOtpAsync(email);
            return Ok(new { success = true, message = "OTP sent successfully" });
        }


        [HttpPost("VerifyOtp")]
        public IActionResult VerifyOtp([FromBody] t_otprequest request)
        {

            if (request == null || string.IsNullOrWhiteSpace(request.c_email))
                return BadRequest("Invalid request");

            if (!_otp.VerifyOtp(request.c_email, request.c_otp, out string message))
                return BadRequest(message);

            return Ok(message);
        }

        [HttpGet("CheckEmailExists")]
        public async Task<IActionResult> CheckEmailExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { success = false, message = "Email is required" });

            var result = await _user.GetUser($"AND c_email = '{email}'");


            bool exists = result.Count > 0;



            return Ok(new
            {
                success = !exists,
                message = exists ? "Email already exists" : ""
            });
        }

        [HttpGet("CheckContactNoExists")]
        public async Task<IActionResult> CheckContactNoExists(string contactno)
        {
            if (string.IsNullOrWhiteSpace(contactno))
                return BadRequest(new { success = false, message = "Contact No is required" });

            var result = await _user.GetUser($"AND c_contactno = '{contactno}'");


            bool exists = result.Count > 0;

            return Ok(new
            {
                success = !exists,
                message = exists ? "Contact No Already Exists" : ""
            });
        }


        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromForm] vm_Login model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid data" });

            var user = (await _user.GetUser($"AND c_email = '{model.c_email}'")).FirstOrDefault();
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            var hasher = new PasswordHasher<object>();
            var passwordVerificationResult = hasher.VerifyHashedPassword(null, user.c_password, model.c_password);

            if (passwordVerificationResult == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Invalid email or password" });

            var jwtSection = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["Key"])
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
    {
        new Claim("userid", user.c_userid.ToString()),
        new Claim(ClaimTypes.Name, user.c_name ?? ""),
        new Claim(ClaimTypes.Email, user.c_email),
        new Claim(ClaimTypes.Role, user.c_role ?? "")
    };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: creds
            );

            return Ok(new
            {
                success = true,
                message = "Login successful",
                token = new JwtSecurityTokenHandler().WriteToken(token),
                user = new
                {
                    userid = user.c_userid,
                    name = user.c_name,
                    email = user.c_email,
                    role = user.c_role
                }
            });
        }

        [HttpGet("CheckEmailExistsForgotPassword")]
        public async Task<IActionResult> CheckEmailExistsForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { success = false, message = "Email is required" });

            var result = await _user.GetUser($"AND c_email = '{email}'");




            if (result.Count > 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "Sending OTP",

                });
            }
            else
            {

                return BadRequest(new
                {
                    success = false,
                    message = "Email has not been registered yet!"
                });
            }

        }


        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromForm] t_resetpassword resetpassword)
        {
            var result = await _user.ResetPassword(resetpassword);

            if (result == 1)
            {
                return Ok(new { success = true, message = "Your password has been reset successfully! You can now log in with your new password" });

            }
            else
            {
                return BadRequest(new { success = false, message = "Error in Reseting password(applicant)" });

            }

        }

        [HttpGet]
        [Route("SendWelcomeMail")]
        public async Task<IActionResult> SendWelcomeMail(string email, string username)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email is required");

            await _email.SendWelcomeEmailAsync(email, username);
            return Ok(new { success = true, message = "Welcome Mail Sent successfully" });
        }

        [HttpGet("GetUserByEmail")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var result = await _user.GetUser($"AND c_email='{email}';");
            if (result.Count == 0)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            var user = result.First();

            return Ok(new
            {
                success = true,
                user = new
                {
                    userid = user.c_userid,
                    name = user.c_name,
                    email = user.c_email,
                    contactno = user.c_contactno,
                    address = user.c_address,
                    profile_image = user.c_profile_image,
                    role = user.c_role,
                    status = user.c_status,
                    dob = user.c_dob
                }
            });
        }


        // apicontroller
    [Authorize]
[HttpPost("ChangePassword")]
public async Task<IActionResult> ChangePassword([FromForm] vm_changePassword changePassword)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // 🔐 Get userId from JWT (custom claim)
    var userIdClaim = User.FindFirst("userid")?.Value;

    if (string.IsNullOrEmpty(userIdClaim))
        return Unauthorized(new { message = "Invalid token" });

    if (!int.TryParse(userIdClaim, out int userId))
        return Unauthorized(new { message = "Invalid user id in token" });

    // 🔎 Get stored hashed password
    var storedHashedPassword = await _user.GetPasswordByUserId(userId);

    if (string.IsNullOrEmpty(storedHashedPassword))
    {
        return BadRequest(new
        {
            success = false,
            message = "User not found"
        });
    }

    // 🔐 Verify old password
    var hasher = new PasswordHasher<object>();
    var verifyResult = hasher.VerifyHashedPassword(
        null,
        storedHashedPassword,
        changePassword.c_oldpassword
    );

    if (verifyResult == PasswordVerificationResult.Failed)
    {
        return Unauthorized(new
        {
            success = false,
            message = "Invalid old password"
        });
    }

    // 🔁 Update password
    var result = await _user.ChangePassword(userId, changePassword);

    if (result == 1)
    {
        return Ok(new
        {
            success = true,
            message = "Your password has been changed successfully!"
        });
    }

    return BadRequest(new
    {
        success = false,
        message = "Error while changing the password"
    });
}

}

    }

