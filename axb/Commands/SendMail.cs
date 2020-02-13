using Microsoft.Dynamics.AX.Framework.Management;
using Microsoft.Dynamics.AX.Framework.Management.Reports;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Common;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace axb.Commands
{
    public class SendMail
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        public void log(string msg)
        {
            logger.Info(msg);
        }
        
        public async Task<int> RunAsync(SendMailOptions options)
        {
            await Task.Run(() => this.SendMailMessage(options));

            return 0;
        }

        private static bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            var redirectionUri = new Uri(redirectionUrl);
            var result = redirectionUri.Scheme == "https";
            return result;
        }

        public void SendMailMessage(SendMailOptions options)
        {
            var service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
            service.Credentials =
                new WebCredentials(options.FromUsername, options.FromPassword);
            service.TraceEnabled = true;
            service.TraceFlags = TraceFlags.All;
            service.AutodiscoverUrl(options.From,
                RedirectionUrlValidationCallback);
            EmailMessage email = new EmailMessage(service);

            var mails = options.To.Split(',');

            foreach (var mail in mails)
            {
                log("adding: '" + mail.Trim() + "'");
                email.ToRecipients.Add(mail.Trim());
            }

            email.Subject = options.Subject;
            email.Body = new MessageBody(options.Body);
            email.Body.BodyType = BodyType.HTML;
            email.Send();
        }
    }
}
