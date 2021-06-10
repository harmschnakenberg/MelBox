using System;
using System.Net.Mail;

namespace MelBoxCore
{
    //Quelle: MelBox
    /* EMailSMTP = kreutztraeger-de.mail.protection.outlook.com               ' (192.168.165.29) SMTP 
     * EMailsTo = u.wenzel@kreutztraeger.de; thomas.schulz@kreutztraeger.de; bernd.kreutztraeger@kreutztraeger.de; d.brylski@kreutztraeger.de; a.fecke@kreutztraeger.de; christian.juds@kreutztraeger.de; steven.kemper@kreutztraeger.de; gunnar.foertsch@kreutztraeger.de; david.eichhorn@kreutztraeger.de
     */

    //Hinweis: Empfang von Emails ist mit System.Net.Mail in .NET Core (noch) nicht möglich.
    class Email
    {       
        public static MailAddress From = new MailAddress("SMSZentrale@Kreutztraeger.de", "SMS-Zentrale");

        public static MailAddress Admin = new MailAddress("harm.Schnakenberg@Kreutztraeger.de", "MelBox2 Admin");

        public static string SmtpHost { get; set; } = "kreutztraeger-de.mail.protection.outlook.com";
        public static int SmtpPort { get; set; } = 25; //587;
        public static bool SmtpEnableSSL { get; set; } = false;
        public static string SmtpUser { get; set; } = "";
        public static string SmtpPassword { get; set; } = "";

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

        /// <summary>
        /// Sende Email an einen Empfänger.
        /// Sendungsverfolung wird nicht in der Datenbank protokolliert.
        /// </summary>
        /// <param name="to">Empfänger der Email</param>
        /// <param name="message">Inhalt der Email</param>
        /// <param name="subject">Betreff. Leer: Wird aus message generiert.</param>
        /// <param name="sendCC">Sende an Ständige Empänger in CC</param>
        public static void Send(MailAddress to, string message, string subject = "", bool sendCC = true)
        {
            var toList = new MailAddressCollection { to };

            Send(toList, message, subject, 0, sendCC);
        }

        /// <summary>
        /// Sende Email an eine Empängerliste.        
        /// </summary>
        /// <param name="toList">Empfängerliste</param>
        /// <param name="message">Inhalt der Email</param>
        /// <param name="subject">Betreff. Leer: Wird aus message generiert.</param>
        /// <param name="emailId">Id zur Protokollierung der Sendungsverfolgung in der Datenbank</param>
        /// <param name="sendCC">Sende an Ständige Empänger in CC</param>
        public static void Send (MailAddressCollection toList, string message, string subject = "", int emailId = 0, bool sendCC = true)
        {
            Console.WriteLine("Sende Email: " + message);

            if (emailId == 0) emailId = (int)(DateTime.UtcNow.Ticks % int.MaxValue);

            MailMessage mail = new MailMessage(); 

            try
            {                
                #region From
                mail.From = From;
                mail.Sender = From;
                #endregion

                #region To               
                foreach (var to in toList ?? new MailAddressCollection() { Admin } )
                {
#if DEBUG           //nur zu mir
                    if (to.Address.ToLower() != Admin.Address.ToLower())
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
                        if (cc.Address.ToLower() != Admin.Address.ToLower())
                            Console.WriteLine("Send(): Emailadresse CC gesperrt: " + cc.Address);
                        else
#endif
                            if (!mail.To.Contains(cc))
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
                //Siehe https://docs.microsoft.com/de-de/dotnet/api/system.net.mail.smtpclient.sendasync?view=net-5.0

                using var smtpClient = new SmtpClient();
             
                smtpClient.Host = SmtpHost;
                smtpClient.Port = SmtpPort;
                
                if (SmtpUser.Length > 0 && SmtpPassword.Length > 0) 
                    smtpClient.Credentials = new System.Net.NetworkCredential(SmtpUser, SmtpPassword);
                
                //smtpClient.UseDefaultCredentials = true;

                smtpClient.EnableSsl = SmtpEnableSSL;

                smtpClient.Send(mail);

                //smtpClient.SendCompleted += SmtpClient_SendCompleted;  
                //smtpClient.SendAsync(mail, emailId); //emailId = Zufallszahl größer 255 (Sms-Ids können zwischen 0 bis 255 liegen)
                #endregion
            }
            catch (SmtpFailedRecipientsException ex)
            {
                for (int i = 0; i < ex.InnerExceptions.Length; i++)
                {
                    SmtpStatusCode status = ex.InnerExceptions[i].StatusCode;
                    if (status == SmtpStatusCode.MailboxBusy ||
                        status == SmtpStatusCode.MailboxUnavailable)
                    {
                        MelBoxSql.Tab_Log.Insert(MelBoxSql.Tab_Log.Topic.Email, 1, $"Senden der Email [{emailId}] fehlgeschlagen. Neuer Sendeversuch.");
                        MelBoxSql.Tab_Sent.UpdateSendStatus(emailId, MelBoxSql.Tab_Sent.Confirmation.RetrySending);

                        System.Threading.Thread.Sleep(5000);
                        using var smtpClient = new SmtpClient();
                        smtpClient.Send(mail);
                    }
                    else
                    {
                        MelBoxSql.Tab_Log.Insert(MelBoxSql.Tab_Log.Topic.Email, 1, $"Fehler beim Senden der Email [{emailId}] an >{ex.InnerExceptions[i].FailedRecipient}<: {ex.InnerExceptions[i].Message}");
                        MelBoxSql.Tab_Sent.UpdateSendStatus(emailId, MelBoxSql.Tab_Sent.Confirmation.AbortedSending);
                    }

                }
            }
            catch (System.Net.Mail.SmtpException ex_smtp)
            {
                MelBoxSql.Tab_Log.Insert(MelBoxSql.Tab_Log.Topic.Email, 1, "Fehler beim Versenden einer Email: " + ex_smtp.Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            MelBoxSql.Tab_Sent.UpdateSendStatus(emailId, MelBoxSql.Tab_Sent.Confirmation.SentSuccessful);
            mail.Dispose();
        }

        /// <summary>
        /// Nur bei Asynchronem Versenden von Mails - löschen?
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void SmtpClient_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            int emailId = (int)e.UserState;

            if (e.Cancelled)
            {
                MelBoxSql.Tab_Log.Insert(MelBoxSql.Tab_Log.Topic.Email, 1, $"Senden der Email [{emailId}] abgebrochen.");
                MelBoxSql.Tab_Sent.UpdateSendStatus(emailId, MelBoxSql.Tab_Sent.Confirmation.AbortedSending);
            }
            if (e.Error != null)
            {
                MelBoxSql.Tab_Log.Insert(MelBoxSql.Tab_Log.Topic.Email, 1, $"Fehler beim Senden der Email [{emailId}]: {e.Error}");
                MelBoxSql.Tab_Sent.UpdateSendStatus(emailId, MelBoxSql.Tab_Sent.Confirmation.AbortedSending);
            }
            else
            {
                MelBoxSql.Tab_Sent.UpdateSendStatus(emailId, MelBoxSql.Tab_Sent.Confirmation.SentSuccessful);
            }
        }

    }
}
