using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class t_educationdetails
    {
        public int c_education_detail_id { get; set; }
        public int c_userid { get; set; }
        public string c_schoolname { get; set; }
        public string c_degree { get; set; }
        public string c_fieldofstudy { get; set; }
        public int c_startyear { get; set; }
        public int c_endyear { get; set; }
        public string c_state { get; set; }
        public string c_city { get; set; }

    }
}