using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class t_resetpassword
    {
        public string c_email { get; set; }
        public string c_newpassword { get; set; }
        public string c_confirmpassword { get; set; }
    }
}