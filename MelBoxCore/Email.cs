using System;
using System.Net.Mail;

namespace MelBoxCore
{
    //Quelle: MelBox
    /* EMailSMTP = kreutztraeger-de.mail.protection.outlook.com               ' (192.168.165.29) SMTP 
     * EMailsTo = u.wenzel@kreutztraeger.de; thomas.schulz@kreutztraeger.de; bernd.kreutztraeger@kreutztraeger.de; d.brylski@kreutztraeger.de; a.fecke@kreutztraeger.de; christian.juds@kreutztraeger.de; steven.kemper@kreutztraeger.de; gunnar.foertsch@kreutztraeger.de; david.eichhorn@kreutztraeger.de
     */
    class Email
    {       
        public static readonly MailAddress From = new MailAddress("SMSZentrale@Kreutztraeger.de", "SMS-Zentrale");

        public static readonly MailAddress Admin = new MailAddress("harm.Schnakenberg@Kreutztraeger.de", "MelBox2 Admin");

        public static string SmtpHost { get; set; } = "kreutztraeger-de.mail.protection.outlook.com";
        public static int SmtpPort { get; set; } = 25; //587;

        private static MailAddressCollection permanentRecievers = null;

        private static MailAddressCollection GetPermenentRecievers()
        {
            if (permanentRecievers == null)
            {
                Console.WriteLine("Ermittle ständige Empfänger:");
                System.Data.DataTable dt = MelBoxSql.Tab_Contact.SelectPermanentEmailRecievers();

                MailAddressCollection collection = new MailAddressCollection();

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    MailAddress mailAddress = new MailAddress(dt.Rows[i]["Email"].ToString(), dt.Rows[i]["Name"].ToString());
                    Console.WriteLine(mailAddress);
                    collection.Add(mailAddress);
                }

                permanentRecievers = collection;
            }

            return permanentRecievers;
        }


        public static void Send (MailAddressCollection toList, string message, string subject = "", bool sendCC = true)
        {
            Console.WriteLine("Sende Email: " + message);

            try
            {
                MailMessage mail = new MailMessage();

                #region From
                mail.From = From;
                #endregion

                #region To               
                foreach (var to in toList ?? new MailAddressCollection() { Admin } )
                {
#if DEBUG           //nur zu mir
                    if (to.Address != Admin.Address)
                        Console.WriteLine("Send(): Emailadresse gesperrt: " + to.Address);
                    else
#endif
                        mail.To.Add(to);
                }

                if (sendCC)
                {
                    foreach (var cc in GetPermenentRecievers() ?? new MailAddressCollection())
                    {
#if DEBUG               //nur zu mir
                        if (cc.Address == Admin.Address)
                            Console.WriteLine("Send(): Emailadresse CC gesperrt: " + cc.Address);
                        else
#endif
                            mail.CC.Add(cc);
                    }
                }
                #endregion

                #region Message
                if (subject.Length > 0)
                    mail.Subject = subject;
                else
                {
                    mail.Subject = message.Replace(System.Environment.NewLine, "");
                }

                mail.Body =  message ;
                #endregion

                #region Smtp
                using var smtpClient = new SmtpClient();
                smtpClient.SendCompleted += SmtpClient_SendCompleted;
                smtpClient.Host = SmtpHost;
                smtpClient.Port = SmtpPort;

                //smtpClient.UseDefaultCredentials = true;
                //smtpClient.EnableSsl = true;

                smtpClient.Send(mail);
                smtpClient.SendCompleted -= SmtpClient_SendCompleted;
                #endregion

            }
            catch (System.Net.Mail.SmtpException ex_smtp)
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("Fehler beim Versenden einer Email: " + ex_smtp.Message);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static void SmtpClient_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Console.WriteLine("Email versendet: " + e.UserState + Environment.NewLine + e.Error);
        }
    }
}
