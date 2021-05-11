using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace MelBoxCore
{
    //Quelle: MelBox
    /* EMailSMTP = kreutztraeger-de.mail.protection.outlook.com               ' (192.168.165.29) SMTP 
     * EMailsTo = u.wenzel@kreutztraeger.de; thomas.schulz@kreutztraeger.de; bernd.kreutztraeger@kreutztraeger.de; d.brylski@kreutztraeger.de; a.fecke@kreutztraeger.de; christian.juds@kreutztraeger.de; steven.kemper@kreutztraeger.de; gunnar.foertsch@kreutztraeger.de; david.eichhorn@kreutztraeger.de
     */
    class Email
    {
        public static string From { get; set; } = "SMSZentrale@Kreutztraeger.de";

        static MailAddressCollection permanentRecievers = new MailAddressCollection();

        //private static void LoadDefault()
        //{
        //    string _AppFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        //    string configPath = System.IO.Path.Combine(_AppFolder, "Config", "MelBoxConfig.cfg");

        //    if (!System.IO.File.Exists(configPath)) return;

        //    string[] lines = System.IO.File.ReadAllLines(configPath);

        //    foreach (var line  in lines)
        //    {
        //        if (line.StartsWith("EMailsTo"))
        //        line.Split('=')
        //    }

        //permanentRecievers.Add(new MailAddress("u.wenzel@kreutztraeger.de"));
        //}

        public static void SendEmail()
        {
            MailMessage mailMsg = new MailMessage();
            mailMsg.To.Add("test@hotmail.com");
            // From
            MailAddress mailAddress = new MailAddress("you@hotmail.com");
            mailMsg.From = mailAddress;

            // Subject and Body
            mailMsg.Subject = "subject";
            mailMsg.Body = "body";

            // Init SmtpClient and send on port 587 in my case. (Usual=port25)
            SmtpClient smtpClient = new SmtpClient("mailserver", 587);
            System.Net.NetworkCredential credentials =
               new System.Net.NetworkCredential("username", "password");
            smtpClient.Credentials = credentials;

            smtpClient.Send(mailMsg);
        }

    }
}
