using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class OtpRepository : IOtpInterface
    {
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _otpStore
            = new();

        private readonly IEmailInterface _emailService;

        public OtpRepository(IEmailInterface emailService)
        {
            _emailService = emailService;
        }

        public async Task GenerateAndSendOtpAsync(string email)
        {
            string otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(5);

            _otpStore[email] = (otp, expiry);

            await _emailService.SendOtpAsync(email, otp);
        }

        public bool VerifyOtp(string email, string otp, out string message)
        {
            if (!_otpStore.TryGetValue(email, out var data))
            {
                message = "OTP not found. Please request a new one.";
                return false;
            }

            if (DateTime.UtcNow > data.Expiry)
            {
                _otpStore.TryRemove(email, out _);
                message = "OTP expired. Please resend.";
                return false;
            }

            if (data.Otp != otp)
            {
                message = "Invalid OTP";
                return false;
            }

            _otpStore.TryRemove(email, out _);
            message = "OTP verified";
            return true;
        }
    }
}