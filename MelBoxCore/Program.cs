using System;
using MelBoxGsm;
using MelBoxSql;

namespace MelBoxCore
{
    partial class Program
    {
        
        static void Main()
        {
            #region Programm hochfahren
            Console.WriteLine("Progammstart.");

            // Auskommentieren für ohne GSM-Modem
            MelBoxGsm.Gsm.GsmStatusReceived += Gsm_GsmStatusReceived;
            Gsm.SmsRecievedEvent += Gsm_SmsRecievedEvent;
            Gsm.StatusReportRecievedEvent += Gsm_StatusReportRecievedEvent;
            Gsm.SmsSentEvent += Gsm_SmsSentEvent;
            Gsm.AdminPhone = 4916095285304;
            
            Gsm.ModemSetup("COM7", 115200);
            Tab_Log.Insert(Tab_Log.Topic.Startup, 3, "Programmstart");

#if DEBUG
            Console.WriteLine("Debug: Es wird keine Info-Email beim Programmstart versendet.");
#else
            Email.Send(new System.Net.Mail.MailAddressCollection() { Email.Admin }, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " Programmstart", "Information von " + Environment.MachineName );
#endif

            Console.WriteLine("Prüfe Datenbank: " + (Sql.CheckDb() ? "ok" : "Fehler") );            
            Console.WriteLine("*** STARTE WEBSERVER ***");
            MelBoxWeb.Server.Start();
            #endregion

            bool run = true;

            Console.WriteLine("Beenden mit >Exit> - Für Hilfe: >Help<");

            while (run)
            {
                string request = Console.ReadLine();
                string[] words = request.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length < 0) continue;

                switch (words[0].ToLower())
                {
                    case "exit":
                        run = false;
                        break;
                    case "schreibe":
                        string[] r = request.Split(';');
                        Gsm.Ask_SmsSend(r[1], r[2]);
                        break;
                    case "lese":
                        Gsm.Ask_SmsRead("ALL");
                        break;
                    case "web":
                        if (words[1].ToLower() == "start")
                            MelBoxWeb.Server.Start();
                        if (words[1].ToLower() == "stop")
                            MelBoxWeb.Server.Stop();
                        break;
                    case "timer":
                        if (words[1].ToLower() == "start")
                            MelBoxGsm.Gsm.SetTimer(true);
                        if (words[1].ToLower() == "stop")
                            MelBoxGsm.Gsm.SetTimer(false);
                        break;
                    case "email":
                        Email.Send(new System.Net.Mail.MailAddressCollection() { Email.Admin }, "Test-Email von MelBox2");
                        break;
                    case "test2":
                        ParseSms sms = new ParseSms();
                        sms.Message = "Dies ist eine MelBox2-Testnachricht. Bitte ignorieren.";
                        sms.Sender = "+4916095285304";
                        Gsm_SmsRecievedEvent(null, sms);
                        break;
                    case "help":
                        ShowHelp();
                        break;
                    default:
                        if (request.Length > 1)
                            Gsm.Write(request);
                        break;
                }
            }

            Gsm.DisConnect();
            MelBoxWeb.Server.Stop();

            //Console.WriteLine("Beliebige Taste zum beenden...");
            //Console.ReadKey();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("** HILFE **");
            Console.WriteLine("Exit\t\tProgramm beenden.");
            Console.WriteLine("Lese\t\tliest alle im GSM-Modem gespeicherten Nachrichten.");
            Console.WriteLine("Schreibe;*Tel.*;*Nachricht*\tSchriebt eine SMS an *Tel.*");
            Console.WriteLine("Web Start\tBedienoberfläche im Browser starten.");
            Console.WriteLine("Web Stop\tBedienoberfläche im Browser beenden.");
            Console.WriteLine("Timer Start\tMinütliche Abfrage nach SMS/Mobilfunkverbidnung starten.");
            Console.WriteLine("Timer Stop\tMinütliche Abfrage nach SMS/Mobilfunkverbidnung beenden.");
            Console.WriteLine("*AT-Befehl*\tFührt einen AT-Befehl aus.");
        }
    }
}
