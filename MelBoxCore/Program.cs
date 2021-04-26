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

            /*// Auskommentieren für ohne GSM-Modem
            MelBoxGsm.Gsm.GsmStatusReceived += Gsm_GsmStatusReceived;
            Gsm.SmsRecievedEvent += Gsm_SmsRecievedEvent;
            Gsm.StatusReportRecievedEvent += Gsm_StatusReportRecievedEvent;
            
            Gsm.ModemSetup("COM7");
            string request = "AT";
            Console.WriteLine("Lese, Schreibe, AT-Befehl:");

            //TEST: nicht erfolgreich
            //Gsm.Ask_RelayIncomingCalls("+4942122317123");
            //*/

            MelBoxSql.Sql.CheckDb();

            //TEST DB
            //string name = "SMSZentrale";
            //string passwort = "7307";

            //int contactId = Tab_Contact.Authentification(name, passwort);

            //Console.WriteLine(name + "; " + passwort + " Kontakt-ID " + contactId);

            Console.WriteLine("*** STARTE WEBSERVER ***");
           MelBoxWeb.Server.Start();


            //while (true)
            //{
            //    request = Console.ReadLine();

            //    if (request.Length == 0) break;

            //    else if (request.StartsWith("Schreibe"))
            //    {
            //        string[] r = request.Split(';');

            //        Gsm.Ask_SmsSend(r[1], r[2]);
            //    }
            //    else if (request.StartsWith("Lese"))
            //    {
            //        Gsm.Ask_SmsRead("ALL");
            //    }
            //    else if (request.StartsWith("Signal"))
            //    {
            //        Gsm.Ask_SignalQuality();
            //        Gsm.Ask_NetworkRegistration();
            //    }
            //    else
            //    {
            //        Gsm.Write(request);
            //    }
            //}

            Console.WriteLine("Progammende.");
            Console.ReadKey();

            Gsm.DisConnect();
        }
    }
}
