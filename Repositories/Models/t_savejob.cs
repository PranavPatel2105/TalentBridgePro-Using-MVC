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
    public class t_savejob
    {
        [Key]//primary key
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int c_saveid { get; set; }
        
        public int c_job_id { get; set; }
        
        public string c_status { get; set; }
    
    }
}