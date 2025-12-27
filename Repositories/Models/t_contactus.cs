using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class t_contactus
    {
        public int c_contactus_id { get; set; }
        public string c_subject { get; set; }
        public string c_message { get; set; }
        public int c_userid { get; set; }
    }
}