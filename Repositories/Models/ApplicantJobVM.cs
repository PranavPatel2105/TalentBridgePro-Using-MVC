using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class ApplicantJobVM
    {
        public int c_job_id { get; set; }
        public string c_role { get; set; }
        public string c_company_name { get; set; }
        public string c_location { get; set; }
        public string c_job_type { get; set; }
        public string c_experience { get; set; }
        public string c_salary { get; set; }
        public string c_skills { get; set; }
        public string c_status { get; set; }

    }
}