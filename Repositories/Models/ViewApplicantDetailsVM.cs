using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class ViewApplicantDetailsVM
    {
        // t_user
        public int c_userid { get; set; }
        public string c_name { get; set; }
        public string c_email { get; set; }


        // t_userdetails
        public string? c_skills { get; set; }
        public string? c_bio { get; set; } 
        public string? c_resume_file { get; set; }
    }
}