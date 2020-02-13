using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb.Commands
{
    [Verb("sendmail", HelpText = "Send mail")]
    public class SendMailOptions : ICommandOptions
    {
        [Option('s', "subject", Required = true, HelpText = "mail subject")]
        public string Subject { get; set; }

        [Option('b', "body", Required = true, HelpText = "mail body")]
        public string Body { get; set; }

        [Option('f', "frommail", Required = true, HelpText = "mail from")]
        public string From { get; set; }

        [Option('u', "fromusername", Required = true, HelpText = "mail from username")]
        public string FromUsername { get; set; }

        [Option('p', "frompassword", Required = true, HelpText = "mail from password")]
        public string FromPassword { get; set; }

        [Option('t', "tomail", Required = true, HelpText = "mail to")]
        public string To { get; set; }
    }
}
