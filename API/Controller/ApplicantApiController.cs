using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Repositories.Interfaces;
using Repositories.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Nest;

namespace API.Controllers
{

    [Authorize(Roles = "applicant")]
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicantApiController : ControllerBase
    {

        private readonly IElasticClient _elasticClient;

        private readonly IElasticsearchInterface _elasticsearchService;
        private readonly IUserInterface _user;
        private readonly IApplicantInterface _applicantRepo;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ApplicantApiController> _logger;
        public ApplicantApiController(IApplicantInterface applicantRepo, IWebHostEnvironment env, IElasticsearchInterface elasticsearchService, ILogger<ApplicantApiController> logger, IUserInterface user, IElasticClient elasticClient = null)
        {
            _applicantRepo = applicantRepo;
            _env = env;
            _logger = logger;
            _user = user;
            _elasticClient = elasticClient;
            _elasticsearchService = elasticsearchService;
        }

        //===========================APPLY PAGE PRANAV=================================================================================================================
        [HttpGet("apply/{jobId}")]
        public async Task<IActionResult> GetApplyJobDetails(int jobId)
        {
            try
            {
                var data = await _applicantRepo.GetApplyJobDetailsAsync(jobId);

                if (data == null)
                    return NotFound("Job not found");

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        //newwwwwwwwwwww
        // Check status
        [HttpGet("status/{jobId}")]
        public async Task<IActionResult> CheckApplicationStatus(int jobId)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid")!.Value);

                var status = await _applicantRepo
                    .GetLatestApplicationStatusAsync(userId, jobId);

                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }



        // Apply job
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyJob([FromForm] t_applications model)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid")!.Value);

                //  Check existing application status
                string existingStatus = null;
                try
                {
                    existingStatus = await _applicantRepo.GetLatestApplicationStatusAsync(userId, model.c_job_id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Status check warning: {ex.Message}");
                }

                //  IMPORTANT: Block if pending or approved
                if (!string.IsNullOrEmpty(existingStatus))
                {
                    if (existingStatus.ToLower() == "pending")
                    {
                        return BadRequest("You have a pending application for this job.");
                    }

                    if (existingStatus.ToLower() == "accepted")
                    {
                        return BadRequest("Your application has already been approved.");
                    }

                    // If rejected, allow to proceed (no error)
                }

                model.c_userid = userId;

                //  Validate resume file
                if (model.resumeform == null || model.resumeform.Length == 0)
                {
                    return BadRequest("Resume file is required.");
                }

                if (model.resumeform.Length > 5 * 1024 * 1024)
                {
                    return BadRequest("Resume file must be less than 5MB.");
                }

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(model.resumeform.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Only PDF, DOC, and DOCX files are allowed.");
                }

                //  Save resume file
                // string resumePath = Path.Combine("wwwroot", "resume");
                string mvcRoot = Path.Combine(
    Directory.GetCurrentDirectory(),
    "..",        // out of API
    "MVC",       // MVC project folder
    "wwwroot"
);

                string resumePath = Path.Combine(mvcRoot, "uploads", "resumes");
                Directory.CreateDirectory(resumePath);

                string fileName = $"{Guid.NewGuid()}{fileExtension}";
                string filePath = Path.Combine(resumePath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.resumeform.CopyToAsync(stream);
                }

                model.c_resume_file = fileName;

                //  Apply for job
                int applicationId = await _applicantRepo.ApplyJobAsync(model);

                string message = existingStatus?.ToLower() == "rejected"
                    ? "Re-application submitted successfully!"
                    : "Job applied successfully!";

                return Ok(new
                {
                    message = message,
                    applicationId = applicationId,
                    status = "pending",
                    isReapplication = existingStatus?.ToLower() == "rejected"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        //Preview by applicationId 
        [HttpGet("preview/{applicationId}")]
        public async Task<IActionResult> GetApplicationPreview(int applicationId)
        {
            try
            {
                var preview = await _applicantRepo
                    .GetApplicationPreviewByIdAsync(applicationId);

                if (preview == null)
                    return NotFound("Application not found");

                return Ok(preview);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        //===========================APPLY PAGE PRANAV END=================================================================================================================




        //======================COMPANY Review ======================================================================================================================

        [HttpPost]
        [Route("GiveCompanyReview")]

        public async Task<IActionResult> GiveCompanyReview(
    [FromForm] t_company_review review,
    [FromForm] int jobId
)
        {
            try
            {

                int userId = int.Parse(User.FindFirst("userid")!.Value);
                review.c_userid = userId;

                bool isAccepted = await _applicantRepo.IsJobAccepted(userId, jobId);
                if (!isAccepted)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "You can only review companies for accepted jobs"
                    });
                }


                int companyId = await _applicantRepo.GetCompanyIdByJobId(jobId);
                review.c_company_id = companyId;

                var result = await _applicantRepo.GiveCompanyReview(review);

                return result switch
                {
                    1 => Ok(new { success = true, message = "Review submitted successfully" }),
                    0 => BadRequest(new { success = false, message = "Review not inserted" }),
                    -1 => BadRequest(new { success = false, message = "Error while saving review" }),
                    _ => BadRequest(new { success = false, message = "Unexpected error" })
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        //======================COMPANY Review END=====================================================================================================================


        // Dev UserDetails POPUP S=============================================================================================

        // ADD POPUP DETAILS (TEMP EMAIL LOGIC)

        [HttpPost("add")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddDetails([FromForm] t_userdetails model)
        {
            var userIdClaim = User.FindFirst("userid");
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            model.c_userid = userId;

            // ===============================
            // RESUME UPLOAD (PDF ONLY)
            // ===============================
            if (model.resumeform != null && model.resumeform.Length > 0)
            {
                string ext = Path.GetExtension(model.resumeform.FileName).ToLower();
                if (ext != ".pdf")
                    return BadRequest("Only PDF resumes are allowed");

                var currentUser = await _applicantRepo.GetUserForProfileAsync(userId);
                if (currentUser == null || string.IsNullOrEmpty(currentUser.c_email))
                    return BadRequest("User email not found");

                string resumeFolderPath = Path.Combine(
                    _env.ContentRootPath,
                    "..", "MVC", "wwwroot", "uploads", "resumes"
                );

                Directory.CreateDirectory(resumeFolderPath);

                string sanitizedEmail = SanitizeFileName(currentUser.c_email);
                string fileName = $"{sanitizedEmail}.resume.pdf";
                string fullPath = Path.Combine(resumeFolderPath, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                await model.resumeform.CopyToAsync(stream);

                model.c_resume_file = fileName;
            }

            else
            {
                _logger.LogWarning("❌ Resume file NOT received or EMPTY");
            }

            // SAVE DETAILS IN DB
            int result = await _applicantRepo.AddUserDetails(model);

            if (result == 1)
                return Ok(new
                {
                    success = true,
                    message = "Popup details saved (TEMP email logic)"
                });

            return StatusCode(500, "Failed to save popup details");
        }

        // CHECK POPUP FILLED OR NOT

        [HttpGet("exists")]
        public async Task<IActionResult> IsPopupFilled()
        {
            var userId = int.Parse(User.FindFirst("userid")!.Value);
            bool exists = await _applicantRepo.IsDetailsExists(userId);
            return Ok(new { filled = exists });
        }
        //===================================

        // ================= PROFILE USER (vm_user) ============================

        [HttpGet("profile/vm-user")]
        public async Task<IActionResult> GetUserProfileVm()
        {
            int userid = int.Parse(User.FindFirst("userid")!.Value);

            var user = await _applicantRepo.GetUserForProfileAsync(userid);

            if (user == null)
                return NotFound("User not found");

            return Ok(user);
        }

        [HttpPost("profile/vm-user/update")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateUserProfileVm([FromForm] vm_user model)
        {
            try
            {
                int userid = int.Parse(User.FindFirst("userid")!.Value);
                model.c_userid = userid;
                if (model == null)
                {
                    return BadRequest(new
                    {
                        title = "Invalid data",
                        message = "User data is required"
                    });
                }

                _logger.LogInformation("Starting profile update for user: {UserId}", model.c_userid);

                var currentUser = await _applicantRepo.GetUserForProfileAsync(model.c_userid);
                if (currentUser == null)
                    return NotFound(new
                    {
                        title = "User not found",
                        message = $"User with ID {model.c_userid} not found"
                    });

                _logger.LogInformation("Current user found: {Email}", currentUser.c_email);

                // Check if image is being uploaded
                if (model.imageform != null && model.imageform.Length > 0)
                {
                    _logger.LogInformation("Processing profile image upload: {FileName}", model.imageform.FileName);

                    // Get user email for filename
                    string userEmail = currentUser.c_email;
                    if (string.IsNullOrEmpty(userEmail))
                        return BadRequest(new
                        {
                            title = "Email required",
                            message = "User email is required for file naming"
                        });

                    // Sanitize email for filename
                    string sanitizedEmail = SanitizeFileName(userEmail);

                    // Save to MVC's wwwroot
                    string mvcProjectPath = Path.Combine(_env.ContentRootPath, "..", "MVC");
                    string webRootPath = Path.Combine(mvcProjectPath, "wwwroot");
                    string folderPath = Path.Combine(webRootPath, "uploads", "profile_images");

                    System.IO.Directory.CreateDirectory(folderPath);

                    // Use email as filename
                    string extension = Path.GetExtension(model.imageform.FileName);
                    string fileName = $"{sanitizedEmail}{extension}";
                    string fullPath = Path.Combine(folderPath, fileName);

                    _logger.LogInformation("Saving profile image to: {Path}", fullPath);

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(currentUser.c_profile_image))
                    {
                        try
                        {
                            string oldFileName = System.IO.Path.GetFileName(currentUser.c_profile_image);
                            if (!string.IsNullOrEmpty(oldFileName))
                            {
                                string oldFullPath = Path.Combine(folderPath, oldFileName);
                                if (System.IO.File.Exists(oldFullPath))
                                {
                                    System.IO.File.Delete(oldFullPath);
                                    _logger.LogInformation("Deleted old profile image: {OldFile}", oldFileName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old profile image, continuing with new upload");
                        }
                    }

                    // Save new file
                    using var stream = new System.IO.FileStream(fullPath, System.IO.FileMode.Create);
                    await model.imageform.CopyToAsync(stream);

                    model.c_profile_image = fileName;
                    _logger.LogInformation("Profile image saved: {ImageUrl}", model.c_profile_image);
                }
                else
                {
                    // Keep existing image
                    model.c_profile_image = currentUser.c_profile_image;
                    _logger.LogInformation("No new image uploaded, preserving existing: {Image}", model.c_profile_image);
                }

                // Preserve other fields from current user (they might not be in the form)
                model.c_name = currentUser.c_name;
                model.c_gender = model.c_gender ?? currentUser.c_gender; // Use new value or keep existing
                model.c_contactno = model.c_contactno ?? currentUser.c_contactno;
                model.c_email = currentUser.c_email;
                model.c_role = currentUser.c_role;
                model.c_status = currentUser.c_status;
                model.c_dob = currentUser.c_dob; // Keep existing DOB unless explicitly updated

                // Update address from form (or keep existing if empty)
                if (string.IsNullOrEmpty(model.c_address))
                {
                    model.c_address = currentUser.c_address;
                }

                _logger.LogInformation("Updating profile with data: Name={Name}, Address={Address}, Contact={Contact}, Gender={Gender}, Image={Image}",
                    model.c_name, model.c_address, model.c_contactno, model.c_gender, model.c_profile_image);

                // Update profile in database
                await _applicantRepo.UpdateUserProfileAsync(model);

                return Ok(new
                {
                    success = true,
                    message = "Profile updated successfully",
                    imageUrl = model.c_profile_image
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new
                {
                    title = "Internal server error",
                    message = ex.Message,
                    details = ex.StackTrace
                });
            }
        }

        // Helper method to sanitize filename
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName
                .Where(ch => !invalidChars.Contains(ch))
                .ToArray())
                .Replace("@", "_at_")
                .Replace(".", "_dot_")
                .Replace(" ", "_");

            return sanitized;
        }

        // ================= USER DETAILS ================================

        [HttpGet("profile/details")]
        public async Task<IActionResult> GetUserDetails()
        {
            int userid = int.Parse(User.FindFirst("userid")!.Value);
            var data = await _applicantRepo.GetUserDetailsAsync(userid);
            return Ok(data);
        }

        [HttpPost("profile/details/save")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SaveUserDetails([FromForm] t_userdetails model)
        {
            try
            {
                int userid = int.Parse(User.FindFirst("userid")!.Value);
                model.c_userid = userid;

                var currentUser = await _applicantRepo.GetUserForProfileAsync(userid);

                if (currentUser == null)
                    return BadRequest("User not found");

                var currentDetails = await _applicantRepo.GetUserDetailsAsync(model.c_userid);

                // Preserve existing resume if no new file uploaded
                if (model.resumeform == null || model.resumeform.Length == 0)
                {
                    if (currentDetails != null && !string.IsNullOrEmpty(currentDetails.c_resume_file))
                    {
                        model.c_resume_file = currentDetails.c_resume_file;
                        _logger.LogInformation("No new resume uploaded, preserving existing: {Resume}",
                            model.c_resume_file);
                    }
                    else
                    {
                        model.c_resume_file = null;
                        _logger.LogInformation("No resume file specified and no existing resume found");
                    }
                }
                else
                {
                    string ext = Path.GetExtension(model.resumeform.FileName).ToLower();
                    if (ext != ".pdf")
                        return BadRequest("Only PDF allowed");

                    if (model.resumeform.Length > 5 * 1024 * 1024)
                        return BadRequest("File size should be less than 5MB");

                    // Save to MVC's wwwroot with email as filename
                    string mvcProjectPath = Path.Combine(_env.ContentRootPath, "..", "MVC");
                    string webRootPath = Path.Combine(mvcProjectPath, "wwwroot");
                    string folderPath = Path.Combine(webRootPath, "uploads", "resumes");

                    System.IO.Directory.CreateDirectory(folderPath);

                    // Use email as filename
                    string sanitizedEmail = SanitizeFileName(currentUser.c_email);
                    string fileName = $"{sanitizedEmail}{ext}";
                    string fullPath = Path.Combine(folderPath, fileName);

                    _logger.LogInformation("Saving resume: {FileName} to {Path}", fileName, fullPath);

                    // Delete old resume if exists
                    if (currentDetails != null && !string.IsNullOrEmpty(currentDetails.c_resume_file))
                    {
                        try
                        {
                            string oldFileName = System.IO.Path.GetFileName(currentDetails.c_resume_file);
                            if (!string.IsNullOrEmpty(oldFileName))
                            {
                                string oldFullPath = Path.Combine(folderPath, oldFileName);
                                if (System.IO.File.Exists(oldFullPath))
                                {
                                    System.IO.File.Delete(oldFullPath);
                                    _logger.LogInformation("Deleted old resume: {OldFile}", oldFileName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old resume, continuing with new upload");
                        }
                    }

                    // Save new file
                    using var stream = new System.IO.FileStream(fullPath, System.IO.FileMode.Create);
                    await model.resumeform.CopyToAsync(stream);

                    model.c_resume_file = fileName;
                    _logger.LogInformation("Resume saved successfully: {ResumeUrl}", model.c_resume_file);
                }

                await _applicantRepo.SaveUserDetailsAsync(model);
                return Ok(new
                {
                    success = true,
                    resumeUrl = model.c_resume_file
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user details");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // ================= EXPERIENCE ================================

        [HttpGet("profile/experience")]
        public async Task<IActionResult> GetExperience()
        {
            int userid = int.Parse(User.FindFirst("userid")!.Value);
            return Ok(await _applicantRepo.GetExperiencesAsync(userid));
        }

        [HttpPost("profile/experience/add")]
        public async Task<IActionResult> AddExperience([FromBody] t_userexperience model)
        {
            model.c_userid = int.Parse(User.FindFirst("userid")!.Value);
            await _applicantRepo.AddExperienceAsync(model);
            return Ok(new { success = true });
        }

        [HttpPut("profile/experience/update")]
        public async Task<IActionResult> UpdateExperience([FromBody] t_userexperience model)
        {
            model.c_userid = int.Parse(User.FindFirst("userid")!.Value);
            await _applicantRepo.UpdateExperienceAsync(model);
            return Ok(new { success = true });
        }

        [HttpDelete("profile/experience/delete/{id}")]
        public async Task<IActionResult> DeleteExperience(int id)
        {
            await _applicantRepo.DeleteExperienceAsync(id);
            return Ok(new { success = true });
        }

        // ================= EDUCATION ================================

        [HttpGet("profile/education")]
        public async Task<IActionResult> GetEducation()
        {
            int userId = int.Parse(User.FindFirst("userid")!.Value);
            return Ok(await _applicantRepo.GetEducationsAsync(userId));
        }

        [HttpPost("profile/education/add")]
        public async Task<IActionResult> AddEducation([FromBody] t_educationdetails model)
        {
            model.c_userid = int.Parse(User.FindFirst("userid")!.Value);
            await _applicantRepo.AddEducationAsync(model);
            return Ok(new { success = true });
        }

        [HttpDelete("profile/education/delete/{id}")]
        public async Task<IActionResult> DeleteEducation(int id)
        {
            await _applicantRepo.DeleteEducationAsync(id);
            return Ok(new { success = true });
        }

        //******
        [HttpPut("profile/education/update")]
        public async Task<IActionResult> UpdateEducation([FromBody] t_educationdetails model)
        {
            model.c_userid = int.Parse(User.FindFirst("userid")!.Value);
            await _applicantRepo.UpdateEducationAsync(model);
            return Ok(new { success = true });
        }

        // ================= CERTIFICATE ================================

        [HttpGet("profile/certificate")]
        public async Task<IActionResult> GetCertificates()
        {
            int userId = int.Parse(User.FindFirst("userid")!.Value);

            return Ok(await _applicantRepo.GetCertificatesAsync(userId));
        }

        [HttpPost("profile/certificate/add")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddCertificate([FromForm] t_certificate model)
        {
            model.c_userid = int.Parse(User.FindFirst("userid")!.Value);
            if (model.certificateform != null && model.certificateform.Length > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
                string ext = Path.GetExtension(model.certificateform.FileName).ToLower();

                if (!allowedExtensions.Contains(ext))
                    return BadRequest("Only PDF, JPG, PNG, DOC files are allowed");

                // Save to MVC's wwwroot
                string mvcProjectPath = Path.Combine(_env.ContentRootPath, "..", "MVC");
                string webRootPath = Path.Combine(mvcProjectPath, "wwwroot");
                string folderPath = Path.Combine(webRootPath, "uploads", "certificates");

                System.IO.Directory.CreateDirectory(folderPath);

                // Use email + original filename for certificate
                var user = await _applicantRepo.GetUserForProfileAsync(model.c_userid);
                string prefix = user?.c_email != null ? SanitizeFileName(user.c_email) : "user";
                string originalName = Path.GetFileNameWithoutExtension(model.certificateform.FileName);
                string safeName = SanitizeFileName(originalName);
                string fileName = $"{prefix}_{safeName}_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";

                string fullPath = Path.Combine(folderPath, fileName);

                using var stream = new System.IO.FileStream(fullPath, System.IO.FileMode.Create);
                await model.certificateform.CopyToAsync(stream);

                model.c_certificatefile = fileName;
            }

            await _applicantRepo.AddCertificateAsync(model);
            return Ok(new { success = true });
        }

        [HttpDelete("profile/certificate/delete/{id}")]
        public async Task<IActionResult> DeleteCertificate(int id)
        {
            await _applicantRepo.DeleteCertificateAsync(id);
            return Ok(new { success = true });
        }
        //====================================================================================================================================================

        [AllowAnonymous]
        [HttpGet("CheckEmailExistsApplicant")]
        public async Task<IActionResult> CheckEmailExistsApplicant(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { success = false, message = "Email is required" });

            var result = await _user.GetUser($"AND c_email = '{email}' AND c_role='applicant';");


            bool exists = result.Count > 0;



            return Ok(new
            {
                success = !exists,
                message = exists ? "Email already exists" : ""
            });
        }

        [AllowAnonymous]
        [HttpGet("CheckContactNoExistsApplicant")]
        public async Task<IActionResult> v(string contactno)
        {
            if (string.IsNullOrWhiteSpace(contactno))
                return BadRequest(new { success = false, message = "Contact No is required" });

            var result = await _user.GetUser($"AND c_contactno = '{contactno}' AND c_role='applicant';");


            bool exists = result.Count > 0;

            return Ok(new
            {
                success = !exists,
                message = exists ? "Contact No Already Exists" : ""
            });
        }

        //===============================================DASH BOARD ===============================================


        [HttpGet("jobs")]
        public async Task<IActionResult> GetAllJobs()
        {
            try
            {
                var data = await _applicantRepo.GetAllJobs();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, success = false });
            }
        }


        [HttpGet("recommended")]
        public async Task<IActionResult> GetRecommendedJobs()
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid")!.Value);
                var data = await _applicantRepo.GetRecommendedJobs(userId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, success = false });
            }
        }

        [HttpPost("initialize-elasticsearch")]
        public async Task<IActionResult> InitializeElasticsearch()
        {
            try
            {
                var result = await _elasticsearchService.CreateIndexAsync();

                if (result == 1)
                    return Ok(new
                    {
                        success = true,
                        message = "Elasticsearch index created successfully"
                    });
                else if (result == 0)
                    return BadRequest(new
                    {
                        success = false,
                        message = "Index already exist!"
                    });
                else
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to create index"
                    });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] t_jobsearchrequest request)
        {
            try
            {
                var (jobs, total) =
                    await _elasticsearchService.SearchJobsAsync(request);
                    Console.WriteLine($"Total Jobs Found: {total}");

        // 🔹 Print each job
        foreach (var job in jobs)
        {
            Console.WriteLine("---- JOB START ----");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                job,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            ));
            Console.WriteLine("---- JOB END ----");
        }

                return Ok(new { success = true, total, data = jobs });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Search failed",
                    error = ex.Message
                });
            }
        }


        [HttpPost("save")]
        public async Task<IActionResult> SaveJob([FromForm] int jobId)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid")!.Value);
                var result = await _applicantRepo.SaveJob(userId, jobId);

                if (result)
                    return Ok(new { message = "Job saved successfully", success = true });
                else
                    return BadRequest(new { message = "Failed to save job", success = false });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, success = false });
            }
        }



        [HttpGet("saved")]
        public async Task<IActionResult> GetSavedJobs()
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid")!.Value);
                var data = await _applicantRepo.GetSavedJobs(userId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, success = false });
            }
        }


        [HttpGet("isJobSaved")]
        public async Task<IActionResult> IsJobSaved(int jobId)
        {

            int userId = int.Parse(User.FindFirst("userid")!.Value);
            if (jobId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid jobId"
                });
            }

            bool isSaved = await _applicantRepo.IsJobSaved(userId, jobId);

            return Ok(new
            {
                success = true,
                isSaved = isSaved
            });
        }

        [HttpGet("applied")]
        public async Task<IActionResult> GetAppliedJobs()
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid")!.Value);
                var data = await _applicantRepo.GetAppliedJobs(userId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, success = false });
            }
        }

        [HttpDelete("remove-saved/{jobId}")]
        public async Task<IActionResult> RemoveSavedJob(int jobId)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid")!.Value);
                var result = await _applicantRepo.RemoveSavedJob(userId, jobId);

                if (result)
                    return Ok(new { message = "Job removed successfully", success = true });
                else
                    return BadRequest(new { message = "Failed to remove job", success = false });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, success = false });
            }
        }
    }
}