using MelBoxGsm;
using MelBoxSql;
using System;

namespace MelBoxCore
{
    /*  Bugs & ToDo siehe Info.txt */

    partial class Program
    {

        static void Main()
        {
            try
            {
                #region Programm hochfahren
                //Console.BufferHeight = 1000; //Max. Zeilen in Console begrenzen
                Console.WriteLine("Progammstart. Beenden mit >Exit> - Für Hilfe: >Help<");
                Ini.ReadIni();
                Console.WriteLine($"Tägliche Abfrage um {HourOfDailyTasks} Uhr.");

                //Modem initialisieren
                Gsm.SmsSentEvent += Gsm_SmsSentEvent;
                Gsm.SmsRecievedEvent += Gsm_SmsRecievedEvent;
                Gsm.GsmStatusReceived += Gsm_GsmStatusReceived;
                Gsm.SmsSentFaildEvent += Gsm_SmsSentFaildEvent;
                Gsm.SerialPortDisposed += Gsm_SerialPortDisposed;
                Gsm.StatusReportRecievedEvent += Gsm_StatusReportRecievedEvent;                
                Gsm.ModemSetup();

                Tab_Log.Insert(Tab_Log.Topic.Startup, 3, "Programmstart");
                
#if DEBUG
                Gsm.Debug = 7;
                Console.WriteLine("Debug: Es wird keine Info-Email beim Programmstart versendet.");
#else
                Email.Send(new System.Net.Mail.MailAddressCollection() { Email.Admin }, DateTime.Now.ToString("G") + " MelBox2 Programmstart", "Information von " + Environment.MachineName);
#endif
                Console.WriteLine("Prüfe Datenbank: " + (Sql.CheckDb() ? "ok" : "Fehler"));

                MelBoxWeb.Server.Start();
                SetHourTimer();

                #endregion

                bool run = true;

                while (run)
                {
                    string request = Console.ReadLine() ?? string.Empty;
                    run = ParseConsoleInput(request);
                }

            }
            finally
            {
                Console.WriteLine("Das Programm wird beendet.");
                Gsm.Ask_DeactivateCallForewarding();
                MelBoxWeb.Server.Stop();
                Gsm.DisConnect();
            }
        }

        private static bool ParseConsoleInput(string request)
        {
            bool run = true;

            string[] words = request.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 1) return run;

            switch (words[0].ToLower())
            {
                case "exit":
                    run = false;
                    break;                
                case "email":
                    if (words[1].ToLower() == "test")
                        Email.Send(Email.Admin, "Test-Email von MelBox2");
                    break;
                case "sms":
                    if (words[1].ToLower() == "sim")
                    {
                        Console.WriteLine("Simuliere SMS-Empfang...");
                        ParseSms sms = new ParseSms
                        {
                            Message = "Dies ist eine MelBox2-Testnachricht. Bitte ignorieren.",
                            TimeUtc = DateTime.Now,
                            Sender = Environment.MachineName
                        };

                        Gsm_SmsRecievedEvent(null, sms);
                    }
                    else
                    {
                        string msg = string.Empty;
                        
                        for (int i = 2; i < words.Length; i++)
                        {
                            msg += words[i];
                        }

                        Gsm.Ask_SmsSend(words[1], msg);
                    }
                    break;
                case "debug":
                    if (words.Length > 1)
                    {
                        if (int.TryParse(words[1], out int debug))
                        {
                            Gsm.Debug = debug;
                            Console.WriteLine("Debug = " + Gsm.Debug);
                        }
                    }
                    break;
                case "decode":
                    if (words.Length > 1)
                    {
                        string ucs2 = words[1].Trim();
                        Console.WriteLine(Gsm.DecodeUcs2(ucs2));
                    }
                    break;
                case "help":
                    ShowHelp();
                    break;
                default:
                    if (request.Length > 1)
                        Gsm.Write(request);
                    break;
            }

            return run;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("** HILFE **");
            Console.WriteLine("Exit".PadRight(32,' ') + "Programm beenden.");          
            Console.WriteLine("Sms *Tel.* *Nachricht*".PadRight(32,' ') + "Schreibt eine SMS an *Tel.*");
            Console.WriteLine("Sms Sim".PadRight(32, ' ') + "Simuliert den Empfang einer Sms.");
            Console.WriteLine("Email Test".PadRight(32, ' ') + "Sendet eine Test-Email an den Admin.");
            
            Console.WriteLine("Debug".PadRight(32, ' ') + "Setzt ein Debug-Word\t" +
                $"{Gsm.DebugCategory.GsmRequest}: {nameof(Gsm.DebugCategory.GsmRequest)} - " +
                $"{Gsm.DebugCategory.GsmAnswer}: {nameof(Gsm.DebugCategory.GsmAnswer)} - " +
                $"{Gsm.DebugCategory.GsmStatus}: {nameof(Gsm.DebugCategory.GsmStatus)}");

            Console.WriteLine("Decode".PadRight(32, ' ') + "UCS2-Encoded-Text (Lange Bytereihenfolge) entziffern.");
            Console.WriteLine("*AT-Befehl*".PadRight(32, ' ') + "Führt einen AT-Befehl aus.");
        }



    }
}
