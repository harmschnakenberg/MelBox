using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MelBoxGsm;

namespace MelBoxCore
{
    static class Ini
    {
        private static readonly string folder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private static readonly string path = Path.Combine(folder, "Ini", "Config.ini");


        private static void CheckIni()
        {
            if (File.Exists(path)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using StreamWriter w = File.AppendText(path);
            try
            {
                w.WriteLine("[ öäü " + w.Encoding.EncodingName + ", Build " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + "]\r\n" +
                            "\r\n[Intern]\r\n" +

                            ";;; Debug-Word für GSM-Modem-Kommunikation:\r\n" +
                            $";{nameof(Gsm.Debug)}={Gsm.Debug}\r\n" +

                            ";;; Sms-Text zur Prüfung des Sendewegs (case-insensitive):\r\n" + 
                            $";{nameof(Program.SmsWayValidationTrigger)}={Program.SmsWayValidationTrigger}\r\n" +

                            "\r\n;;; Tagesstunde zu der tägliche Aufgaben angestoßen werden (Routine-Meldung, DB-Backup,..):\r\n" +
                            $";{nameof(Program.HourOfDailyTasks)}={Program.HourOfDailyTasks}\r\n" +

                            "\r\n;;; ID des Kontakts aus der Tabelle 'Contact', der als Standardempfänger eingesetzt werden soll:\r\n" +
                            $";{nameof(MelBoxSql.Tab_Shift.DefaultShiftContactId)}={MelBoxSql.Tab_Shift.DefaultShiftContactId}\r\n" +
                            
                            "\r\n[Gsm-Modem]\r\n" +
                            ";;; Telefonnummer für Debug-Meldungen (z.B. Routine-Meldung):\r\n" +
                            $";{nameof(Gsm.AdminPhone)}={Gsm.AdminPhone}\r\n" +

                            "\r\n;;; Telefonnummer an die Sprachanrufe weitergelietet werden:\r\n" +
                             $";{nameof(Gsm.RelayCallsToPhone)}={Gsm.RelayCallsToPhone}\r\n" +

                            "\r\n;;; COM-Port an den das Modem angeschlossen ist:\r\n" +
                            $";{nameof(Gsm.SerialPortName)}={Gsm.SerialPortName}\r\n" +

                             "\r\n;;; Baudrate der Modem-Kommunikation:\r\n" +
                            $";{nameof(Gsm.SerialPortBaudRate)}={Gsm.SerialPortBaudRate}\r\n" +

                             "\r\n;;; Pin der eingelegten SIM-Karte (bei Bedarf):\r\n" +
                            $";{nameof(Gsm.SimPin)}={Gsm.SimPin}\r\n" +

                            "\r\n[Email]\r\n" +
                             ";;; E-Mail-Server:\r\n" +
                            $";{nameof(Email.SmtpHost)}={Email.SmtpHost}\r\n" +
                            $";{nameof(Email.SmtpPort)}={Email.SmtpPort}\r\n" +
                            $";{nameof(Email.SmtpEnableSSL)}={Email.SmtpEnableSSL}\r\n" +
                            

                             "\r\n;;; Angezeigte Absenderadresse von Emails aus dem Programm:\r\n" +
                            $";{nameof(Email.From)}={Email.From}\r\n" +

                            "\r\n;;; Empfänger für Debug-Meldungen:\r\n" +
                            $";{nameof(Email.Admin)}={Email.Admin}\r\n" +

                             "\r\n[DB]\r\n" +
                             ";;; min. Level für Administratorrechte:\r\n" +
                            $";{nameof(MelBoxWeb.Server.Level_Admin)}={MelBoxWeb.Server.Level_Admin}\r\n" +

                            "\r\n;;; min. Level für Benutzerrechte:\r\n" +
                            $";{nameof(MelBoxWeb.Server.Level_Reciever)}={MelBoxWeb.Server.Level_Reciever}\r\n" +

                            "\r\n;;; max. dargestellte Einträge in Weboberfläche:\r\n" +
                            $";{nameof(MelBoxSql.Sql.MaxSelectedRows)}={MelBoxSql.Sql.MaxSelectedRows}\r\n"

                            );
            }
            catch (IOException io_ex)
            {
                throw io_ex;
            }
        }


        internal static void ReadIni()
        {
            try
            {
                CheckIni();
                string[] lines = System.IO.File.ReadAllLines(path, Encoding.UTF8);

                foreach (string line in lines)
                {
                    if (line.Length < 3 || line.StartsWith(';') || line.StartsWith('[')) continue;

                    string[] item = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    string key = item[0];
                    string val = item[1].Trim();

                    switch (key)
                    {
                        case nameof(Program.SmsWayValidationTrigger):
                            Program.SmsWayValidationTrigger = val;
                            break;
                        case nameof(Program.HourOfDailyTasks):
                            if (int.TryParse(val, out int i))
                             Program.HourOfDailyTasks = i;
                            break;
                        case nameof(MelBoxSql.Tab_Shift.DefaultShiftContactId):
                            if (int.TryParse(val, out i))
                                MelBoxSql.Tab_Shift.DefaultShiftContactId = i;
                            break;
                        case nameof(MelBoxSql.Sql.MaxSelectedRows):
                            if (int.TryParse(val, out i))
                                MelBoxSql.Sql.MaxSelectedRows = i;
                            break;
                        case nameof(Gsm.Debug):
                            if (int.TryParse(val, out i))
                                Gsm.Debug = i;
                            break;
                        case nameof(Gsm.AdminPhone):
                            if (ulong.TryParse(val.Trim('+'), out ulong phone))
                                Gsm.AdminPhone = phone;
                            break;
                        case nameof(Gsm.RelayCallsToPhone):
                            if (ulong.TryParse(val.Trim('+'), out phone))
                                Gsm.RelayCallsToPhone = phone;
                            break;                            
                        case nameof(Gsm.SerialPortName):
                            Gsm.SerialPortName = val;
                            break;
                        case nameof(Gsm.SerialPortBaudRate):
                            if (int.TryParse(val, out i))
                                Gsm.SerialPortBaudRate = i;
                            break;
                        case nameof(Email.SmtpHost):
                            Email.SmtpHost = val;
                            break;
                        case nameof(Email.SmtpPort):
                            if (int.TryParse(val, out i))
                                Email.SmtpPort = i;
                            break;
                        case nameof(Email.From):
                            Email.From = GetMailAddress(val);
                            break;
                        case nameof(Email.Admin):
                            Email.Admin = GetMailAddress(val);
                            break;
                        case nameof(MelBoxWeb.Server.Level_Admin):
                            if (int.TryParse(val, out i))
                                MelBoxWeb.Server.Level_Admin = i;
                            break;
                        case nameof(MelBoxWeb.Server.Level_Reciever):
                            if (int.TryParse(val, out i))
                                MelBoxWeb.Server.Level_Reciever = i;
                            break;
                        case nameof(Email.SmtpEnableSSL):
                            if (bool.TryParse(val, out bool b))
                                Email.SmtpEnableSSL = b;
                            break;                            
                    }

                }

            }
            catch (IOException io_ex)
            {
                throw io_ex;
            }
        }


        private static System.Net.Mail.MailAddress GetMailAddress(string txt)
        {
            int index2 = txt.IndexOf('<');
            string name = txt[1..txt.LastIndexOf('"')];
            string email = txt.Substring(index2 + 1, txt.Length - index2 - 2);

            Console.WriteLine($"Config: >{name}< >{email}<");
            return new System.Net.Mail.MailAddress(email, name);
        }
    }
}
