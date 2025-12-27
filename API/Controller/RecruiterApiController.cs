using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Repositories.Interfaces;
using Repositories.Models;
using Google.GenAI;
using Microsoft.Extensions.Options;
using System.Text;
using Microsoft.AspNetCore.Authorization;



namespace API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecruiterApiController : ControllerBase
    {
        private readonly IEmailInterface _email;
        private readonly IUserInterface _user;
        private readonly IElasticsearchInterface _elasticsearchService;
        private readonly HuggingFaceConfig _hfConfig;
        private readonly IRecruiterInterface _recruiterRepo;
        private readonly IWebHostEnvironment _env;

        private readonly ILogger<RecruiterApiController> _logger;
        private readonly HttpClient _httpClient;
        public RecruiterApiController(IRecruiterInterface recruiterRepo, IWebHostEnvironment env, IOptions<HuggingFaceConfig> hfConfig, ILogger<RecruiterApiController> logger, IHttpClientFactory httpClientFactory, IEmailInterface email, IUserInterface user, IElasticsearchInterface elasticsearchInterface)


        {
            _elasticsearchService = elasticsearchInterface;
            _hfConfig = hfConfig.Value;
            _recruiterRepo = recruiterRepo;
            _env = env;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();

            _email = email;
            _user = user;
        }


        // ================CRUD FOR JOB ==============================================================================================================================================================

        //CREATE JOB
        [Authorize(Roles = "recruiter")]
        [HttpPost("AddJob")]
        public async Task<IActionResult> AddJob([FromForm] t_job job)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid")!.Value);

                int companyId = await _recruiterRepo.GetCompanyIdByUserId(userId);
                if (companyId == 0)
                    return BadRequest("Company profile not found");

                job.c_userid = userId;
                job.c_company_id = companyId;

                if (job.jdform != null)
                {
                    string mvcRoot = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "..",        // move out of API
                        "MVC",       // MVC project folder name
                        "wwwroot"
                    );
                    string mvcJdPath = Path.Combine(mvcRoot, "uploads", "jd");
                    Directory.CreateDirectory(mvcJdPath);

                    string fileName = Guid.NewGuid() + Path.GetExtension(job.jdform.FileName);
                    string filePath = Path.Combine(mvcJdPath, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await job.jdform.CopyToAsync(stream);

                    // Save only filename in DB
                    job.c_jd_file = fileName;
                }
                else
                {
                    job.c_jd_file = ""; // Set empty if no file
                }

                int jobId = await _recruiterRepo.AddJobAsync(job);
                job.c_job_id = jobId;
                var result = await _elasticsearchService.CreateDocumentAsync(job);
                return Ok(new { message = "Job created successfully", jobId });
            }
            catch (Exception ex)
            {
                // Log the full exception for debugging
                _logger.LogError(ex, "Error creating job");
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        // UPDATE JOB
        // [Authorize(Roles = "recruiter")]
        // [HttpPut("{id}")]
        // public async Task<IActionResult> UpdateJob(int id, [FromForm] t_job job)
        // {
        //     try
        //     {
        //         int userId = int.Parse(User.FindFirst("userid")!.Value);
        //         int companyId = await _recruiterRepo.GetCompanyIdByUserId(userId);

        //         if (companyId == 0)
        //             return BadRequest("Company profile not found");

        //         job.c_userid = userId;
        //         job.c_company_id = companyId;

        //         if (job.jdform != null)
        //         {
        //             string mvcJdPath = Path.Combine(
        //                 Directory.GetCurrentDirectory(),
        //                 "..",
        //                 "MVC",
        //                 "wwwroot",
        //                 "jd"
        //             );

        //             Directory.CreateDirectory(mvcJdPath);

        //             string fileName = Guid.NewGuid() + Path.GetExtension(job.jdform.FileName);
        //             string filePath = Path.Combine(mvcJdPath, fileName);

        //             using var stream = new FileStream(filePath, FileMode.Create);
        //             await job.jdform.CopyToAsync(stream);

        //             job.c_jd_file = fileName;
        //         }
        //         else
        //         {
        //             // If no file uploaded but there's existing file, keep it
        //             // You might want to fetch existing job and keep its c_jd_file
        //         }

        //         var updated = await _recruiterRepo.UpdateJobAsync(id, job);
        //         if (!updated)
        //             return NotFound("Job not found");

        //         return Ok("Job updated successfully");
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error updating job");
        //         return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        //     }
        // }

        //DELETE JOB
        // [Authorize(Roles = "recruiter")]
        // [HttpDelete("{id}")]
        // public async Task<IActionResult> DeleteJob(int id)
        // {
        //     try
        //     {
        //         int userId = int.Parse(User.FindFirst("userid")!.Value);
        //         int companyId = await _recruiterRepo.GetCompanyIdByUserId(userId);

        //         if (companyId == 0)
        //             return BadRequest("Company profile not found");
        //         var deleted = await _recruiterRepo.DeleteJobAsync(id);
        //         if (!deleted)
        //             return NotFound("Job not found");

        //         return Ok("Job deleted successfully");
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, ex.Message);
        //     }
        // }




        // Get job by ID for editing
        [HttpGet("GetJob/{jobId}")]
        public async Task<IActionResult> GetJob(int jobId)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid")!.Value);
                var job = await _recruiterRepo.GetJobByIdAndUserId(jobId, userId);

                if (job == null)
                    return NotFound("Job not found or you don't have permission to edit it");

                return Ok(job);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching job: {ex.Message}");
            }
        }

        // Update job
        [HttpPut("UpdateJob")]
        public async Task<IActionResult> UpdateJob([FromBody] t_job job)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid")!.Value);

                // Verify ownership
                var existingJob = await _recruiterRepo.GetJobByIdAndUserId(job.c_job_id, userId);
                if (existingJob == null)
                    return Unauthorized("You don't have permission to edit this job");

                // Update job
                var result = await _recruiterRepo.UpdateJob(job);
                var result1 = await _elasticsearchService.UpdateDocumentAsync(job);

                if (result > 0)
                    return Ok(new { success = true, message = "Job updated successfully" });
                else
                    return BadRequest("Failed to update job");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating job: {ex.Message}");
            }
        }



        [HttpDelete("DeleteJob/{jobId}")]
        public async Task<IActionResult> DeleteJob(int jobId)
        {
            try
            {
                // First check if job exists and belongs to current recruiter
                int userId = int.Parse(User.FindFirst("userid")!.Value);
                var job = await _recruiterRepo.GetJobByIdAndUserId(jobId, userId);

                if (job == null)
                    return NotFound("Job not found or you don't have permission to delete it");

                // Delete from database
                var result = await _recruiterRepo.DeleteJob(jobId);
                var result1 = await _elasticsearchService.DeleteDocumentAsync(jobId);

                if (result > 0)
                    return Ok(new { success = true, message = "Job deleted successfully" });
                else
                    return BadRequest("Failed to delete job");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting job: {ex.Message}");
            }
        }

        [HttpPost("generate-description")]
        public async Task<IActionResult> GenerateJobDescription([FromBody] DescriptionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.JobRole))
                    return BadRequest(new { error = "Job role is required" });

                string prompt = $"""
You are an HR expert.

Generate a detailed professional job description (300–400 words).

Job Title: {request.JobRole}
Job Type: {request.JobType}
Location: {request.Location}
Experience: {request.Experience} years
Skills: {string.Join(", ", request.Skills ?? new List<string>())}
Salary: {request.Salary}

Include sections:
- Job Overview
- Responsibilities
- Required Skills
- Benefits

Use bullet points.
""";

                var text = await CallHuggingFaceAsync(prompt);

                return Ok(new
                {
                    success = true,
                    description = text.Trim(),
                    wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HF AI Error");
                return StatusCode(500, new
                {
                    success = false,
                    error = "AI generation failed",
                    message = ex.Message
                });
            }
        }
        // SIMPLE HTTP CLIENT METHOD (Most reliable)
        private async Task<string> CallHuggingFaceAsync(string prompt)
        {
            var requestBody = new
            {
                model = "google/gemma-2-2b-it",
                messages = new[]
                {
            new { role = "system", content = "You are a professional HR assistant." },
            new { role = "user", content = prompt }
        },
                max_tokens = 350,
                temperature = 0.7
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://router.huggingface.co/v1/chat/completions"
            );

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", _hfConfig.ApiKey);

            request.Headers.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception(body);

            using var doc = JsonDocument.Parse(body);

            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                ?? throw new Exception("Empty AI response");
        }

        public class DescriptionRequest
        {
            public string JobRole { get; set; }
            public string JobType { get; set; }
            public string Location { get; set; }
            public decimal Experience { get; set; }
            public List<string> Skills { get; set; }
            public string Salary { get; set; }
        }

        //=====================CRUD JOB END==================================================================================================================================================================

        //======================================== Dashboard=====================================================================================================
        [Authorize(Roles = "recruiter")]
        [HttpGet("jobsById/me")]
        public async Task<IActionResult> GetJobDetailsByRecruiterId()
        {
            //     string? userId = User.FindFirst("userid")!.Value;

            //     List<t_job> jobDetails;

            //     if (!string.IsNullOrEmpty(userId))
            //     {
            //         jobDetails = await _recruiterRepo.GetJobDetailsByRecruiterId(
            //             Convert.ToInt32(userId)
            //         );
            //     }
            //     else
            //     {
            //         return Unauthorized();
            //     }

            //     return Ok(jobDetails);
            int userId = int.Parse(User.FindFirst("userid")!.Value);

            var jobDetails = await _recruiterRepo.GetJobDetailsByRecruiterId(userId);

            return Ok(jobDetails);
        }

        [AllowAnonymous]
        [HttpGet("DownloadJD")]
        public IActionResult DownloadJD(string fileName)
        {
            // string? userId = User.FindFirst("userid")!.Value;

            // if (string.IsNullOrEmpty(userId))
            //     return Unauthorized();

            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("Invalid file");

            fileName = Path.GetFileName(fileName);

            string mvcRoot = Path.Combine(
        Directory.GetCurrentDirectory(),
        "..",
        "MVC",
        "wwwroot"
    );

            string jdFolder = Path.Combine(mvcRoot, "uploads", "jd");
            string filePath = Path.Combine(jdFolder, fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found");

            return PhysicalFile(filePath, "application/octet-stream", fileName);
        }
        //==============================Dashboard end=============================================================================================================        

        //===============================Contact us ===================================================================================================
        [Authorize(Roles = "recruiter")]
        [HttpPost("AddContactUsDetails")]
        public async Task<IActionResult> AddContactUsDetails([FromBody] t_contactus contactus)
        {
            string? userId = User.FindFirst("userid")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                contactus.c_userid = Convert.ToInt32(userId);
            }
            else
            {
                return Unauthorized();
            }

            int result = await _recruiterRepo.AddContactUsDetails(contactus);

            if (result == 1)
            {
                return Ok(new { success = true, message = "Contact us details added successfully" });
            }
            else
            {
                return BadRequest(new { success = false, message = "Failed to add contact us details" });
            }
        }

        //===============================contact us end========================================================================================================

        //====================================PROFILE RECRUITER============================================================================================================================================


        [Authorize(Roles = "recruiter")]
        [HttpGet("me")]

        public async Task<IActionResult> GetOneRecruiter()
        {


            int userId = int.Parse(User.FindFirst("userid")!.Value);

            var recruiter = await _recruiterRepo.GetOneRecruiter(userId);
            if (recruiter == null)
                return BadRequest(new { success = false, message = "There was no recruiter found" });
            return Ok(recruiter);
        }

        [Authorize(Roles = "recruiter")]
        [HttpPut("editrecruiter")]
        public async Task<IActionResult> EditRecruiter([FromForm] t_user recruiter)
        {

            int userId = int.Parse(User.FindFirst("userid")!.Value);
            recruiter.c_userid = userId;
            Console.WriteLine(JsonSerializer.Serialize(recruiter, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Return validation errors
            }
            if (recruiter.imageform != null && recruiter.imageform.Length > 0)
            {
                string filename = recruiter.c_email + Path.GetExtension(recruiter.imageform.FileName);
                string mvcJdPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "..",        // move out of API
                        "MVC",       // MVC project folder name
                        "wwwroot"
                    );

                string folder = Path.Combine(mvcJdPath, "user_images");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string filepath = Path.Combine(folder, filename);
                recruiter.c_profile_image = filename;
                using (var stream = new FileStream(filepath, FileMode.Create))
                {
                    recruiter.imageform.CopyTo(stream);
                }
            }

            int status = await _recruiterRepo.EditRecruiter(recruiter);
            if (status == 1)
            {
                return Ok(new { success = true, message = "recruiter Updated Successfully" });
            }
            else
            {
                return BadRequest(new { success = false, message = "There was some error while Update recruiter" });
            }
        }





        //================================ EDIT COMPANY DETAILS RECRUITER ================================================================================================================================================
        [Authorize(Roles = "recruiter")]
        [HttpGet("company/me")]
        public async Task<IActionResult> GetOneCompany()
        {
            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            var company = await _recruiterRepo.GetOneCompany(userId);
            if (company == null)
                return BadRequest(new { success = false, message = "There was no company found" });
            return Ok(company);
        }



        [Authorize(Roles = "recruiter")]
        [HttpPut("editcompany")]
        public async Task<IActionResult> EditCompany([FromForm] t_companydetails company)
        {

            // int userId = int.Parse(User.FindFirst("UserId")!.Value);

            int userId = int.Parse(User.FindFirst("userid")!.Value);


            int? companyId = await _recruiterRepo.GetCompanyIdByUserId(userId);

            if (companyId == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Company not found for this recruiter"
                });
            }

            company.c_company_id = companyId.Value;
            company.c_userid = userId;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Return validation errors
            }
            if (company.imageform != null && company.imageform.Length > 0)
            {
                string filename = company.c_company_email + Path.GetExtension(company.imageform.FileName);
                string mvcJdPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "..",        // move out of API
                        "MVC",       // MVC project folder name
                        "wwwroot"
                    );

                string folder = Path.Combine(mvcJdPath, "company_images");
                Directory.CreateDirectory(folder);
                string filepath = Path.Combine(folder, filename);
                company.c_image = filename;
                using (var stream = new FileStream(filepath, FileMode.Create))
                {
                    company.imageform.CopyTo(stream);
                }
            }
            int status = await _recruiterRepo.EditCompany(company);
            if (status == 1)
            {
                return Ok(new { success = true, message = "company Updated Successfully" });
            }
            else
            {
                return BadRequest(new { success = false, message = "There was some error while Update company" });
            }
        }

        //====================================Company details END============================================================================================================================================

        //===================================Recruiter side Approve / reject =====================================================================
        [Authorize(Roles = "recruiter")]
        [HttpGet("job/{c_job_id}/applications")]
        public async Task<IActionResult> ViewApplications(int c_job_id)
        {
            int userId = int.Parse(User.FindFirst("userid")!.Value);


            bool isOwner = await _recruiterRepo.IsJobOwnedByRecruiter(c_job_id, userId);
            if (!isOwner)
            {
                return Forbid("You are not authorized to view this job's applications");
            }
            var applications = await _recruiterRepo.ViewApplication(c_job_id);
            if (applications == null || applications.Count == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "No applications found",
                    data = new List<ViewApplicationVM>()
                });
            }

            return Ok(new
            {
                success = true,
                data = applications
            });
        }

        [Authorize(Roles = "recruiter")]
        [HttpPut("application/{c_application_id}/status")]
        public async Task<IActionResult> UpdateApplicationStatus(int c_application_id, [FromBody] JsonElement data)
        {
            string c_status = data.GetProperty("c_status").GetString();

            int result = await _recruiterRepo.UpdateApplicationStatus(
                c_application_id,
                c_status
            );

            if (result == 1)
            {
                _recruiterRepo.GetMailDetails(c_application_id);
                if (c_status == "accepted")
                {
                    var mailDetails = await _recruiterRepo.GetMailDetails(c_application_id);
                    await _email.SendApplicationAcceptedEmailAsync(mailDetails.email, mailDetails.name, mailDetails.companyName, mailDetails.job_role);
                }
                if (c_status == "rejected")
                {
                    var mailDetails = await _recruiterRepo.GetMailDetails(c_application_id);
                    await _email.SendApplicationRejectedEmailAsync(mailDetails.email, mailDetails.name, mailDetails.companyName, mailDetails.job_role);
                }
                if (c_status == "pending")
                {
                    var mailDetails = await _recruiterRepo.GetMailDetails(c_application_id);
                    await _email.SendApplicationPendingEmailAsync(mailDetails.email, mailDetails.name, mailDetails.companyName, mailDetails.job_role);
                }

                return Ok(new
                {
                    success = true,
                    message = "Application status updated successfully"
                });
            }

            return BadRequest(new
            {
                success = false,
                message = "Failed to update application status"
            });
        }
        //===================================Recruiter side Approve / reject END =====================================================================


        [Authorize(Roles = "recruiter")]
        [HttpPost]
        [Route("AddCompanyDetails")]
        public async Task<IActionResult> AddCompanyDetails([FromForm] t_companydetails companydetails)
        {

            int userId = int.Parse(User.FindFirst("userid")!.Value);
            companydetails.c_userid = userId;
            // Log incoming data
            // Console.WriteLine(
            //     JsonSerializer.Serialize(
            //         companydetails,
            //         new JsonSerializerOptions { WriteIndented = true }
            //     )
            // );

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid Company Details Model" });
            }

            if (companydetails.imageform != null && companydetails.imageform.Length > 0)
            {
                // Create filename using company email
                var filename = companydetails.c_company_email + Path.GetExtension(companydetails.imageform.FileName);
                string mvcJdPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "..",        // move out of API
                        "MVC",       // MVC project folder name
                        "wwwroot"
                    );
                // Ensure folder exists
                var folderPath = Path.Combine(mvcJdPath, "company_images");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filepath = Path.Combine(folderPath, filename);

                using (var stream = new FileStream(filepath, FileMode.Create))
                {
                    await companydetails.imageform.CopyToAsync(stream);
                }

                companydetails.c_image = filename;
            }
            else
            {
                companydetails.c_image = "default.jpg";
            }

            var result = await _recruiterRepo.AddCompanyDetails(companydetails);

            if (result == -1)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Company Email Already Exist"
                });
            }

            if (result == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Error in Inserting Company Details"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Company Details Added Successfully"
            });
        }

        [HttpGet("CheckEmailExists")]
        public async Task<IActionResult> CheckEmailExists(string email)
        {
            Console.WriteLine(email);
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { success = false, message = "Email is required" });

            var result = await _recruiterRepo.CheckCompanyEmailExists($"WHERE c_company_email = '{email}';");
            bool exists = result > 0;

            return Ok(new
            {
                success = !exists,
                message = exists ? "Email already exists" : ""
            });
        }

        [HttpGet]
        [Route("SendApplicationEmailAsync")]
        public async Task<IActionResult> SendApplicationEmailAsync(string recruiterEmail,
            string recruiterName,
            string applicantName,
            string jobTitle)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _email.SendRecruiterNewApplicationEmailAsync(recruiterEmail, recruiterName, applicantName, jobTitle);

            return Ok(new
            {
                message = "Application  email sent successfully."
            });
        }


        [HttpGet("CheckContactNoExistsRecruiter")]
        public async Task<IActionResult> CheckContactNoExistsRecruiter(string contactno)
        {
            if (string.IsNullOrWhiteSpace(contactno))
                return BadRequest(new { success = false, message = "Contact No is required" });

            var result = await _user.GetUser($"AND c_contactno = '{contactno}' AND c_role='recruiter';");


            bool exists = result.Count > 0;

            return Ok(new
            {
                success = !exists,
                message = exists ? "Contact No Already Exists" : ""
            });
        }


        [HttpGet("CheckEmailExistsRecruiter")]
        public async Task<IActionResult> CheckEmailExistsRecruiter(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { success = false, message = "Email is required" });

            var result = await _user.GetUser($"AND c_email = '{email}' AND c_role='recruiter';");


            bool exists = result.Count > 0;



            return Ok(new
            {
                success = !exists,
                message = exists ? "Email already exists" : ""
            });
        }


        [HttpGet]
        [Route("SendPendingEmail")]
        public async Task<IActionResult> SendPendingEmail(string username, string email,
        string companyName,
        string jobRole)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _email.SendApplicationPendingEmailAsync(email, username, companyName, jobRole);

            return Ok(new
            {
                message = "Application pending email sent successfully."
            });
        }

        [HttpGet]
        [Route("SendAcceptEmail")]
        public async Task<IActionResult> SendAcceptEmail(string username, string email,
        string companyName,
        string jobRole)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _email.SendApplicationAcceptedEmailAsync(email, username, companyName, jobRole);

            return Ok(new
            {
                message = "Application Accept email sent successfully."
            });
        }

        [HttpGet]
        [Route("SendRejectEmail")]
        public async Task<IActionResult> SendRejectEmail(string username, string email,
        string companyName,
        string jobRole)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _email.SendApplicationRejectedEmailAsync(email, username, companyName, jobRole);

            return Ok(new
            {
                message = "Application Accept email sent successfully."
            });
        }

        //============================================================================

        [Authorize(Roles = "recruiter")]
        [HttpGet("applicants")]
        public async Task<IActionResult> GetAllApplicants()
        {
            try
            {
                var recruiterId = User.FindFirst("userid")?.Value;
                if (recruiterId == null) return Unauthorized();
                var applicants = await _recruiterRepo.GetAllApplicants();
                return Ok(new { success = true, data = applicants });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [Authorize(Roles = "recruiter")]
        [HttpGet("searchapplicants")]
        public async Task<IActionResult> SearchApplicants([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    var allApplicants = await _recruiterRepo.GetAllApplicants();
                    return Ok(new { success = true, data = allApplicants });
                }

                var filteredApplicants = await _recruiterRepo.SearchApplicantsBySkill(query);
                return Ok(new { success = true, data = filteredApplicants });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize(Roles = "recruiter")]
        [HttpGet("applicant/{userid}/details")]
        public async Task<IActionResult> GetApplicantDetails(int userid)
        {
            var data = await _recruiterRepo.GetApplicantFullDetails(userid);
            return Ok(new { success = true, data });
        }
    }
}
