using MelBoxGsm;
using MelBoxWeb;
using MelBoxSql;
using System;
using System.Collections.Generic;
using System.Text;

namespace MelBoxCore
{
    partial class Program
    {

        #region Properties
        public static string SmsWayValidationTrigger { get; set; } = "SMSAbruf";
        #endregion

        private static void Gsm_GsmStatusReceived(object sender, GsmStatusArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(e.Property + ":\r\n" + e.Value);
            Console.ForegroundColor = ConsoleColor.Gray;

            switch (e.Property)
            {
                case Gsm.Modem.SignalQuality:
                    MelBoxWeb.GsmStatus.SignalQuality = (int)e.Value;
                    break;
                case Gsm.Modem.BitErrorRate:
                    MelBoxWeb.GsmStatus.SignalErrorRate = (double)e.Value;
                    break;
                case Gsm.Modem.OwnPhoneNumber:
                    MelBoxWeb.GsmStatus.OwnNumber = e.Value.ToString();
                    break;
                case Gsm.Modem.OwnName:
                    MelBoxWeb.GsmStatus.OwnName = e.Value.ToString();
                    break;
                case Gsm.Modem.ServiceCenterNumber:
                    MelBoxWeb.GsmStatus.ServiceCenterNumber = e.Value.ToString();
                    break;
                case Gsm.Modem.NetworkRegistration:
                    MelBoxWeb.GsmStatus.NetworkRegistration = e.Value.ToString();
                    if (e.Value.ToString() != "registriert")
                        MelBoxSql.Tab_Log.Insert(Tab_Log.Topic.Gsm, 2, "Mobilfunknetz: " + e.Value);
                    break;
                case Gsm.Modem.ProviderName:
                    MelBoxWeb.GsmStatus.ProviderName = e.Value.ToString();
                    break;
                case Gsm.Modem.IncomingCall:
                    MelBoxSql.Tab_Log.Insert(MelBoxSql.Tab_Log.Topic.Gsm, 2, "Eingehender Anruf von " + e.Value);
                    break;
                default:
                    break;
            }
        }

        private static void Gsm_StatusReportRecievedEvent(object sender, StatusReport e)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("SMS-Sendebestätigung | Status: " + e.SendStatus + " | intern: " + e.InternalReference +"\t" + e.Reciever + ":\r\n" + e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;

            if (e.Reciever == null && e.Message == null) //z.B. nach Neustart 'alte' Sendebestätigungen aus SIM-Speicher
            {
                Console.WriteLine("Die SMS-Sendebestätigung mit der Referrenz " + e.InternalReference + " konnte keiner gesendeten Nachricht zugeordnet werden.");
                return;
            }

            int contactId = MelBoxSql.Tab_Contact.SelectContactId(e.Reciever);
            int contentId = MelBoxSql.Tab_Message.SelectOrCreateMessageId(e.Message);

            //Kontakt unbekannt? Neu erstellen!
            if (contactId == 0)
            {
                Tab_Contact.InsertNewContact(e.Reciever, e.Message);
                contactId = MelBoxSql.Tab_Contact.SelectContactId(e.Reciever);
            }

            //Sendebestätigung in Datenbank schreiben
            MelBoxSql.Sent set = new MelBoxSql.Sent(contactId, contentId, MelBoxSql.Tab_Contact.Communication.Sms)
            {
                Confirmation = e.SendStatus,
                SentTime = e.DischargeTimeUtc                
            };
            
            Console.WriteLine("Gsm_StatusReportRecievedEvent(): Statusreport für Referrenz " + e.InternalReference);

            MelBoxSql.Sent where = new MelBoxSql.Sent()
            {
                Reference = e.InternalReference
            };

            if (! MelBoxSql.Tab_Sent.Update(set, where) )
            {
                Console.WriteLine("Gsm_StatusReportRecievedEvent(): Statusreport für Referrenz " + e.InternalReference + " konnte keiner gesendeten SMS zugeordnet werden.");
            }
        }

        private static void Gsm_SmsRecievedEvent(object sender, ParseSms e)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(e.Sender + ":\r\n" + e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;

            #region SMS-Empfang in Datenbank protokollieren
            int fromId = MelBoxSql.Tab_Contact.SelectContactId(e.Sender);

            if (fromId == 0 ) // Unbekannter Sender
            {
                Tab_Contact.InsertNewContact(e.Sender, e.Message);
                fromId = MelBoxSql.Tab_Contact.SelectContactId(e.Sender);
            }

            int messageId = MelBoxSql.Tab_Message.SelectOrCreateMessageId(e.Message);

            Recieved recieved1 = new Recieved(fromId, messageId)
            {
                RecTime = DateTime.UtcNow
            };
            MelBoxSql.Tab_Recieved.Insert(recieved1);
            #endregion


            #region Meldelinientest 'SmsAbruf'
            if (e.Message.ToLower().Trim() == SmsWayValidationTrigger.ToLower())
            {
                MelBoxGsm.Gsm.Ask_SmsSend(e.Sender, e.Message.Trim() + " um " + DateTime.Now.ToString("HH:mm:ss") + " Uhr.");

                Sent sent = new Sent(fromId, messageId, Tab_Contact.Communication.Sms)
                {
                    SentTime = DateTime.UtcNow
                };
                MelBoxSql.Tab_Sent.Insert(sent);
                
                return;
            }
            #endregion


            #region An Bereitschaft senden
            //Bereitschaft ermitteln
            List<MelBoxSql.Shift> currentShifts = MelBoxSql.Tab_Shift.SelectOrCreateCurrentShift();

            //an Bereitschaft weiterleiten
            foreach (var shift in currentShifts)
            {
                Contact to = MelBoxSql.Tab_Contact.SelectContact(shift.Id);

                //Email
                if ((to.Via & Tab_Contact.Communication.Email) > 0)
                {
                    //BAUSTELLE
                    Console.WriteLine("Email nicht implementiert. Keine Email an " + to.Name + "\t" + to.Email);
                }

                //SMS
                if ((to.Via & Tab_Contact.Communication.Sms) > 0)
                {
                    Sent sent = new Sent(shift.ContactId, messageId, Tab_Contact.Communication.Sms)
                    {
                        Confirmation = -1
                    };

                    MelBoxSql.Tab_Sent.Insert(sent);
                    MelBoxGsm.Gsm.Ask_SmsSend("+" + to.Phone.ToString(), e.Message);
                }
            }
            #endregion
        }

        private static void Gsm_SmsSentEvent(object sender, ParseSms e)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Versendet " + e.Sender + ":\r\n" + e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;

            int toId = MelBoxSql.Tab_Contact.SelectContactId(e.Sender);
            int contentId = MelBoxSql.Tab_Message.SelectOrCreateMessageId(e.Message);

            //Kontakt unbekannt? Neu erstellen!
            if (toId == 0)
            {
                Tab_Contact.InsertNewContact(e.Sender, e.Message);
                toId = MelBoxSql.Tab_Contact.SelectContactId(e.Sender);
            }

            //'SMS gesendet' in Datenbank schreiben
            MelBoxSql.Sent sent = new MelBoxSql.Sent(toId, contentId, MelBoxSql.Tab_Contact.Communication.Sms);
            sent.Reference = e.InternalReference;
            sent.Confirmation = 255;
            sent.SentTime = e.TimeUtc;
            
            MelBoxSql.Tab_Sent.Insert(sent);
        }

    }

}
