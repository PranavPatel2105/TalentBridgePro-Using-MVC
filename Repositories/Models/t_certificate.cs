using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Repositories.Models
{
    public class t_certificate
    {
        public int c_certificate_id { get; set; }
        public int c_userid { get; set; }
        public string c_certificatename { get; set; }
        public string? c_certificatefile { get; set; }
        public IFormFile? certificateform { get; set; }

    }
}