using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SerAPI.Pages
{
    [Authorize]
    public class PrivacyModel : PageModel
    {
        public SignInManager<IdentityUser> _signInManager;

        public PrivacyModel(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;

        }
        public void OnGet()
        {
            if (_signInManager.IsSignedIn(User))
            {
                Console.WriteLine($"-----------------------------user is {User.Identity.Name} signed");
            }
            else
            {
                Console.WriteLine($"-----------------------------user is not sign in");
            }
        }
    }
}
