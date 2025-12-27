using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Models;


namespace Repositories.Interfaces

{
    public interface IUserInterface
    {
        Task<List<t_user>> GetUser(string? filter=null);
        Task<int> RegisterUser(t_user user);
        Task<int> ResetPassword(t_resetpassword resetpassword);
       Task<int> ChangePassword(int userId, vm_changePassword changePassword);
       Task<string?> GetPasswordByUserId(int userId);
    }
}
