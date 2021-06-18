using System;
using System.IO.Ports;

namespace MelBoxGsm
{
    public partial class Gsm
    {
        #region Debug
        public static int Debug { get; set; } = 7;

        [Flags]
        public enum DebugCategory
        {
            None,
            GsmAnswer,
            GsmStatus,
            GsmRequest
        }
        #endregion

        private static ReliableSerialPort Port = null;

        public static event EventHandler SerialPortDisposed;

        public static string SerialPortName { get; set; } = SerialPort.GetPortNames()[SerialPort.GetPortNames().Length - 1];
        public static int SerialPortBaudRate { get; set; } = 38400;

        public static int SimPin { get; set; } = 0000;

        private static bool Connect()
        {
            if (Port == null || !Port.IsOpen)
            {
                if (System.IO.Ports.SerialPort.GetPortNames().Length == 0) return false;
                Port = new ReliableSerialPort(SerialPortName, SerialPortBaudRate, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                Port.DataReceived += ParseResponse;
                Port.Disposed += Port_Disposed;
                Port.Open();
            }

            return Port.IsOpen;
        }

        private static void Port_Disposed(object sender, EventArgs e)
        {            
            SerialPortDisposed?.Invoke(null, e);
        }

        public static void DisConnect()
        {
            if (Port != null)
            {
                Port.Close();
                Port.Dispose();
            }
        }

        public static void Write(string request)
        {
            if (Port == null || !Port.IsOpen)
                Connect();

            Port.WriteLine(request);
        }

        static void ParseResponse(object sender, DataReceivedArgs e)
        {
            string input = e.Data;

            if ((Debug & (int)DebugCategory.GsmAnswer) > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;                
                Console.WriteLine(input);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            if (input.Contains(Answer_SmsRead))
            {
                ParseTextMessage(input);
            }
            else
            {
                //Entferne "OK" am Ende, Teile auf in Zeilen
                string[] lines = e.Data.Replace(ReliableSerialPort.Terminator, string.Empty).Replace("\r\n", "\n").Split('\n');

                // Einzeilige Amtworten                              
                foreach (var line in lines)
                {
                    // +CMGS: 12
                    if (line.StartsWith(Answer_SmsSent))
                    {
                        ParseMessageReference(input);
                    }

                    // +CSQ: 20,99
                    else if (line.StartsWith(Answer_Signal))
                    {
                        ParseSignalQuality(line);
                    }

                    //  +CDSI:  ||  +CMTI: 
                    else if (line.StartsWith(Answer_NewStatusReport) || line.StartsWith(Answer_NewSms))
                    {
                        Ask_SmsRead();
                    }

                    //  +CLIP: <number>, <type>, , [, <alpha>][, <CLI validity>]
                    else if (line.Contains(Answer_IncomingCallInfo))
                    {
                        ParseIncomingCallInfo(line);
                    }

                    //  +CNUM: "Eigne Rufnummer","+49123456789",145
                    else if (line.StartsWith(Answer_MyPhoneNumber))
                    {
                        ParseOwnNumber(line);
                    }

                    //  +CSCA: "+491710760000",145
                    else if (line.StartsWith(Answer_ServiceCenterNumber))
                    {
                        ParseServiceCenterNumber(line);
                    }

                    //  +CREG: 0,1 | +CREG: 1
                    else if (line.Contains(Answer_NetworkRegistration))
                    {
                        ParseNetworkRegistration(line);
                    }

                    //  +COPS: 0,0,"T-Mobile D"
                    else if (line.Contains(Answer_ProviderName))
                    {
                        ParseProviderName(line);
                    }

                    //  ^SCKS: <mode>,<SimStatus>'
                    else if (line.Contains(Answer_SimSlot))
                    {
                        ParseISimTrayStatus(line);                        
                    }

                    // +CPIN: READY
                    else if (line.Contains(Answer_Pin))
                    {
                        ParseSimPin(line);                        
                    }

                    // +CCFC: 0,1,"+4916095285304",145
                    else if (line.Contains(Answer_CallRelay))
                    {
                        ParseCallRelay(line);
                    }

                    else if (line.StartsWith(Answer_Error))
                    {
                        ParseGsmError(line);
                    }

                }
            }

        }

    }

}