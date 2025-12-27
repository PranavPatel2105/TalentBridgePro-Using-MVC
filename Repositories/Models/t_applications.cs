using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Repositories.Models
{
    public class t_applications
    {
        public int c_application_id { get; set; }
        public int c_userid { get; set; }
        public int c_job_id { get; set; }
        public string c_name { get; set; }
        public string c_email { get; set; }
        public string? c_resume_file { get; set; }
        //optional

        
        public string? c_status { get; set; }

        public IFormFile? resumeform { get; set; }
        

    }
}