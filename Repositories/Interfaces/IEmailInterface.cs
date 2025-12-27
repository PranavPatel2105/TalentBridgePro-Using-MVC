using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IEmailInterface
    {
        Task SendOtpAsync(string email, string otp);
        Task SendWelcomeEmailAsync(string email, string username);
        Task SendApplicationRejectedEmailAsync(string email,
        string username,
        string companyName,
        string jobRole);

        Task SendApplicationPendingEmailAsync(
        string email,
        string username,
        string companyName,
        string jobRole);

        Task SendApplicationAcceptedEmailAsync(string email,
       string username,
       string companyName,
       string jobRole);

       Task SendRecruiterNewApplicationEmailAsync(
            string recruiterEmail,
            string recruiterName,
            string applicantName,
            string jobTitle);


    }
}