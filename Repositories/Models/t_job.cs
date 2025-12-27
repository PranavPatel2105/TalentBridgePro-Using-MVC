using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Nest;

namespace Repositories.Models
{
    public class t_job
    {
        [Key]//primary key
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int c_job_id { get; set; }
        
        public int c_userid { get; set; }
        
        public int c_company_id { get; set; }
        public string? c_company_name { get; set; }
        public string c_job_type { get; set; }
        
        public string c_skills { get; set; }
        
        public string c_role { get; set; }
        
        public string c_location { get; set; }
        
        public string c_experience { get; set; }
        
        public string c_description { get; set; }
        public string c_salary { get; set; }
        public string? c_jd_file { get; set; }
        
        [Ignore]
        public IFormFile? jdform { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime c_created_at { get; set; } = DateTime.UtcNow;

        public string? c_name { get; set; }
    }
}