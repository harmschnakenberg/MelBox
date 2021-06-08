using System;
using System.Globalization;
using MelBoxGsm;
using MelBoxSql;

namespace MelBoxCore
{
    /*  Bugs:
     *  1)  Empfang SMS mit Sonderzeichen (°C, ö,Ä, ß, &, *) funktioniert jetzt.
     *  2)  Frage: WebServer auslagern in eigene EXE ?
     * 
     */
    
    partial class Program
    {
        
        static void Main()
        {
            #region Programm hochfahren
            Console.BufferHeight = 300; //Max. Zeilen in Console begrenzen
            Console.WriteLine("Progammstart.");
            Ini.ReadIni();
            
            //Modem initialisieren            
            MelBoxGsm.Gsm.GsmStatusReceived += Gsm_GsmStatusReceived;
            Gsm.SmsRecievedEvent += Gsm_SmsRecievedEvent;
            Gsm.StatusReportRecievedEvent += Gsm_StatusReportRecievedEvent;
            Gsm.SmsSentEvent += Gsm_SmsSentEvent;
            Gsm.SmsSentFaildEvent += Gsm_SmsSentFaildEvent;
   
            Gsm.ModemSetup();
            
            Gsm.Ask_RelayIncomingCalls(Gsm.RelayCallsToPhone); //Am Ende Rufumleitung, da Modem für ca. 2 Sek. beschäftigt.

            Tab_Log.Insert(Tab_Log.Topic.Startup, 3, "Programmstart");
            //*/
#if DEBUG
            Gsm.Debug = 7;//(int)Gsm.DebugCategory.GsmRequest | (int)Gsm.DebugCategory.GsmAnswer | (int)Gsm.DebugCategory.GsmStatus ;
            Console.WriteLine("Debug: Es wird keine Info-Email beim Programmstart versendet.");
#else
            Email.Send(new System.Net.Mail.MailAddressCollection() { Email.Admin }, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " MelBox2 Programmstart", "Information von " + Environment.MachineName );
#endif

            Console.WriteLine("Prüfe Datenbank: " + (Sql.CheckDb() ? "ok" : "Fehler") );            

            MelBoxWeb.Server.Start();
            SetHourTimer();
            Gsm.SerialPortDisposed += Gsm_SerialPortDisposed;
            #endregion

            bool run = true;

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
            }

            Gsm.Ask_DeactivateCallForewarding();            
            MelBoxWeb.Server.Stop();
            Gsm.DisConnect();
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
            Console.WriteLine("Debug\tSetzt ein Debug-Word\t" +
                $"{Gsm.DebugCategory.GsmRequest}: {nameof(Gsm.DebugCategory.GsmRequest)} - " +
                $"{Gsm.DebugCategory.GsmAnswer}: {nameof(Gsm.DebugCategory.GsmAnswer)} - " +
                $"{Gsm.DebugCategory.GsmStatus}: {nameof(Gsm.DebugCategory.GsmStatus)}");

            Console.WriteLine("Decode\tUCS2-Encoded-Text umwandeln.");
            Console.WriteLine("*AT-Befehl*\tFührt einen AT-Befehl aus.");
        }
    }
}
