using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories
{
    public class vm_changePassword
    {
        [Required(ErrorMessage ="Old Password is Required!")]
        [MinLength(8,ErrorMessage ="Old Password length must be 8 charcter.")]
        public string c_oldpassword { get; set; }

        [Required(ErrorMessage ="New Password is Required!")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",ErrorMessage ="Password must contain at least 8 characters, including one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string c_newpassword { get; set; }

        
    }
}