using System;
using System.Timers;

namespace MelBoxGsm
{
    public partial class Gsm
    {
        public enum Modem
        {
            SignalQuality,
            BitErrorRate,
            OwnPhoneNumber,
            OwnName,
            ServiceCenterNumber,
            NetworkRegistration,
            ProviderName,
            IncomingCall,
            RelayCallEnabled,
            PinStatus,
            SimSlot,

        }

        public static ulong AdminPhone { get; set; } = 4916095285304;
        public static ulong RelayCallsToPhone { get; set; } = 4916095285304;

        #region Event Status-Update
        public delegate void GsmStatusReceivedEventHandler(object sender, GsmStatusArgs e);
        public static event EventHandler<GsmStatusArgs> GsmStatusReceived;

        static void OnGsmStatusReceived(Modem property, object value)
        {
            GsmStatusReceived?.Invoke(null, new GsmStatusArgs { Property = property, Value = value });
        }
        #endregion

        #region Timer ; Regelmäßige Anfragen
        static readonly Timer askingTimer = new Timer(60000);
        #endregion


        #region Abfragen
        public static void ModemSetup(string serialPort = "", int baudRate = 0)
        {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();

            if (Array.Exists(ports, p => p == serialPort))
            {
                Gsm.SerialPortName = serialPort;
            }

            if (baudRate > 0)
            {
                Gsm.SerialPortBaudRate = baudRate;
            }

            if (Connect())
            {
                //Modem Type; Gewinnt Zeit, falls Modem noch nicht bereit.
                Write("AT+CGMM");

                //extended error reports
                Write("AT+CMEE=1");

                //Textmode
                Write("AT+CMGF=1");
                Write("AT+CMGF?");
                //Write("AT+CSCS=\"UCS2\""); //böse! kein UCS2-Decoding implementiert!

                //Signalqualität
                Ask_SignalQuality();

                //Eigene Telefonnumer-Nummer der SIM-Karte auslesen 
                Write("AT+CNUM");

                //Im Netzwerk Registriert?
                Ask_NetworkRegistration();

                //Modem meldet Registrierungsänderungänderung mit +CREG: <stat>
                Write("AT+CREG=1");

                //ProviderName?
                Write("AT+COPS?");

                //SMS-Service-Center Adresse
                Write("AT+CSCA?");

                //Alle Speicher nutzen
                Write("AT+CPMS=\"MT\",\"MT\",\"MT\"");

                //Sendeempfangsbestätigungen abonieren
                //Quelle: https://www.codeproject.com/questions/271002/delivery-reports-in-at-commands
                //Quelle: https://www.smssolutions.net/tutorials/gsm/sendsmsat/
                //AT+CSMP=<fo> [,  <vp> / <scts> [,  <pid> [,  <dcs> ]]]
                // <fo> First Octet:
                // <vp> Validity-Period: 0 - 143 (vp+1 x 5min), 144 - 167 (12 Hours + ((VP-143) x 30 minutes)), [...]
                Write("AT+CSMP=49,1,0,0");

                Write("AT+CNMI=2,1,2,2,1");
                //erfolgreich getestet: AT+CNMI=2,1,2,2,1

                //Statusänderung SIM-Schubfach melden
                Write("AT^SCKS=1");

                //SIM-Karte gesperrt?
                Write("AT+CPIN?");

                #region Regelmäßige Anfragen an das Modem
                askingTimer.Elapsed += new ElapsedEventHandler(Ask_RegularQueue);
                askingTimer.AutoReset = true;
                askingTimer.Start();
                #endregion

                //Sprachanrufe:
                //Display unsolicited result codes
                Write("AT+CLIP=1");

                //Abfrage Rufweiterleitung aktiv?
                Write("AT+CCFC=0,2");
            }
        }

        public static void Ask_DeactivateCallForewarding()
        {
            Write("AT+CCFC=0,0");
        }

        /// <summary>
        /// Regelmäßig (alle Minute) gestellte Anfragen an das Modem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Ask_RegularQueue(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " Uhr");
            Ask_NetworkRegistration();
            Ask_SignalQuality();
            Ask_SmsRead();

            CheckPendingStatusReports();
        }

        public static void Ask_SignalQuality()
        {
            Write("AT+CSQ");
        }

        public static void Ask_NetworkRegistration()
        {
            Write("AT+CREG?");
        }

        public static void Ask_RelayIncomingCalls(ulong phone)
        {

            //AT+CCFC=<reason> ,  <mode> [,  <number> [,  <type> [,  <class> [,  <time> ]]]]
            //<type> 145: international '+'
            // Write("AT+CCFC=0,3,\"" + phone + "\", 145");

            //Rufumleitung BAUSTELLE: nicht ausreichend getestet //            
            // Write("ATD*61*" + phone + "*10#;");

            if (phone > 0)
            {
                //Write($"**61*+{phone}*11*05#");
                Write("AT+CCFC=0,3,\"" + phone + "\", 145");
                Console.WriteLine("Sprachanrufe werden umgeleitet an +" + phone);

                //Antwort ^SCCFC : <reason>, <status> (0: inaktiv, 1: aktiv), <class> [,.
                //System.Threading.Thread.Sleep(4000); //Antwort abwarten - Antwort wird nicht ausgewertet.                
            }
        }
        #endregion

        #region Auslesen
        // +CSQ: 20,99
        private static void ParseSignalQuality(string input)
        {
            string[] items = input
                .Replace(Answer_Signal, string.Empty)
                .Split(',');

            if (int.TryParse(items[0], out int rawSignal))
            {
                int signalQuality = (rawSignal > 30) ? -1 : rawSignal * 100 / 30;

                OnGsmStatusReceived(Modem.SignalQuality, signalQuality);
            }

            if (int.TryParse(items[1], out int bitErrorRate))
            {
                double ModemErrorRate;
                switch (bitErrorRate)
                {
                    case 0:
                        ModemErrorRate = 0.2;
                        break;
                    case 1:
                        ModemErrorRate = 0.4;
                        break;
                    case 2:
                        ModemErrorRate = 0.8;
                        break;
                    case 3:
                        ModemErrorRate = 1.6;
                        break;
                    case 4:
                        ModemErrorRate = 3.2;
                        break;
                    case 5:
                        ModemErrorRate = 6.4;
                        break;
                    case 6:
                        ModemErrorRate = 12.8;
                        break;
                    case 7:                        
                    default:
                        ModemErrorRate = 99;
                        break;
                }

                OnGsmStatusReceived(Modem.BitErrorRate, ModemErrorRate);
            }
        }

        //  +CNUM: "Eigne Rufnummer","+49123456789",145
        private static void ParseOwnNumber(string input)
        {
            string[] items = input
                .Replace(Answer_MyPhoneNumber, string.Empty)
                .Split(',');

            string name = items[0].Trim('"');
            string number = items[1].Trim('"');

            OnGsmStatusReceived(Modem.OwnName, name);
            OnGsmStatusReceived(Modem.OwnPhoneNumber, number);
        }

        //  +CREG: 0,1 | +CREG: 1
        private static void ParseNetworkRegistration(string input)
        {
            string[] items = input
                .Replace(Answer_NetworkRegistration, string.Empty)
                .Split(',');

            //bei Abfrage items[1], bei Änderungsbenachrichtigung items[0]            
            int.TryParse(items[items.Length - 1], out int stat);

            string regString = "unbekannt";
            switch (stat)
            {
                case 0:
                    regString = "nicht registriert";
                    break;
                case 1:
                    regString = "registriert";

                    if (items.Length < 2) //unsolicated Status
                    {
                        Write("AT+CNUM"); //Eigene Telefonnumer-Nummer der SIM-Karte auslesen                 
                        Write("AT+COPS?");//ProviderName?
                        Write("AT+CSCA?");//SMS-Service-Center Adresse
                    }
                    break;
                case 2:
                    regString = "suche Netz";
                    break;
                case 3:
                    regString = "verweigert";
                    break;
                case 4:
                    regString = "unbekannt";
                    break;
                case 5:
                    regString = "Roaming";
                    break;
            }

            OnGsmStatusReceived(Modem.NetworkRegistration, regString);
        }

        //  +CSCA: "+491710760000",145
        private static void ParseServiceCenterNumber(string input)
        {
            string[] items = input
                .Replace(Answer_ServiceCenterNumber, string.Empty)
                .Split(',');

            string number = items[0].Trim('"');
            OnGsmStatusReceived(Modem.ServiceCenterNumber, number);
        }

        //  +COPS: 0,0,"T-Mobile D"
        private static void ParseProviderName(string input)
        {
            string[] items = input
                .Replace(Answer_ProviderName, string.Empty)
                .Split(',');

            if (items.Length < 3) return;
            string name = items[2].Trim('"');
            OnGsmStatusReceived(Modem.ProviderName, name);
        }

        //  +CLIP: <number>, <type>, , [, <alpha>][, <CLI validity>]
        //  +CLIP: "+4942122317123",145,,,,0
        private static void ParseIncomingCallInfo(string input)
        {
            string[] items = input
                .Replace(Answer_IncomingCallInfo, string.Empty)
                .Split(',');

            string phone = items[0].Trim('"');
            string phoneBookEntry = items[4].Trim('"');

            if (phoneBookEntry.Length > 3 && phoneBookEntry != phone)
                phone += " " + phoneBookEntry;

            if (phone.Length < 3) phone = "unbekannt";

            //<CLI validity> 0:ok, 1:Nummer unterdrückt, 2:N/V

            OnGsmStatusReceived(Modem.IncomingCall, phone);
        }

        //  ^SCKS: <mode>,<SimStatus>'
        private static void ParseISimTrayStatus(string input)
        {
            string[] sim = input.Replace(Answer_SimSlot, string.Empty).Split(',');

            if (sim[sim.Length - 1].Trim() == "1")            
                OnGsmStatusReceived(Modem.SimSlot, "SIM-Schubfach: SIM-Karte erkannt");            
            else            
                OnGsmStatusReceived(Modem.SimSlot, "SIM-Schubfach: SIM-Karte nicht erkannt");            
        }

        // +CPIN: READY                         //BAUSTELLE: nicht ausreichend getestet
        private static void ParseSimPin(string input)
        {
            string pinStatus = input.Replace(Answer_Pin, string.Empty).Trim();

            switch (pinStatus)
            {
                case "READY": //PIN has already been entered. No further entry needed
                    Console.WriteLine("SIM-Karte: PIN ok");                    
                    break;
                case "SIM PIN": // ME (Mobile Equipment) is waiting for SIM PIN1
                    Console.WriteLine("Setze hinterlegte PIN für SIM-Karte.");
                    Write("AT+CPIN=" + SimPin);
                    break;
                default:
                    Console.WriteLine($"SIM-Karte gesperrt: >{pinStatus}<");
                    break;
            }

            OnGsmStatusReceived(Modem.PinStatus, pinStatus.ToLower() ) ;
        }

        // +CCFC: 0,1,"+4916095285304",145
        private static void ParseCallRelay(string input)
        {
            string[] items = input.Replace(Answer_CallRelay, string.Empty).Split(',');

            if (items.Length > 2 && "1" == items[1]) //1=Weiterleitung Sprachanrufe, 2=Daten, 4=Fax
            {
                bool status = "1" == items[0];
                //Console.WriteLine($"Weiterleitung Sprachanrufe an {items[2].Trim('"')} {(status ? "aktiviert" : "deaktiviert")}");

                OnGsmStatusReceived(Modem.RelayCallEnabled, status);
            }
        }
        #endregion
    }

    public class GsmStatusArgs : EventArgs
    {
        public Gsm.Modem Property { get; set; }
        public object Value { get; set; }
    }

}
