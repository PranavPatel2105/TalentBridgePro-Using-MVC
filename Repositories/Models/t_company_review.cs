using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Repositories.Models
{
    public class t_company_review
    {
        public int c_review_id { get; set; }
        public int c_company_id { get; set; }
        public int c_userid { get; set; }
        public string c_description { get; set; }
        public int c_stars { get; set; }
    }
}