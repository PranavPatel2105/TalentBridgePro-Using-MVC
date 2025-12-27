using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class ViewApplicantFullDetailsVM
    {
        public int c_userid { get; set; }
        public string c_name { get; set; }
        public string c_email { get; set; }

        public List<t_educationdetails> Educations { get; set; } = new();
        public List<t_certificate> Certificates { get; set; } = new();
    }
}