using System;
using System.Timers;

namespace MelBoxGsm
{
    public partial class Gsm
    {
        #region Event Status-Update
        public delegate void GsmStatusReceivedEventHandler(object sender, GsmStatusArgs e);
        public static event EventHandler<GsmStatusArgs> GsmStatusReceived;

        static void OnGsmStatusReceived(Modem property, object value)
        {
            GsmStatusReceived?.Invoke(null, new GsmStatusArgs { Property = property, Value = value });
        }
        #endregion

        #region Timer ; Regelmäßige Anfragen
        static Timer aksingTimer = new Timer(60000);
        #endregion


        #region Abfragen
        public static void ModemSetup(string serialPort = "")
        {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();

            if (Array.Exists(ports, p => p == serialPort))
            {
                Gsm.SerialPortName = serialPort;
            }

            if (Connect())
            {
                //Modem Type; Gewinnt Zeit, falls Modem noch nicht bereit.
                Write("AT+CGMM");

                //Textmode
                Write("AT+CMGF=1");

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


                //Sprachanrufe:
                //Display unsolicited result codes
                Write("AT+CLIP=1");

                //Statusänderung SIM-Schubfach melden
                Write("AT^SCKS=1");

                #region Regelmäßige Anfragen an das Modem
                aksingTimer.Elapsed += new ElapsedEventHandler(Ask_RegularQueue);
                aksingTimer.AutoReset = true;
                aksingTimer.Start();
                #endregion
            }

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
        }

        public static void Ask_SignalQuality()
        {
            Write("AT+CSQ");
        }

        public static void Ask_NetworkRegistration()
        {
            Write("AT+CREG?");
        }

        public static void Ask_RelayIncomingCalls(string phone)
        {

            //AT+CCFC=<reason> ,  <mode> [,  <number> [,  <type> [,  <class> [,  <time> ]]]]
            //<type> 145: international '+'
            // Write("AT+CCFC=0,3,\"" + phone + "\", 145");

            //Rufumleitung BAUSTELLE: nicht ausreichend getestet //            
            Write("ATD*61*" + phone + "*10#;");

            //Antwort ^SCCFC : <reason>, <status> (0: inaktiv, 1: aktiv), <class> [,.
            System.Threading.Thread.Sleep(4000); //Antwort abwarten - Antwort wird nicht ausgewertet.
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
                int signalQuality = (rawSignal > 32) ? -1 : rawSignal * 100 / 32;

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
                    default:
                        ModemErrorRate = 99;
                        break;
                }

                OnGsmStatusReceived(Modem.BitErrorRate, ModemErrorRate);
            }
        }

        //  +CMGS: 123
        private static void ParseMessageReference(string input)
        {
            if (int.TryParse(input.Replace(Answer_SmsSent, string.Empty), out int reference))
            {
                LastSmsReference = reference;
                autoEvent.Set();
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

        #endregion
    }

    public class GsmStatusArgs : EventArgs
    {
        public Gsm.Modem Property { get; set; }
        public object Value { get; set; }
    }

}
