using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Models.ViewModels
{
    public class UserExternalLogin
    {
        //
        // Summary:
        //     Gets or sets the provider for this instance of Microsoft.AspNetCore.Identity.UserLoginInfo.
        //
        // Remarks:
        //     Examples of the provider may be Local, Facebook, Google, etc.
        public string Provider { get; set; }
      
        //
        // Summary:
        //     Gets or sets the unique identifier for the user identity user provided by the
        //     login provider.
        //
        // Remarks:
        //     This would be unique per provider, examples may be @microsoft as a Twitter provider
        //     key.
        public string Token { get; set; }
    }
}
