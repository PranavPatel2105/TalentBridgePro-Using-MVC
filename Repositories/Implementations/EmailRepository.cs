using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementations
{
    public class EmailRepository : IEmailInterface
    {
        private readonly t_email _email;

        public EmailRepository(IOptions<t_email> options)
        {
            _email = options.Value;
        }
        public async Task SendOtpAsync(string email, string otp)
        {
            try
            {
                using var smtp = new SmtpClient(_email.SmtpServer, _email.Port)
                {
                    Credentials = new NetworkCredential(_email.SenderEmail, _email.Password),
                    EnableSsl = true, // 🔹 always true for STARTTLS
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 20000 // 20 seconds timeout
                };

                string body = $@"
<table width='100%' cellpadding='0' cellspacing='0' style='font-family: Arial, sans-serif; background-color:#f4f4f7; padding:20px;'>
    <tr>
        <td align='center'>
            <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 0 10px rgba(0,0,0,0.1);'>
                
                <!-- Header -->
                <tr>
                    <td style='background-color:#2563eb; color:#ffffff; text-align:center; padding:20px; font-size:24px; font-weight:bold;'>
                        TalentBridgePro
                    </td>
                </tr>

                <!-- Body -->
                <tr>
                    <td style='padding:30px; color:#333333; font-size:16px; line-height:1.5;'>
                        <p>Dear User,</p>
                        <p>We received a request to access your TalentBridgePro account. Your One-Time Password (OTP) is:</p>

                        <p style='text-align:center; margin:30px 0;'>
                            <span style='display:inline-block; padding:15px 25px; font-size:28px; letter-spacing:4px; color:#ffffff; background-color:#2563eb; border-radius:6px; font-weight:bold;'>
                                {otp}
                            </span>
                        </p>

                        <p>This OTP is valid for <strong>5 minutes</strong>. Please do not share it with anyone.</p>

                        <p>If you did not request this, please ignore this email.</p>

                        <p>Best Regards,<br/>The TalentBridgePro Team</p>
                    </td>
                </tr>

                <!-- Footer -->
                <tr>
                    <td style='background-color:#f4f4f7; text-align:center; padding:15px; font-size:12px; color:#888888;'>
                        &copy; {DateTime.Now.Year} TalentBridgePro. All rights reserved.
                    </td>
                </tr>

            </table>
        </td>
    </tr>
</table>";


                var mail = new MailMessage
                {
                    From = new MailAddress(_email.SenderEmail),
                    Subject = "OTP Verification",
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(email);

                await smtp.SendMailAsync(mail);
            }
            catch (SmtpException ex)
            {
                // Log or handle error
                throw new Exception($"Failed to send OTP: {ex.Message}", ex);
            }
        }

        public async Task SendWelcomeEmailAsync(string email, string username)
        {
            try
            {

                using var smtp = new SmtpClient(_email.SmtpServer, _email.Port)
                {
                    Credentials = new NetworkCredential(_email.SenderEmail, _email.Password),
                    EnableSsl = _email.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 20000
                };

                string body = $@"
<table width='100%' cellpadding='0' cellspacing='0' style='font-family: Arial, sans-serif; background-color:#f4f4f7; padding:20px;'>
    <tr>
        <td align='center'>
            <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 0 10px rgba(0,0,0,0.1);'>
                <!-- Header -->
                <tr>
                    <td style='background-color:#2563eb; color:#ffffff; text-align:center; padding:20px; font-size:24px; font-weight:bold;'>
                        TalentBridgePro
                    </td>
                </tr>

                <!-- Body -->
                <tr>
                    <td style='padding:30px; color:#333333; font-size:16px; line-height:1.5;'>
                        <p>Dear {username},</p>
                        <p>Welcome to TalentBridgePro! We are thrilled to have you on board.</p>

                        <p>We created your account successfully, and you can now explore all the features that TalentBridgePro has to offer. Whether you're looking for a job or recruiting top talent, we’re here to help you every step of the way.</p>

                        <p>Here are some things you can do to get started:</p>
                        <ul style='color:#333333;'>
                            <li><strong>Complete your profile:</strong> Add your details to improve your chances of getting matched with relevant opportunities.</li>
                            <li><strong>Explore our job listings:</strong> Discover a variety of positions across industries that match your skills.</li>
                            <li><strong>Connect with professionals:</strong> Network with top talent or potential employers in the TalentBridgePro community.</li>
                        </ul>

                        <p>If you need any help, feel free to <a href='#' style='color:#2563eb;'>contact our support team</a>.</p>

                        <p>We are excited to see you succeed with TalentBridgePro.</p>

                        <p>Best Regards,<br/>The TalentBridgePro Team</p>
                    </td>
                </tr>

                <!-- Footer -->
                <tr>
                    <td style='background-color:#f4f4f7; text-align:center; padding:15px; font-size:12px; color:#888888;' >
                        &copy; {DateTime.Now.Year} TalentBridgePro. All rights reserved. <br/>
                        <a href='#' style='color:#888888;'>Unsubscribe</a> | <a href='#' style='color:#888888;'>Privacy Policy</a>
                    </td>
                </tr>
            </table>
        </td>
    </tr>
</table>";


                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(_email.SenderEmail);
                    mail.Subject = "Welcome to TalentBridgePro!";
                    mail.Body = body;
                    mail.IsBodyHtml = true;

                    mail.To.Add(email);

                    // Send the email asynchronously
                    await smtp.SendMailAsync(mail);
                }
            }
            catch (SmtpException ex)
            {
                // Log or handle error
                throw new Exception($"Failed to send welcome email to {email}: {ex.Message}", ex);
            }
        }

        public async Task SendApplicationAcceptedEmailAsync(string email,
    string username,
    string companyName,
    string jobRole)
        {
            try
            {
                using var smtp = new SmtpClient(_email.SmtpServer, _email.Port)
                {
                    Credentials = new NetworkCredential(_email.SenderEmail, _email.Password),
                    EnableSsl = _email.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 20000
                };

                string body = $@"
<table width='100%' cellpadding='0' cellspacing='0' style='font-family: Arial, sans-serif; background-color:#f4f4f7; padding:20px;'>
    <tr>
        <td align='center'>
            <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 0 10px rgba(0,0,0,0.1);'>

                <!-- Header -->
                <tr>
                    <td style='background-color:#2563eb; color:#ffffff; text-align:center; padding:20px; font-size:24px; font-weight:bold;'>
                        TalentBridgePro
                    </td>
                </tr>

                <!-- Body -->
                <tr>
                    <td style='padding:30px; color:#333333; font-size:16px; line-height:1.6;'>
                        <p>Dear {username},</p>

                        <p>
                            Congratulations! 🎉 We are pleased to inform you that your application has been
                            <strong>successfully accepted</strong>.
                        </p>

                        <p><strong>Job Details</strong></p>
                        <table width='100%' cellpadding='8' cellspacing='0' style='background-color:#f9fafb; border-radius:6px;'>
                            
                            <tr>
                                <td><strong>Company:</strong></td>
                                <td>{companyName}</td>
                            </tr>
                            <tr>
                                <td><strong>Job Role:</strong></td>
                                <td>{jobRole}</td>
                            </tr>
                        </table>

                        <p style='margin-top:20px;'>
                            Our team has reviewed your profile and qualifications, and we believe you are a great
                            fit for this opportunity.
                        </p>

                        <p><strong>What happens next?</strong></p>
                        <ul>
                            <li>You may be contacted by the employer for the next steps.</li>
                            <li>Keep your profile updated to improve your visibility.</li>
                            <li>Check your dashboard regularly for updates and messages.</li>
                        </ul>

                        <p>
                            If you have any questions or need assistance, feel free to
                            <a href='#' style='color:#16a34a; text-decoration:none;'>contact our support team</a>.
                        </p>

                        <p>
                            Best Regards,<br/>
                            <strong>The TalentBridgePro Team</strong>
                        </p>
                    </td>
                </tr>

                <!-- Footer -->
                <tr>
                    <td style='background-color:#f4f4f7; text-align:center; padding:15px; font-size:12px; color:#888888;'>
                        &copy; {DateTime.Now.Year} TalentBridgePro. All rights reserved.
                    </td>
                </tr>

            </table>
        </td>
    </tr>
</table>";


                using var mail = new MailMessage
                {
                    From = new MailAddress(_email.SenderEmail),
                    Subject = "Your Application Has Been Accepted 🎉",
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(email);

                await smtp.SendMailAsync(mail);
            }
            catch (SmtpException ex)
            {
                throw new Exception($"Failed to send application accepted email to {email}: {ex.Message}", ex);
            }
        }


        public async Task SendApplicationPendingEmailAsync(
    string email,
    string username,
    string companyName,
    string jobRole)
        {
            try
            {
                using var smtp = new SmtpClient(_email.SmtpServer, _email.Port)
                {
                    Credentials = new NetworkCredential(_email.SenderEmail, _email.Password),
                    EnableSsl = _email.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 20000
                };

                string body = $@"
<table width='100%' cellpadding='0' cellspacing='0' style='font-family: Arial, sans-serif; background-color:#f4f4f7; padding:20px;'>
    <tr>
        <td align='center'>
            <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 0 10px rgba(0,0,0,0.1);'>

                <!-- Header -->
                <tr>
                    <td style='background-color:#2563eb; color:#ffffff; text-align:center; padding:20px; font-size:24px; font-weight:bold;'>
                        TalentBridgePro
                    </td>
                </tr>

                <!-- Body -->
                <tr>
                    <td style='padding:30px; color:#333333; font-size:16px; line-height:1.6;'>
                        <p>Dear {username},</p>

                        <p>
                            Thank you for applying through <strong>TalentBridgePro</strong>.
                            We are pleased to inform you that your application is currently under review.
                        </p>

                        <p><strong>Application Details</strong></p>
                        <table width='100%' cellpadding='8' cellspacing='0' style='background-color:#f9fafb; border-radius:6px;'>
                            
                            <tr>
                                <td><strong>Company:</strong></td>
                                <td>{companyName}</td>
                            </tr>
                            <tr>
                                <td><strong>Job Role:</strong></td>
                                <td>{jobRole}</td>
                            </tr>
                        </table>

                        <p style='margin-top:20px;'>
                            The hiring team is currently reviewing applications. This process may take some time,
                            and we appreciate your patience.
                        </p>

                        <p>
                            We will notify you as soon as there is an update regarding the next steps.
                            In the meantime, feel free to explore other opportunities on our platform.
                        </p>

                        <p>
                            Thank you for your interest and for choosing TalentBridgePro.
                        </p>

                        <p>
                            Kind Regards,<br/>
                            <strong>The TalentBridgePro Team</strong>
                        </p>
                    </td>
                </tr>

                <!-- Footer -->
                <tr>
                    <td style='background-color:#f4f4f7; text-align:center; padding:15px; font-size:12px; color:#888888;'>
                        &copy; {DateTime.Now.Year} TalentBridgePro. All rights reserved.
                    </td>
                </tr>

            </table>
        </td>
    </tr>
</table>";

                using var mail = new MailMessage
                {
                    From = new MailAddress(_email.SenderEmail),
                    Subject = "Your Application Is Under Review",
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(email);

                await smtp.SendMailAsync(mail);
            }
            catch (SmtpException ex)
            {
                throw new Exception($"Failed to send application pending email to {email}: {ex.Message}", ex);
            }
        }

        public async Task SendApplicationRejectedEmailAsync(string email,
    string username,
    string companyName,
    string jobRole)
        {
            try
            {
                using var smtp = new SmtpClient(_email.SmtpServer, _email.Port)
                {
                    Credentials = new NetworkCredential(_email.SenderEmail, _email.Password),
                    EnableSsl = _email.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 20000
                };

                string body = $@"
<table width='100%' cellpadding='0' cellspacing='0' style='font-family: Arial, sans-serif; background-color:#f4f4f7; padding:20px;'>
    <tr>
        <td align='center'>
            <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 0 10px rgba(0,0,0,0.1);'>
                
                <!-- Header -->
                <tr>
                    <td style='background-color:#2563eb; color:#ffffff; text-align:center; padding:20px; font-size:24px; font-weight:bold;'>
                        TalentBridgePro
                    </td>
                </tr>

                <!-- Body -->
                <tr>
                    <td style='padding:30px; color:#333333; font-size:16px; line-height:1.6;'>
                        <p>Dear {username},</p>

                        <p>
                            Thank you for taking the time to apply through <strong>TalentBridgePro</strong>.
                            We appreciate your interest and effort.
                        </p>

                        <p><strong>Application Details</strong></p>
                        <table width='100%' cellpadding='8' cellspacing='0' style='background-color:#f9fafb; border-radius:6px;'>
                            
                            <tr>
                                <td><strong>Company:</strong></td>
                                <td>{companyName}</td>
                            </tr>
                            <tr>
                                <td><strong>Job Role:</strong></td>
                                <td>{jobRole}</td>
                            </tr>
                        </table>

                        <p style='margin-top:20px;'>
                            After careful consideration, we regret to inform you that your application
                            was not selected at this time.
                        </p>

                        <p>
                            This decision does not reflect your abilities or potential. We encourage you
                            to continue exploring other opportunities on our platform that may be a
                            better fit for your skills and experience.
                        </p>

                        <p>
                            Please keep your profile updated and feel free to apply for future openings.
                        </p>

                        <p>
                            We sincerely wish you success in your job search and future career endeavors.
                        </p>

                        <p>
                            Kind Regards,<br/>
                            <strong>The TalentBridgePro Team</strong>
                        </p>
                    </td>
                </tr>

                <!-- Footer -->
                <tr>
                    <td style='background-color:#f4f4f7; text-align:center; padding:15px; font-size:12px; color:#888888;'>
                        &copy; {DateTime.Now.Year} TalentBridgePro. All rights reserved.
                    </td>
                </tr>

            </table>
        </td>
    </tr>
</table>";


                using var mail = new MailMessage
                {
                    From = new MailAddress(_email.SenderEmail),
                    Subject = "Update on Your Application",
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(email);

                await smtp.SendMailAsync(mail);
            }
            catch (SmtpException ex)
            {
                throw new Exception($"Failed to send application rejected email to {email}: {ex.Message}", ex);
            }
        }
        public async Task SendRecruiterNewApplicationEmailAsync(
            string recruiterEmail,
            string recruiterName,
            string applicantName,
            string jobTitle)
        {
            try
            {
                using var smtp = new SmtpClient(_email.SmtpServer, _email.Port)
                {
                    Credentials = new NetworkCredential(_email.SenderEmail, _email.Password),
                    EnableSsl = _email.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 20000
                };

                string body = $@"
<table width='100%' cellpadding='0' cellspacing='0' style='font-family: Arial, sans-serif; background-color:#f4f4f7; padding:20px;'>
    <tr>
        <td align='center'>
            <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 0 10px rgba(0,0,0,0.1);'>
                
                <!-- Header -->
                <tr>
                    <td style='background-color:#2563eb; color:#ffffff; text-align:center; padding:20px; font-size:24px; font-weight:bold;'>
                        TalentBridgePro
                    </td>
                </tr>

                <!-- Body -->
                <tr>
                    <td style='padding:30px; color:#333333; font-size:16px; line-height:1.6;'>
                        <p>Dear {recruiterName},</p>

                        <p>
                            You have received a <strong>new application</strong> for a position at your company.
                        </p>

                        <p><strong>Application Details:</strong></p>
                        <ul style='color:#333333;'>
                            <li><strong>Applicant Name:</strong> {applicantName}</li>
                            <li><strong>Job Title:</strong> {jobTitle}</li>
                        </ul>

                        <p>
                            Please log in to your TalentBridgePro dashboard to review the applicant’s profile,
                            resume, and take the next steps.
                        </p>

                        <p>
                            <a href='#'
                               style='display:inline-block; padding:12px 20px; background-color:#2563eb; color:#ffffff;
                               text-decoration:none; border-radius:6px; font-weight:bold;'>
                                View Application
                            </a>
                        </p>

                        <p>
                            If you have any questions or need assistance, feel free to contact our support team.
                        </p>

                        <p>
                            Best Regards,<br/>
                            <strong>The TalentBridgePro Team</strong>
                        </p>
                    </td>
                </tr>

                <!-- Footer -->
                <tr>
                    <td style='background-color:#f4f4f7; text-align:center; padding:15px; font-size:12px; color:#888888;'>
                        &copy; {DateTime.Now.Year} TalentBridgePro. All rights reserved.<br/>
                        <a href='#' style='color:#888888;'>Privacy Policy</a>
                    </td>
                </tr>

            </table>
        </td>
    </tr>
</table>";

                using var mail = new MailMessage
                {
                    From = new MailAddress(_email.SenderEmail),
                    Subject = $"New Application Received – {jobTitle}",
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(recruiterEmail);

                await smtp.SendMailAsync(mail);
            }
            catch (SmtpException ex)
            {
                throw new Exception($"Failed to send recruiter notification email to {recruiterEmail}: {ex.Message}", ex);
            }
        }

    }
}