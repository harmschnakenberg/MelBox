using System;
using System.Globalization;

namespace MelBoxGsm
{
    public partial class Gsm
    {
        #region Enum
        enum HeaderSms
        {
            Index = 0,                      //<index>
            MessageStatus = 1,              //<stat>
            Sender = 2,                     //<oa>/<da>         | <fo>
            PhoneBookEntry = 3,             //[<alpha>]         | <mr>
            ProviderDate = 4,               //[<scts>] Datum    | [<ra>]
            ProviderTime = 5,               //[<scts>] Zeit     | [<tora>]
            TypeOfSenderAddress = 6,        //[<tooa>/<toda>]   | [<scts>] Datum
            TextLength = 7                  //[<length>]        | [<scts>] Zeit
        }

        enum HeaderStatusReport
        {
            Index = 0,                      //<index>
            MessageStatus = 1,              //<stat>
            FirstOctet = 2,                 //<oa>/<da>         | <fo>
            MessageReference = 3,           //[<alpha>]         | <mr>
            RecipientAddress = 4,           //[<scts>] Datum    | [<ra>]
            TypeOfRecipientAddress = 5,     //[<scts>] Zeit     | [<tora>]
            ProviderDate = 6,               //[<tooa>/<toda>]   | [<scts>] Datum
            ProviderTime = 7,               //[<length>]        | [<scts>] Zeit
            DischargeDate = 8,              //[<tooa>/<toda>]   | [<dt>] Datum
            DischargeTime = 9,              //[<length>]        | [<dt>] Zeit
            SendStatus = 10                 //<st>
        }
        #endregion

        #region Events

        public delegate void SmsRecievedEventHandler(object sender, ParseSms e);

        public static event SmsRecievedEventHandler SmsRecievedEvent;

        public delegate void StatusReportRecievedEventHandler(object sender, StatusReport e);

        public static event StatusReportRecievedEventHandler StatusReportRecievedEvent;

        #endregion

        public static void Ask_SmsDelete(int index)
        {
            Port.WriteLine("AT+CMGD=" + index);
        }

        #region SMS Lesen
        public static void Ask_SmsRead(string status = "REC UNREAD")
        {
            Gsm.Write(string.Format("AT+CMGL=\"{0}\"", status));
        }

        // Example input:
        // +CMGL: 0,"REC UNREAD","002B003100310031003200320032003300330033003400340034",,"19/07/07,20:40:54+08",145,15
        // 00480065006C006C006F00200057006F0072006C006400210020D83CDF0D

        //Nachricht:
        //+CMGL: <index> ,  <stat> ,  <oa> / <da> , [ <alpha> ], [ <scts> ][,  <tooa> / <toda> ,  <length> ]
        //<data>
        //[... ]
        //OK

        //+CMGL: 9,"REC READ","+4917681371522",,"20/11/08,13:47:10+04"
        //Ein Test 08.11.2020 13:46 PS sms38.de
        //

        //Statusreport:
        //+CMGL: < index > ,  < stat > ,  < fo > ,  < mr > , [ < ra > ], [ < tora > ],  < scts > ,  < dt > ,  < st >
        //[... ]
        //OK
        //z.B.: +CMGL: 1,"REC READ",6,34,,,"20/11/06,16:08:45+04","20/11/06,16:08:50+04",0

        //     [0],       [1],             [2],[3],      [4],         [5],[6],      [7],         [8],      [9],        [10],[11]
        //+CMGL: 9,"REC READ","+4917681371522",   ,"20/11/08,13:47:10+04"
        //+CMGL: 1,"REC READ",6               , 34,                      ,   ,"20/11/06,16:08:45+04","20/11/06,16:08:50+04",0
        private static void ParseTextMessage(string input)
        {
            string[] msgs = input.Split(new string[] { Answer_SmsRead }, StringSplitOptions.RemoveEmptyEntries);

#if DEBUG
            Console.WriteLine($"Roh-SMS: {msgs.Length} Roh-Smsen gelesen.");
#endif
            try
            {
                foreach (string msg in msgs)
                {
                    if (msg.StartsWith("AT+CMGL")) continue; // erste Zeile

                    string[] lines = msg
                        .Replace("\r\n", "\n")
                        .Split('\n');
#if DEBUG
                    Console.WriteLine($"Roh-SMS: {lines.Length} Zeilen.");
#endif
                    string[] header = lines[0]
                        .Split(',');
#if DEBUG
                    Console.WriteLine($"Roh-SMS-Kopf: {header.Length} Einträge.");
#endif
                    //<index> Index
                    if (!int.TryParse(header[(int)HeaderSms.Index], out int index))
                        continue; //Beginnt nicht mit einer Speicherplatz-Nummer: kein gültiges Format

                    //[<alpha>] PhoneBookentry für <oa>/<da> | <mr> MessageReference
                    if (int.TryParse(header[(int)HeaderStatusReport.MessageReference], out int reference))
                    {
                        //Statusreport
                        ParseNewStatusReport(header);
                    }
                    else
                    {
#if DEBUG
                        Console.WriteLine("Lese SMS aus:\r\n" + input);
#endif
                        ParseNewSms(header, lines);
                    }

                    Ask_SmsDelete(index);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"### FEHLER ParseTextMessage()\r\n{ex.GetType()}\r\n{ex.Message}");
            }

        }

        private static void ParseNewStatusReport(string[] header)
        {
            //Dies ist festgestellt als ein Statusreport

            //<index> Index
            int.TryParse(header[(int)HeaderSms.Index], out int index);

            //<stat> Status
            string status = header[(int)HeaderSms.MessageStatus].Trim('"');

            //<oa>/<da> OriginatingAddress/ DestinationAddress | <fo> First Oxctet
            string sender = header[(int)HeaderSms.Sender].Trim('"');
            if (sender.StartsWith("002B")) sender = DecodeUcs2(sender); // hex 002B = '+'; Encoding in Ucs2

            //[<alpha>] PhoneBookentry für <oa>/<da> | <mr> MessageReference
            int.TryParse(header[(int)HeaderStatusReport.MessageReference], out int reference);

            //<scts> ServiceProvider TimeStamp
            DateTime TimeUtc = ParseUtcTime(header[(int)HeaderStatusReport.ProviderDate].Trim('"'), header[(int)HeaderStatusReport.ProviderTime].Trim('"'));

            //<dt> DischargeTime
            DateTime DischargeTimeUtc = ParseUtcTime(header[(int)HeaderStatusReport.DischargeDate].Trim('"'), header[(int)HeaderStatusReport.DischargeTime].Trim('"'));

            //<st> Status
            int.TryParse(header[(int)HeaderStatusReport.SendStatus], out int SendStatus);

            StatusReport Report = new StatusReport
            {
                RawHeader = string.Join(",", header),
                Index = index,
                MessageStatus = status,
                ServiceCenterTimeStampTimeUtc = TimeUtc,
                DischargeTimeUtc = DischargeTimeUtc,
                InternalReference = reference,
                SendStatus = SendStatus
            };

            #region Sendungsnachverfolgung abschließen
            foreach (ParseSms sms in WaitingForStatusReport) //siehe auch ParseMessageReference() 
            {
                if (sms.InternalReference == reference)
                {
                    Console.WriteLine($"Erwartete Empfangsbestätigung eingetroffen für Nachricht [{sms.InternalReference}] {sms.Message}");
                    WaitingForStatusReport.Remove(sms);
                }
            }
            #endregion 

            StatusReportRecievedEvent?.Invoke(null, Report);
        }

        private static void ParseNewSms(string[] header, string[] line)
        {
            //Dies ist vorher festgestellt als eine SMS-Nachricht
            try
            {
#if DEBUG
                foreach (var item in header)
                {
                    Console.WriteLine($">{item}<");
                }
                Console.WriteLine("Header.length = " + header.Length);
#endif

                if (header.Length < 5) return; //Ungültiger Header!

                //<index> Index
                int.TryParse(header[(int)HeaderSms.Index], out int index);

                //<stat> Status
                string status = header[(int)HeaderSms.MessageStatus].Trim('"');

                //<oa>/<da> OriginatingAddress/ DestinationAddress | <fo> First Oxctet
                string sender = header[(int)HeaderSms.Sender].Trim('"');

                //[<alpha>] PhoneBookentry für <oa>/<da>
                string SenderPhoneBookEntry = header[3].Trim('"');

                //[<scts>] Service Centre Time Stamp | [<ra>] Recipient Address
                DateTime TimeUtc = ParseUtcTime(header[4].Trim('"'), header[5].Trim('"'));

                string messageText = string.Empty;

                for (int i = 1; i < line.Length; i++)
                {
                    if (line[i] == "OK") break; //und OK-Ausgabe am Ende nicht speichern
                    if (line[i].Length == 0) continue; // keine Leerzeilen 
                    messageText += line[i] + " ";
                }

                messageText = messageText.Trim();

                if (!messageText.StartsWith("00") && messageText.Length > 20) //Kein Leerzeichen, startet mit '00' und lang: Vermutung Sms-Inhalt ist in UCS2 Formatiert wegen Sonderzeichen z.B. °C, ä, ß...            
                    messageText = DecodeUcs2(messageText);
                //else
                //{
                //    messageText = DecodeGsm(messageText); // schmeißt Exception
                //}

                ParseSms sms = new ParseSms
                {
                    RawHeader = line[0],
                    Index = index,
                    MessageStatus = status,
                    Sender = sender,
                    SenderPhonebookEntry = SenderPhoneBookEntry,
                    TimeUtc = TimeUtc,
                    Message = messageText // Sinnvoll?
                };


                // if (header.Length > 5) // Letzte werden beim MC75 nicht ausgegeben
                // {
                //     int.TryParse(header[6], out int numberTypeInt);

                //     int.TryParse(header[7], out int textLength);

                //    
                //     sms.MessageLength = textLength;
                //     sms.PhoneNumberType = numberTypeInt;
                //}

                SmsRecievedEvent?.Invoke(null, sms);
            }
            catch (Exception ex)
            {
                throw new Exception($"FEHLER ParseNewSms(): {ex.GetType()}\r\n{ex.Message}");
            }
        }

        private static DateTime ParseUtcTime(string gsmDateString, string gsmTimeString)
        {
            //"19/07/07,20:40:54+08"
            //+08 = Zeitzone in Viertelstunden
            string[] t = ("20" + gsmDateString + " " + gsmTimeString)
                .Trim('"')
                .Replace('/', '-')
                .Replace(',', ' ')
                .Split('+');

            DateTime.TryParse(t[0], out DateTime time);

            if (int.TryParse(t[1], out int timeZoneQuarters))
            {
                time = time.AddHours(timeZoneQuarters / -4);
            }

            return time;
        }

        public static string DecodeUcs2(string ucs2)
        {
            //UCS2 ist Fallback-Encode, wenn Standard GSM-Encode nicht ausreicht.
            ucs2 = ucs2.Trim();
            System.Collections.Generic.List<byte> bytes = new System.Collections.Generic.List<byte>();

            for (int i = 0; i < ucs2.Length; i += 2)
            {
                string str = ucs2.Substring(i, 2);
                bytes.Add(byte.Parse(str, NumberStyles.HexNumber));
#if DEBUG
                Console.Write(str + " ");
#endif
            }
            //#if DEBUG
            //            Console.WriteLine();

            //            string result = "Unicode: " + System.Text.Encoding.Unicode.GetString(bytes.ToArray());
            //            result += Environment.NewLine + "UTF8:      " + System.Text.Encoding.UTF8.GetString(bytes.ToArray());
            //            result += Environment.NewLine + "Default:   " + System.Text.Encoding.Default.GetString(bytes.ToArray());
            //            result += Environment.NewLine + "UTF7:      " + System.Text.Encoding.UTF7.GetString(bytes.ToArray());
            //            result += Environment.NewLine + "ASCII:     " + System.Text.Encoding.ASCII.GetString(bytes.ToArray());
            //            result += Environment.NewLine + "BigEndian: " + System.Text.Encoding.BigEndianUnicode.GetString(bytes.ToArray());
            //#endif
            return System.Text.Encoding.BigEndianUnicode.GetString(bytes.ToArray());
        }

        /// <summary>
        /// Umlaute aus GSM-Encoding herauslesen. 
        /// </summary>
        /// <param name="gsm">string, GSM-Encodes</param>
        /// <returns>bereinigter Text </returns>
        public static string DecodeGsm(string gsm)
        {
            //Quelle: http://www.unicode.org/Public/MAPPINGS/ETSI/GSM0338.TXT

            //System.Text.StringBuilder sb = new System.Text.StringBuilder();

            //try
            //{
            //    gsm = gsm.Trim();

            //    for (int i = 0; i < gsm.Length; i += 2)
            //    {
            //        string str = gsm.Substring(i, 2);
            //        byte b = byte.Parse(str, NumberStyles.HexNumber);

            //        switch (b)
            //        {
            //            case 0x5B:
            //                sb.Append('Ä');
            //                break;
            //            case 0x5C:
            //                sb.Append('Ö');
            //                break;
            //            case 0x5E:
            //                sb.Append('Ü');
            //                break;
            //            case 0x7C:
            //                sb.Append('ö');
            //                break;
            //            case 0x7B:
            //                sb.Append('ä');
            //                break;
            //            case 0xFC:
            //                sb.Append('ü');
            //                break;
            //            default:
            //                sb.Append(str);
            //                break;
            //        }
            //    }
            //}
            //catch
            //{

            //}

            //return sb.ToString(); 

            //HaSch: M|hre, ^berg{nge,  [hnlich ung~nstig \lig
            return gsm.Replace('[', 'Ä').Replace('\\', 'Ö').Replace('^', 'Ü').Replace('{', 'ä').Replace('|', 'ö').Replace('~', 'ü');

        }

        #endregion
    }

    public class ParseSms : EventArgs
    {
        public string RawHeader { get; set; }
        public int Index { get; set; }
        public string MessageStatus { get; set; }
        public string Sender { get; set; }
        public string SenderPhonebookEntry { get; set; }
        public DateTime TimeUtc { get; set; }
        public string Message { get; set; }
        public int PhoneNumberType { get; set; }
        public int MessageLength { get; set; }

        public int InternalReference { get; set; }
    }

    public class StatusReport : EventArgs
    {
        public string RawHeader { get; set; }
        public int Index { get; set; }
        public string MessageStatus { get; set; }
        public DateTime ServiceCenterTimeStampTimeUtc { get; set; }
        public DateTime DischargeTimeUtc { get; set; }
        public int InternalReference { get; set; } //Identifizierung der gesendeten SMS
        public int SendStatus { get; set; }

        //Nachträglich eingefügte Infos für Nachvollziehbarkeit
        public string Reciever { get; set; }
        public string Message { get; set; }
    }

}