using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Repositories.Models;

namespace MVC.Controllers
{
    public class RecruiterController : Controller
    {
        private readonly ILogger<RecruiterController> _logger;

        public RecruiterController(ILogger<RecruiterController> logger)
        {
            _logger = logger;
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            if(HttpContext.Session.GetString("Role") != "recruiter" || HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "User");
            }   
            return View();
        }
        
         [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult CompanyDetailPopup()
        {

              if(HttpContext.Session.GetString("Role") != "recruiter" || HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "User");
            }   
            
            return View();
        }
        public IActionResult CreateJob()
        {
              if(HttpContext.Session.GetString("Role") != "recruiter" || HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "User");
            }   
            var jwt = HttpContext.Session.GetString("JWT");
            Console.WriteLine("JWT in CreateJob: " + jwt);

            ViewBag.CompanyLogo = "recruiter1.png";
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        [HttpPost]
        public async Task<IActionResult> GenerateDescription([FromBody] object data)
        {
              if(HttpContext.Session.GetString("Role") != "recruiter" || HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "User");
            }   
            using var client = new HttpClient();

            var response = await client.PostAsJsonAsync(
                "http://localhost:5140/api/RecruiterApi/generate-description",
                data
            );

            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }
                [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult EditCompany()
        {
              if(HttpContext.Session.GetString("Role") != "recruiter" || HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "User");
            }   
            return View();
        }
                [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult EditRecruiter()
        {
              if(HttpContext.Session.GetString("Role") != "recruiter" || HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "User");
            }   

            return View();
        }
        [HttpGet]
        public IActionResult ChangePassword()
        
        {
            
              if(HttpContext.Session.GetString("Role") != "recruiter" || HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "User");
            }   
            
            return View();
        }
                [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult ViewApplication(int jobId)
        {
              if(HttpContext.Session.GetString("Role") != "recruiter" || HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "User");
            }   
            ViewBag.JobId = jobId;
            return View();
        }
                [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult ContactUs()
        {
              if(HttpContext.Session.GetString("Role") != "recruiter" || HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "User");
            }   
            return View();
        }

        
                [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Register()
        {
            //   if(HttpContext.Session.GetString("Role") != "recruiter" || HttpContext.Session.GetString("UserId") == null)
            // {
            //     return RedirectToAction("Login", "User");
            // }   
            return View();
        }
        

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
 //==================================================================================================================================================

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
 public IActionResult ViewApplicants()
        {
               if(HttpContext.Session.GetString("Role") != "recruiter" || HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "User");
            }   
            return View();
            
        }
       

       public IActionResult ApplicantDetails(int userid)
        {
            if (HttpContext.Session.GetString("Role") != "recruiter")
                return RedirectToAction("Login", "User");

            ViewBag.UserId = userid;
            return View();
        }
    }
}