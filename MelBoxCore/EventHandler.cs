using MelBoxGsm;
using MelBoxSql;
using System;
using System.Collections.Generic;
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
            if ((Gsm.Debug & (int)Gsm.DebugCategory.GsmStatus) > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(e.Property + ":\t" + e.Value);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

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
                        MelBoxSql.Tab_Log.Insert(Tab_Log.Topic.Gsm, 1, "Mobilfunknetz: " + e.Value);
                    break;
                case Gsm.Modem.ProviderName:
                    MelBoxWeb.GsmStatus.ProviderName = e.Value.ToString();
                    break;
                case Gsm.Modem.IncomingCall:
                    string call = "Eingehender Sprachanruf von " + e.Value;
                    MelBoxSql.Tab_Log.Insert(MelBoxSql.Tab_Log.Topic.Gsm, 2, call);
                    Console.WriteLine(DateTime.Now + "\t" + call);
                    break;
                case Gsm.Modem.RelayCallEnabled:
                    if ((bool)e.Value)
                    {
                        MelBoxWeb.GsmStatus.RelayNumber = Gsm.RelayCallsToPhone;
                        Tab_Log.Insert(Tab_Log.Topic.Gsm, 3, "Sprachanrufe werden umgeleitet an +" + Gsm.RelayCallsToPhone);                        
                    }
                    else
                    {
                        Tab_Log.Insert(Tab_Log.Topic.Gsm, 1, "Keine Umleitung von Sprachanrufen an +" + Gsm.RelayCallsToPhone);
                        Gsm.Ask_RelayIncomingCalls(Gsm.RelayCallsToPhone);
                    }
                    break;
                case Gsm.Modem.PinStatus:
                    MelBoxWeb.GsmStatus.PinStatus = e.Value.ToString();
                    break;
                case Gsm.Modem.ModemError:
                    MelBoxWeb.GsmStatus.LastError = DateTime.Now.ToLongTimeString() + " - " + e.Value.ToString();
                    Tab_Log.Insert(Tab_Log.Topic.Gsm, 2, "Fehler an Modem: " + e.Value);
                    break;
                case Gsm.Modem.SimSlot:
                    //BAUSTELLE
                    break;
                default:
                    break;
            }
        }

        private static void Gsm_StatusReportRecievedEvent(object sender, StatusReport e)
        {

            Console.ForegroundColor = ConsoleColor.Green;
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

                string email = $"SMS Absender >{e.Reciever}<\r\n" +
                    $"SMS Text >{e.Message}<\r\n" +
                    $"SMS Sendezeit >{e.DischargeTimeUtc}<\r\n" +
                    $"Interne Refernez >{e.InternalReference}<\r\n\r\n" +
                    $"Senden durch Mobilfunknetzbetreiber abgebrochen! Empfänger empfangsbereit?";

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

        private static void Gsm_SmsSentEvent(object sender, ParseSms e)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Versendet " + e.Sender + ":\r\n" + e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;
                       
            string msg = e.Message.ToLower().StartsWith(SmsWayValidationTrigger.ToLower()) ? SmsWayValidationTrigger : e.Message; //Bei "SmsAbruf" ist SendeText und Empfangstect verschieden.
            int contentId = MelBoxSql.Tab_Message.SelectOrCreateMessageId(msg);
            int toId = GetSmsSenderID(e.Sender, e.Message);

            //'SMS gesendet' in Datenbank schreiben
            MelBoxSql.Sent sent = new MelBoxSql.Sent(toId, contentId, MelBoxSql.Tab_Contact.Communication.Sms)
            {
                Reference = e.InternalReference,
                Confirmation = Tab_Sent.Confirmation.AwaitingRefernece,
                SentTime = e.TimeUtc
            };

            if (sent.SentTime == DateTime.MinValue)
                sent.SentTime = DateTime.UtcNow;

            MelBoxSql.Tab_Sent.Insert(sent);
        }

        private static void Gsm_SmsRecievedEvent(object sender, ParseSms e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(e.Sender + ":\r\n" + e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;

            #region SMS-Empfang in Datenbank protokollieren
            int fromId = GetSmsSenderID(e.Sender, e.Message);
            int messageId = MelBoxSql.Tab_Message.SelectOrCreateMessageId(e.Message);

            Recieved recieved1 = new Recieved()
            {
                FromId = fromId,
                ContentId = messageId,
                RecTime = DateTime.UtcNow
            };

            MelBoxSql.Tab_Recieved.Insert(recieved1);
            #endregion

            #region Weiterleiten per EMail oder SMS

            MailAddressCollection emailRecievers = new MailAddressCollection();
            string emailSuffix = string.Empty;
                        
            if (e.Message.ToLower().Trim() == SmsWayValidationTrigger.ToLower()) // SmsAbruf?
            {
                #region Meldelinientest 'SmsAbruf'

                MelBoxGsm.Gsm.Ask_SmsSend(e.Sender, e.Message.Trim() + " um " + DateTime.Now.ToString("HH:mm:ss") + " Uhr.");

                Sent sent = new Sent(fromId, messageId, Tab_Contact.Communication.Sms)
                {
                    SentTime = DateTime.UtcNow
                };
                MelBoxSql.Tab_Sent.Insert(sent);

                #endregion
            }
            else if (Tab_Message.IsMessageBlockedNow(messageId)) // Nachricht zum jetzigen Zeitpunkt gesperrt?            
            {
                emailSuffix += Environment.NewLine + "Keine Weiterleitung an Bereitschaftshandy da SMS zur Zeit gesperrt.";
            }
            else // An Bereitschaft senden          
            {
                #region An Bereitschaft senden
                //Bereitschaft ermitteln
                List<MelBoxSql.Shift> currentShifts = MelBoxSql.Tab_Shift.SelectOrCreateCurrentShift();
                Console.WriteLine("Aktuelle Bereitschaft: ");

                //an Bereitschaft weiterleiten
                foreach (var shift in currentShifts)
                {
                    Contact to = MelBoxSql.Tab_Contact.SelectContact(shift.ContactId);
                    Console.WriteLine($"Id [{shift.Id}] >{to.Name}< von >{shift.Start}< bis >{shift.End}<");

                    //Email freigegeben und gültig?
                    if ((to.Via & Tab_Contact.Communication.Email) > 0 && Tab_Contact.IsEmail(to.Email))
                    {
                        emailRecievers.Add(new MailAddress(to.Email, to.Name));

                        Sent sent = new Sent(shift.ContactId, messageId, Tab_Contact.Communication.Email)
                        {
                            Confirmation = Tab_Sent.Confirmation.Unknown,
                            SentTime = DateTime.UtcNow
                        };

                        MelBoxSql.Tab_Sent.Insert(sent);
                    }

                    //SMS?
                    if ((to.Via & Tab_Contact.Communication.Sms) > 0)
                    {
                        Sent sent = new Sent(shift.ContactId, messageId, Tab_Contact.Communication.Sms)
                        {
                            Confirmation = Tab_Sent.Confirmation.NaN,
                            SentTime = DateTime.UtcNow
                        };

                        MelBoxSql.Tab_Sent.Insert(sent);
                        MelBoxGsm.Gsm.Ask_SmsSend("+" + to.Phone.ToString(), e.Message);
                    }
                }

                if (currentShifts.Count == 0)
                {
                    Console.WriteLine("z.Zt. keine aktive Bereitschaft");
                    emailSuffix += Environment.NewLine + "Keine Weiterleitung an Bereitschaftshandy während der Geschäftszeit.";
                }
                else
                {
                    emailSuffix += Environment.NewLine + "Weiterleitung an Bereitschaftshandy außerhalb Geschäftszeiten ist erfolgt.";
                }
                #endregion
            }
            
            //Emails an Bereitschaft und ständige Empfänger senden.
            string subject = $"SMS-Eingang >{MelBoxSql.Tab_Contact.SelectName_Company_City(fromId)}<, Text >{e.Message}<"; 
            string body = $"Absender >{e.Sender}<\r\nText >{e.Message}<\r\nSendezeit >{e.TimeUtc.ToLocalTime().ToLongTimeString()}<\r\n" + emailSuffix;
            Email.Send(emailRecievers, body, subject);

            #endregion
        }

        #region Hilfs-Methoden

        private static int GetSmsSenderID(string phone, string message)
        {
            int fromId = MelBoxSql.Tab_Contact.SelectContactId(phone);

            if (fromId == 0) // Unbekannter Sender
            {
                Tab_Contact.InsertNewContact(phone, message); 
                fromId = MelBoxSql.Tab_Contact.SelectContactId(phone);

                string log = message.Length > 32 ? message.Substring(0, 32) + "..." : message;
                log = $"Neuen Benutzer [{fromId}] angelegt mit Absender >{phone}< Nachricht: >{log}<";

                Tab_Log.Insert(Tab_Log.Topic.Database, 2, log);
                Email.Send(null, log, "Unbekannter Absender: Benutzer angelegt.", false);
            }

            #if DEBUG
            Console.WriteLine($"Debug: Kontakt >{phone}< hat die Id {fromId}");
            #endif

            return fromId;
        }
        #endregion

    }
}
