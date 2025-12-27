using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class t_admin
    {
        public int c_admin_id { get; set; }
        public string c_email { get; set; }
        public string c_password { get; set; }
        public string c_role { get; set; }
    }
}