using MelBoxGsm;
using MelBoxWeb;
using MelBoxSql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;

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
                Console.WriteLine("SMS-Sendebestätigungs-Nr. intern: " + e.InternalReference);
                Console.ForegroundColor = ConsoleColor.Gray;
            
            //TEST: In Hilfstabelle schreiben
            MelBoxSql.Sql.InsertReportProtocoll(e.DischargeTimeUtc, e.InternalReference);

            int gsmSendStatus = e.SendStatus; //<st> 0-31 erfolgreich versandt; 32-63 versucht weiter zu senden: 64-127 Sendeversuch abgebrochen
            Tab_Sent.Confirmation confirmation = Tab_Sent.Confirmation.Unknown;

            if (gsmSendStatus > 127)
                confirmation = Tab_Sent.Confirmation.AwaitingRefernece;
            else if (gsmSendStatus > 63)
            {
                confirmation = Tab_Sent.Confirmation.AbortedSending;

                string email = $"SMS Absender >{e.Reciever}<\r\nSMS Text >{e.Message}<\r\nSMS Sendezeit >{e.DischargeTimeUtc}<\r\n\r\nWeiterleitungsfehler!";
                MelBoxSql.Tab_Log.Insert(Tab_Log.Topic.Gsm, 1, $"SMS konnte nicht an >{e.Reciever}< versendet werden: {e.Message}");
                Email.Send(null, email, $"Sendefehler >{e.Reciever}<");
            }
            else if (gsmSendStatus > 31)
                confirmation = Tab_Sent.Confirmation.RetrySending;
            else if (gsmSendStatus >= 0)
                confirmation = Tab_Sent.Confirmation.SentSuccessful;

            //Sendebestätigung in Datenbank schreiben
            if (!MelBoxSql.Tab_Sent.UpdateSendStatus(e.InternalReference, confirmation))
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
#if DEBUG
            Console.WriteLine($"Debug: Sender >{e.Sender}< hat die Id {fromId}");
#endif

            int messageId = MelBoxSql.Tab_Message.SelectOrCreateMessageId(e.Message);

            Recieved recieved1 = new Recieved(fromId, messageId)
            {
                RecTime = DateTime.UtcNow
            };
            MelBoxSql.Tab_Recieved.Insert(recieved1);
            #endregion

            #region Weiterleiten per EMail oder SMS
            MailAddressCollection emailRecievers = new MailAddressCollection();

            #region Meldelinientest 'SmsAbruf'
            if (e.Message.ToLower().Trim() == SmsWayValidationTrigger.ToLower())
            {
                MelBoxGsm.Gsm.Ask_SmsSend(e.Sender, e.Message.Trim() + " um " + DateTime.Now.ToString("HH:mm:ss") + " Uhr.");

                Sent sent = new Sent(fromId, messageId, Tab_Contact.Communication.Sms)
                {
                    SentTime = DateTime.UtcNow
                };
                MelBoxSql.Tab_Sent.Insert(sent);
            }            
            else
            #endregion
            {
                //Nachricht zum jetzigen Zeitpunkt gesperrt?
                bool blocked = Tab_Message.IsMessageBlockedNow(messageId);

                if (blocked) 
                    e.Message += Environment.NewLine + "Keine Weiterleitung an Bereitschaftshandy da SMS gesperrt.";
                else
                {
                    //Bereitschaft ermitteln
                    List<MelBoxSql.Shift> currentShifts = MelBoxSql.Tab_Shift.SelectOrCreateCurrentShift();                    
                    Console.WriteLine("Aktuelle Bereitschaft: ");

                    //an Bereitschaft weiterleiten
                    foreach (var shift in currentShifts)
                    {
                        Contact to = MelBoxSql.Tab_Contact.SelectContact(shift.ContactId);
                        Console.WriteLine($"Id [{shift.Id}] >{to.Name}<");

                        //Email freigegeben und gültig?
                        if ((to.Via & Tab_Contact.Communication.Email) > 0 && Tab_Contact.IsEmail(to.Email))
                        {
                            emailRecievers.Add(new MailAddress(to.Email, to.Name));

                            Sent sent = new Sent(shift.ContactId, messageId, Tab_Contact.Communication.Email)
                            {
                                Confirmation = Tab_Sent.Confirmation.Unknown
                            };

                            MelBoxSql.Tab_Sent.Insert(sent);
                        }

                        //SMS?
                        if ((to.Via & Tab_Contact.Communication.Sms) > 0)
                        {
                            Sent sent = new Sent(shift.ContactId, messageId, Tab_Contact.Communication.Sms)
                            {
                                Confirmation = Tab_Sent.Confirmation.NaN
                            };

                            MelBoxSql.Tab_Sent.Insert(sent);
                            MelBoxGsm.Gsm.Ask_SmsSend("+" + to.Phone.ToString(), e.Message);
                        }
                    }

                    if (currentShifts.Count == 0)
                    {
                        Console.WriteLine("z.Zt. keine aktive Bereitschaft");
                        e.Message += Environment.NewLine + "Keine Weiterleitung an Bereitschaftshandy in der Geschäftszeit.";
                    }
                }
            }

            //Emails an Bereitschaft und ständige Empfänger senden.
            Email.Send(emailRecievers, e.Message);

            #endregion
        }

        private static void Gsm_SmsSentEvent(object sender, ParseSms e)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Versendet " + e.Sender + ":\r\n" + e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;

            int toId = MelBoxSql.Tab_Contact.SelectContactId(e.Sender);

            string msg = e.Message.ToLower().StartsWith(SmsWayValidationTrigger.ToLower()) ? SmsWayValidationTrigger : e.Message; //Bei "SmsAbruf" ist SendeText und EMpfangstect verschieden.
            int contentId = MelBoxSql.Tab_Message.SelectOrCreateMessageId(msg);

            //Kontakt unbekannt? Neu erstellen!
            if (toId == 0)
            {
                Tab_Contact.InsertNewContact(e.Sender, e.Message);
                toId = MelBoxSql.Tab_Contact.SelectContactId(e.Sender);

                string log = e.Message.Length > 32 ? e.Message.Substring(0, 32) + "..." : e.Message;
                log = $"Neuen Benutzer [{toId}] angelegt mit Absender >{e.Sender}< Nachricht: >{log}<";

                Tab_Log.Insert(Tab_Log.Topic.Database, 2, log);
                Email.Send(null, log, "Unbekannter Absender: Benutzer angelegt.", false);
            }

            //'SMS gesendet' in Datenbank schreiben
            MelBoxSql.Sent sent = new MelBoxSql.Sent(toId, contentId, MelBoxSql.Tab_Contact.Communication.Sms)
            {
                Reference = e.InternalReference,
                Confirmation = Tab_Sent.Confirmation.AwaitingRefernece,
                SentTime = e.TimeUtc
            };

            MelBoxSql.Tab_Sent.Insert(sent);
        }

    }

}
