using SerAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SerAPI.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string name, string email, string subject, string message, string template);

        //
        // Resumen:
        //     This API supports the ASP.NET Core Identity default UI infrastructure and is
        //     not intended to be used directly from your code. This API may change or be removed
        //     in future releases.
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
