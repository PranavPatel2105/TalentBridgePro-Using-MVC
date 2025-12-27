using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Models;

using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IApplicantInterface
    {
//PRANAV===================================================================================================================================================
        Task<object?> GetApplyJobDetailsAsync(int jobId);
        Task<string?> GetLatestApplicationStatusAsync(int userId, int jobId);
        Task<int> ApplyJobAsync(t_applications model);
        Task<t_applications?> GetApplicationPreviewByIdAsync(int applicationId);


//===================================================================================================================================================

        Task<int> GiveCompanyReview(t_company_review review);
        // Dev UserDetails POPUP S =====
        Task<int> AddUserDetails(t_userdetails model);
        Task<bool> IsDetailsExists(int userid);

        // Dev UserDetails POPUP E =====

        // Dev Profile Management S =====

        
        // =========================
        // USER (READ + UPDATE)
        // =========================
        Task<vm_user?> GetUserForProfileAsync(int userid);

        Task UpdateUserProfileAsync(vm_user user);

        // =========================
        // USER DETAILS
        // =========================
        Task<t_userdetails?> GetUserDetailsAsync(int userid);
        Task SaveUserDetailsAsync(t_userdetails details);

        // =========================
        // EXPERIENCE
        // =========================
        Task<List<t_userexperience>> GetExperiencesAsync(int userid);
        Task AddExperienceAsync(t_userexperience experience);
        Task UpdateExperienceAsync(t_userexperience experience);
        Task DeleteExperienceAsync(int experienceId);

        // =========================
        // EDUCATION
        // =========================
        Task<List<t_educationdetails>> GetEducationsAsync(int userid);
        Task AddEducationAsync(t_educationdetails education);
//***
        Task UpdateEducationAsync(t_educationdetails education);
        Task DeleteEducationAsync(int educationId);

        // =========================
        // CERTIFICATE
        // =========================
        Task<List<t_certificate>> GetCertificatesAsync(int userid);
        Task AddCertificateAsync(t_certificate certificate);
        Task DeleteCertificateAsync(int certificateId);


        // Dev Profile Management E =====


        Task<List<ApplicantJobVM>> GetAllJobs();
        Task<List<ApplicantJobVM>> GetRecommendedJobs(int userId);
        Task<List<ApplicantJobVM>> GetSavedJobs(int userId);
        Task<List<ApplicantJobVM>> GetAppliedJobs(int userId);
        Task<bool> SaveJob(int userId, int jobId);
        Task<bool> ApplyJob(int userId, int jobId, string name, string email, string resumeFile);
        Task<bool> IsJobSaved(int userId, int jobId);
        Task<bool> RemoveSavedJob(int userId, int jobId);

     //checkkkkkkkkkkkkkkkkkkkkkkkkkkkk herrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrreeeeeeeeeeeeeeeeeeeeeeeeeeee
        Task<bool> IsJobAccepted(int userId, int jobId);
Task<int> GetCompanyIdByJobId(int jobId);
    }
}