using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MVC.Controllers
{
    public class ApplicantController : Controller
    {
        private readonly ILogger<ApplicantController> _logger;

        public ApplicantController(ILogger<ApplicantController> logger)
        {
            _logger = logger;
        }

        private bool IsApplicantLoggedIn()
        {
            var jwt = HttpContext.Session.GetString("JWT");
            var role = HttpContext.Session.GetString("Role");

            return !string.IsNullOrEmpty(jwt) && role == "applicant";
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Dashboard()
        {
            if (!IsApplicantLoggedIn())
                return RedirectToAction("Login", "User");

            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Profile()
        {
            if (!IsApplicantLoggedIn())
                return RedirectToAction("Login", "User");

            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult MyJobs()
        {
            if (!IsApplicantLoggedIn())
                return RedirectToAction("Login", "User");

            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult ApplyJob(int jobId)
        {
            if (!IsApplicantLoggedIn())
                return RedirectToAction("Login", "User");
             ViewBag.JobId = jobId;
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult DetailPopup()
        {
            if (!IsApplicantLoggedIn())
                return RedirectToAction("Login", "User");

            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult Register()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult tempCompanyReview()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
