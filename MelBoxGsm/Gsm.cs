using System;
using System.IO.Ports;

namespace MelBoxGsm
{
    public partial class Gsm
    {
        private static ReliableSerialPort Port = null;

        public static string SerialPortName { get; set; } = SerialPort.GetPortNames()[SerialPort.GetPortNames().Length - 1];

        private static bool Connect()
        {
            if (Port == null || !Port.IsOpen)
            {
                Port = new ReliableSerialPort(SerialPortName, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                Port.DataReceived += ParseResponse;
                Port.Open();
            }

            return Port.IsOpen;
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

        #region Konstanten
        const string ctrlz = "\u001a";

        const string Answer_Signal = "+CSQ: ";
        const string Answer_SmsRead = "+CMGL: ";
        const string Answer_SmsSent = "+CMGS: ";
        const string Answer_NewStatusReport = "+CDSI: ";
        const string Answer_NewSms = "+CMTI: ";
        const string Answer_MyPhoneNumber = "+CNUM: ";
        const string Answer_ServiceCenterNumber = "+CSCA: ";
        const string Answer_NetworkRegistration = "+CREG: ";
        const string Answer_ProviderName = "+COPS: ";
        const string Answer_IncomingCallInfo = "+CLIP: ";
        #endregion


        static void ParseResponse(object sender, DataReceivedArgs e)
        {
            string input = e.Data;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(input);
            Console.ForegroundColor = ConsoleColor.Gray;

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
                    else if (line.StartsWith(Answer_IncomingCallInfo))
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
                    else if (line.StartsWith(Answer_NetworkRegistration))
                    {
                        ParseNetworkRegistration(line);
                    }

                    //  +COPS: 0,0,"T-Mobile D"
                    else if (line.Contains(Answer_ProviderName))
                    {
                        ParseProviderName(line);
                    }


                }
            }

        }

    }

}
