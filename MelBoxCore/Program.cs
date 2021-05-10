using System;
using MelBoxGsm;
using MelBoxSql;

namespace MelBoxCore
{
    partial class Program
    {        
        static void Main()
        {
            Console.WriteLine("Progammstart.");

            // Auskommentieren für ohne GSM-Modem
            MelBoxGsm.Gsm.GsmStatusReceived += Gsm_GsmStatusReceived;
            Gsm.SmsRecievedEvent += Gsm_SmsRecievedEvent;
            Gsm.StatusReportRecievedEvent += Gsm_StatusReportRecievedEvent;
            Gsm.SmsSentEvent += Gsm_SmsSentEvent;
            Gsm.ModemSetup("COM7");
            
            //TEST: nicht erfolgreich
            //Gsm.Ask_RelayIncomingCalls("+4942122317123");
            //*/

            Console.WriteLine("Prüfe Datenbank: " + (Sql.CheckDb() ? "ok" : "Fehler") );            
            Console.WriteLine("*** STARTE WEBSERVER ***");
            MelBoxWeb.Server.Start();

            bool run = true;
            //string request = "AT";
            Console.WriteLine("Lese, Schreibe, AT-Befehl:");

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

            Console.WriteLine("Beliebige Taste zum beenden...");
            Console.ReadKey();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("** HILFE **");
            Console.WriteLine("Exit\tProgramm beenden.");
            Console.WriteLine("Lese\tliest alle im GSM-Modem gespeicherten Nachrichten.");
            Console.WriteLine("Schreibe;*Tel.*;*Nachricht*\tSchriebt eine SMS an *Tel.*");
            Console.WriteLine("Web Start\tBedienoberfläche im Browser starten.");
            Console.WriteLine("Web Stop\tBedienoberfläche im Browser beenden.");
            Console.WriteLine("*AT-Befehl*\tFührt einen AT-Befehl aus.");
        }
    }
}
