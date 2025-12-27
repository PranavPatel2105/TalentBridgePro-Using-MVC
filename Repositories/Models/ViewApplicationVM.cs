using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class ViewApplicationVM
    {
          // t_job
        public int c_job_id { get; set; }
        public string c_role { get; set; }
        public string c_location { get; set; }

        // t_applications
        public int c_application_id { get; set; }
        public string c_status { get; set; }
        public string? c_resume_file { get; set; }

        // t_user (applicant)
        public int c_userid { get; set; }
        public string c_name { get; set; }
        public string c_email { get; set; }
    }
}