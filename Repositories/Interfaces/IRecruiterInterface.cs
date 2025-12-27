using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Models;



namespace Repositories.Interfaces
{
    public interface IRecruiterInterface
    {
// PRANAV ===================================================================================================================================================
        
        Task<int> AddJobAsync(t_job job);

        // Task<bool> UpdateJobAsync(int jobId, t_job job);
        Task<int> UpdateJob(t_job job);

       Task<int> DeleteJob(int jobId);

       Task<t_job> GetJobByIdAndUserId(int jobId, int userId);
         Task<bool> IsJobOwnedByRecruiter(int jobId, int recruiterId);

//===================================================================================================================================================
        
        
        Task<List<t_job>> GetJobDetailsByRecruiterId(int recruiterid);
        Task<int> AddContactUsDetails(t_contactus contactus);
        Task<t_user> GetOneRecruiter(int c_userid);
        Task<int> EditRecruiter(t_user recruiter);
        Task<t_companydetails> GetOneCompany(int c_company_id);
        Task<int> EditCompany(t_companydetails company);
        Task<List<ViewApplicationVM>> ViewApplication(int c_job_id);
        // Task<int> AcceptApplication(int c_application_id);
        // Task<int> RejectApplication(int c_application_id);

        Task<int> UpdateApplicationStatus(int c_application_id, string c_status);


    Task<int> GetCompanyIdByUserId(int userId);
        Task<int> AddCompanyDetails(t_companydetails companydetails);
        Task<int> CheckCompanyEmailExists(string? filter=null);
        Task<mailDetAIL_VM> GetMailDetails(int id);

        //===================================================================================================================================================

        Task<List<ViewApplicantDetailsVM>> GetAllApplicants();
        Task<List<ViewApplicantDetailsVM>> SearchApplicantsBySkill(string skill);

        Task<ViewApplicantFullDetailsVM> GetApplicantFullDetails(int userid);
    }
}