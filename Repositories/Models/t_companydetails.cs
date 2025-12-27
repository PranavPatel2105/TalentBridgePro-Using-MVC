using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Repositories.Models
{
    public class t_companydetails
    {
        public int c_company_id { get; set; }
        public int c_userid { get; set; }
        public string? c_company_email { get; set; }
        public string? c_name { get; set; }
        public string? c_image { get; set; }
        public string? c_state { get; set; }
        public string? c_city { get; set; }

        public IFormFile? imageform { get; set; }

    }
}