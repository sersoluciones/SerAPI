using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Net.Http;
using Newtonsoft.Json;
using SerAPI.Utils;
using SerAPI.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SerAPI.Services
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link https://go.microsoft.com/fwlink/?LinkID=532713
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        private IConfiguration _config;
        private readonly IViewRenderService _viewRenderService;
        private readonly SMSoptions _options;
        private string SMTP_SERVERNAME;
        private int SMTP_PORT;
        private string SMTP_USERNAME;
        private string SMTP_PASSWORD;

        public AuthMessageSender(IConfiguration config, IViewRenderService viewRenderService,
             IOptionsMonitor<SMSoptions> optionsAccessor)
        {
            _config = config;
            _viewRenderService = viewRenderService;
            _options = optionsAccessor.CurrentValue;

            SMTP_SERVERNAME = _config.GetSection("AWS").GetSection("SES").GetSection("SMTP").GetSection("ServerName").Value;
            SMTP_PORT = _config.GetSection("AWS").GetSection("SES").GetSection("SMTP").GetValue<int>("Port");
            SMTP_USERNAME = _config.GetSection("AWS").GetSection("SES").GetSection("SMTP").GetSection("Username").Value;
            SMTP_PASSWORD = _config.GetSection("AWS").GetSection("SES").GetSection("SMTP").GetSection("Password").Value;
        }

        public Task SendEmailAsync(string name, string email, string subject, string message, string template)
        {
            // Plug in your email service here to send an email.
            EmailBinding model = new EmailBinding
            {
                name = name,
                email = email,
                subject = subject,
                message = message,
                template = template
            };

            return SendEmail(model);
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            var SMSAPI = _options.SMSAccountAPI;
            var SMSCliente = _options.SMSAccountCliente;

            return SendSMS(new SMSBinding()
            {
                api = SMSAPI,
                cliente = SMSCliente,
                numero = number,
                sms = message
            });
        }

        public async Task SendSMS(SMSBinding model)
        {
            var url = "https://api.hablame.co/sms/envio/";
            using (var client = new HttpClient())
            {
                var jsonInString = System.Text.Json.JsonSerializer.Serialize(model);
                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonInString);

                var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(dictionary) };
                var response = await client.SendAsync(req);

                response.EnsureSuccessStatusCode();
                string stringResult = await response.Content.ReadAsStringAsync();
                var jObjRes = JObject.Parse(stringResult);
                if ((int)jObjRes["resultado"] == 1) Console.WriteLine($"Mensaje no enviado {jObjRes}");
                else if ((int)jObjRes["resultado"] == 0) Console.WriteLine("Mensaje enviado exitosamente");
            }
        }

        private MimeMessage GetEmailConfig(EmailBinding model)
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress(
                _config.GetSection("AWS").GetSection("SES").GetSection("From").GetSection("Name").Value,
                _config.GetSection("AWS").GetSection("SES").GetSection("From").GetSection("Mail").Value));
            emailMessage.To.Add(new MailboxAddress(model.email, model.email));
            emailMessage.Subject = model.subject;
            return emailMessage;
        }

        public async Task SendEmail(EmailBinding model)
        {
            var emailMessage = GetEmailConfig(model);
            var builder = new BodyBuilder();

            // Set the html version of the message text
            var Data = new EmailTemplateBindingModel
            {
                FirstName = model.name,
                Href = model.message
            };

            builder.HtmlBody = await _viewRenderService.RenderToStringAsync<PageModel>(model.template, Data);
            await SendEmail(emailMessage, builder);

        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return Task.FromResult(0);
        }

        private async Task SendEmail(MimeMessage emailMessage, BodyBuilder builder)
        {
            // Now we just need to set the message body and we're done
            emailMessage.Body = builder.ToMessageBody();

            using (var client = new SmtpClient())
            {

                await client.ConnectAsync(SMTP_SERVERNAME, SMTP_PORT, false);

                // Note: since we don't have an OAuth2 token, disable
                // the XOAUTH2 authentication mechanism.
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                // Note: only needed if the SMTP server requires authentication
                client.Authenticate(SMTP_USERNAME, SMTP_PASSWORD);

                client.Send(emailMessage);
                client.Disconnect(true);
            }

        }
    }
}
