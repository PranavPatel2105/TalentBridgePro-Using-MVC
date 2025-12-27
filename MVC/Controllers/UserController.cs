using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Repositories;
using Repositories.Models;
using System.Net.Http;
using System.Text.Json;
using System.Text;


namespace MVC.Controllers
{
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            return View();
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult Login()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]


        // mvc controller
        [HttpGet]
        public IActionResult ChangePassword()
        {
            
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        [HttpPost]
        public IActionResult SaveSession(
         string token,
         int userid,
         string name,
         string email,
         string role)
        {
            Console.WriteLine("SaveSeesion"+token);
            HttpContext.Session.SetString("JWT", token ?? "");
            HttpContext.Session.SetInt32("UserId", userid);
            HttpContext.Session.SetString("Name", name ?? "");
            HttpContext.Session.SetString("Email", email ?? "");
            HttpContext.Session.SetString("Role", role ?? "");
            Console.WriteLine(token);
            return Ok(new { success = true });
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult ForgotPassword()
        {
            return View();
        }
        

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword", "User");
            }

            var model = new t_resetpassword
            {
                c_email = email
            };

            return View(model);
        }

        
    }
}