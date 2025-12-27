using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Repositories.Models
{
    public class t_userdetails
    {
        // Dev UserDeatils Changes ============================
        public int c_details_id { get; set; }
        public int c_userid { get; set; }
        public string? c_bio { get; set; }
        public string? c_skills { get; set; }
        public string? c_resume_file { get; set; }
        public string? c_job_role { get; set; }
        public string? c_job_type { get; set; }
        public string? c_location { get; set; }
        [FromForm]
        public IFormFile? resumeform { get; set; }

    }
}