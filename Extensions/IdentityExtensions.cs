using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Models;

namespace Server.Extensions
{
    public static class IdentityExtensions
    {
        public static async Task<ApplicationUser> FindByUserNameOrEmailAsync(this ApplicationUserManager userManager, string userNameOrEmail, string passWord)
        {
            var userName = userNameOrEmail;
            if (userName.Contains("@"))
            {
                var user = await userManager.FindByEmailAsync(userNameOrEmail);
                if (user != null && user.EmailConfirmed)
                {
                    userName = user.UserName;
                }
            }

            return await userManager.FindAsync(userName, passWord);
        }

        public static async Task<ApplicationUser> FindByUserNameOrEmailAsync(this ApplicationUserManager userManager, string userNameOrEmail)
        {
            var userName = userNameOrEmail;
            if (userName.Contains("@"))
            {
                var user = await userManager.FindByEmailAsync(userNameOrEmail);
                if (user != null && user.EmailConfirmed)
                {
                    userName = user.UserName;
                }
            }

            return await userManager.FindByNameAsync(userName);
        }


    }
}
