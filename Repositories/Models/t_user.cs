using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class t_user
    {
        [Key]
        public int c_userid { get; set; }


        public string c_name { get; set; }


        public string c_gender { get; set; }


        
        public string c_contactno { get; set; }


        public string c_email { get; set; }


        public string c_address { get; set; }


        public string c_password { get; set; }


        public string c_confirmpassword { get; set; }

        
        public string c_role { get; set; }

        public string c_status { get; set; }


        public DateTime c_dob { get; set; }


        public string? c_profile_image { get; set; }

        public IFormFile? imageform { get; set; }
    }
}