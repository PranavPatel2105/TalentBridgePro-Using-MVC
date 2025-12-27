using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class t_userexperience
    {
        public int c_experience_id { get; set; }
        public int c_userid { get; set; }
        public string c_title { get; set; }
        public string c_employment_type { get; set; }
        public string c_role { get; set; }
        public string c_company { get; set; }
        public DateTime c_start_date { get; set; }
        public DateTime c_end_date { get; set; }
        public string c_state { get; set; }
        public string c_city { get; set; }

    }
}