using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Repositories.Models;

public class vm_Login
{
    [StringLength(100)]
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid Email formate.")]
    public string c_email { get; set; }

    [StringLength(100)]
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Paaword must be 6 character long")]
    public string c_password { get; set; }
}
