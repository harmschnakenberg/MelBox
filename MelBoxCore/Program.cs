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
            Gsm.ModemSetup("COM7");
            
            //TEST: nicht erfolgreich
            //Gsm.Ask_RelayIncomingCalls("+4942122317123");
            //*/

            Console.WriteLine("Prüfe Datenbank: " + (Sql.CheckDb() ? "ok" : "Fehler") );            
            Console.WriteLine("*** STARTE WEBSERVER ***");
            MelBoxWeb.Server.Start();

            string request = "AT";
            Console.WriteLine("Lese, Schreibe, AT-Befehl:");

            while (true)
            {
                request = Console.ReadLine();

                if (request.ToLower() == "exit") break;

                else if (request.StartsWith("Schreibe"))
                {
                    string[] r = request.Split(';');

                    Gsm.Ask_SmsSend(r[1], r[2]);
                }
                else if (request.StartsWith("Lese"))
                {
                    Gsm.Ask_SmsRead("ALL");
                }
                else if (request.StartsWith("Signal"))
                {
                    Gsm.Ask_SignalQuality();
                    Gsm.Ask_NetworkRegistration();
                }
                else
                {
                    if (request.Length > 1)
                        Gsm.Write(request);
                }
            }

            Console.WriteLine("Beliebige Taste zum beenden...");
            Console.ReadKey();

            Gsm.DisConnect();
            MelBoxWeb.Server.Stop();
        }

    }
}
