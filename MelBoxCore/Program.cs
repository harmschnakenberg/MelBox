using System;
using System.Globalization;
using MelBoxGsm;
using MelBoxSql;

namespace MelBoxCore
{
    /*  Bugs:
     *  1)  Empfang SMS mit Sonderzeichen (°C, ö,Ä, ß, &, *) funktioniert jetzt.
     * 
     * 
     */
    
    partial class Program
    {
        
        static void Main()
        {
            #region Programm hochfahren
            Console.BufferHeight = 200; //Max. Zeilen in Console begrenzen
            Console.WriteLine("Progammstart.");
            Ini.ReadIni();
            
            //Modem initialisieren            
            MelBoxGsm.Gsm.GsmStatusReceived += Gsm_GsmStatusReceived;
            Gsm.SmsRecievedEvent += Gsm_SmsRecievedEvent;
            Gsm.StatusReportRecievedEvent += Gsm_StatusReportRecievedEvent;
            Gsm.SmsSentEvent += Gsm_SmsSentEvent;
            Gsm.SmsSentFaildEvent += Gsm_SmsSentFaildEvent;
   
            Gsm.ModemSetup();
            MelBoxWeb.GsmStatus.RelayNumber = Gsm.RelayCallsToPhone;
            Gsm.Ask_RelayIncomingCalls(Gsm.RelayCallsToPhone); //Am Ende Rufumleitung, da Modem für ca. 2 Sek. beschäftigt.

            Tab_Log.Insert(Tab_Log.Topic.Startup, 3, "Programmstart");
            //*/
#if DEBUG
            Gsm.Debug = (int)Gsm.DebugCategory.GsmAnswer;
            Console.WriteLine("Debug: Es wird keine Info-Email beim Programmstart versendet.");
#else
            Email.Send(new System.Net.Mail.MailAddressCollection() { Email.Admin }, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " MelBox2 Programmstart", "Information von " + Environment.MachineName );
#endif

            Console.WriteLine("Prüfe Datenbank: " + (Sql.CheckDb() ? "ok" : "Fehler") );            
            //Console.WriteLine("*** STARTE WEBSERVER ***");
            MelBoxWeb.Server.Start();
            SetHourTimer();
            Gsm.SerialPortDisposed += Gsm_SerialPortDisposed;
            #endregion

            bool run = true;
            bool restart = false;

            //foreach (var url in MelBoxWeb.Server.Urls)
            //{
            //    Console.WriteLine(url);
            //}

            Console.WriteLine("Beenden mit >Exit> - Für Hilfe: >Help<");

            while (run)
            {
                string request = Console.ReadLine();
                string[] words = request.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length < 1) continue;

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
                        if (words[1].ToLower() == "test")
                            Email.Send(new System.Net.Mail.MailAddressCollection() { Email.Admin }, "Test-Email von MelBox2");
                        break;
                    case "sim":
                        if (words[1].ToLower() == "sms")
                        {
                            Console.WriteLine("Simuliere SMS-Empfang...");
                            ParseSms sms = new ParseSms
                            {
                                Message = "Dies ist eine MelBox2-Testnachricht. Bitte ignorieren.",
                                Sender = Environment.MachineName
                            };
                            
                            Gsm_SmsRecievedEvent(null, sms);
                        }
                        break;
                    case "debug":
                        if (words.Length > 1)
                        {
                            if (int.TryParse(words[1], out int debug))
                                Gsm.Debug = debug;
                            Console.WriteLine("Debug = " + Gsm.Debug); 
                        }
                        break;
                    case "restart":
                        restart = true;
                        run = false;
                        break;
                    case "umleitung":
                        Gsm.Ask_DeactivateCallForewarding();
                        break;
                    case "help":
                        ShowHelp();
                        break;
                    case "decode":
                        if (words.Length > 1)
                        {
                            string ucs2 = words[1].Trim();
                            Console.WriteLine(Gsm.DecodeUcs2(ucs2));
                        }
                        break;
                    default:
                        if (request.Length > 1)
                            Gsm.Write(request);
                        break;
                }
            }

            Gsm.Ask_DeactivateCallForewarding();            
            MelBoxWeb.Server.Stop();
            Gsm.DisConnect();

            if (restart) Main();
        }

        private static void Gsm_SerialPortDisposed(object sender, EventArgs e)
        {
            Console.WriteLine("GSM-Modem getrennt.");
        }

        private static void Gsm_SmsSentFaildEvent(object sender, ParseSms e)
        {
            string Text =   $"Für die SMS  >{e.InternalReference}<\r\n" +
                            $"An >{e.Sender}<\r\n" +
                            $"Text >{e.Message}<\r\n" +
                            $"ist seit >{e.TimeUtc.ToLocalTime()}< keine Empfangsbestätigung eingegangen. \r\n" +
                            $"Senden abgebrochen. Kein erneuter Sendeversuch an Empfänger.";

            MelBoxSql.Tab_Log.Insert(Tab_Log.Topic.Gsm, 1, Text.Replace("\r\n", " "));
            Email.Send(null, Text, "Senden fehlgeschlagen: " + e.Message);
        }

        private static void ShowHelp()
        {
            Console.WriteLine("** HILFE **");
            Console.WriteLine("Exit\t\tProgramm beenden.");
            Console.WriteLine("Lese\t\tliest alle im GSM-Modem gespeicherten Nachrichten.");
            Console.WriteLine("Schreibe;*Tel.*;*Nachricht*\tSchreibt eine SMS an *Tel.*");

            Console.WriteLine("Email Test\tSendet eine Test-Email an den Admin.");
            Console.WriteLine("Sim Sms\tSimuliert den Empfang einer Sms.");
            Console.WriteLine("Debug\tSetzt ein Debug-Word\t1: GsmAntwort - 2: GsmStatus");
            Console.WriteLine("Web Start\tBedienoberfläche im Browser starten.");
            Console.WriteLine("Web Stop\tBedienoberfläche im Browser beenden.");
            Console.WriteLine("Timer Start\tMinütliche Abfrage nach SMS/Mobilfunkverbidnung starten.");
            Console.WriteLine("Timer Stop\tMinütliche Abfrage nach SMS/Mobilfunkverbidnung beenden.");
            Console.WriteLine("Decode\tUCS2-Encoded-Text umwandeln.");
            Console.WriteLine("*AT-Befehl*\tFührt einen AT-Befehl aus.");
        }
    }
}
