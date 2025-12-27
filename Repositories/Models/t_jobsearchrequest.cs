using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class t_jobsearchrequest
    {
    public string? Keyword { get; set; }
    public List<string>? JobTypes { get; set; }
    public List<string>? ExperienceLevels { get; set; }
    public int? MinSalary { get; set; }
     public int? MaxSalary { get; set; }
    public string? SortBy { get; set; }
    
    }
}