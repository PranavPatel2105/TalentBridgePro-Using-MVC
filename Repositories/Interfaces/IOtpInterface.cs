using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IOtpInterface
    {
        Task GenerateAndSendOtpAsync(string email);
        bool VerifyOtp(string email, string otp, out string message);
    }
}