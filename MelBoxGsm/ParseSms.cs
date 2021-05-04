﻿using System;
using System.Collections.Generic;
using System.Threading;

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

        #region Fields
        static readonly AutoResetEvent autoEvent = new AutoResetEvent(false);
        static int LastSmsReference = 0;
        public static Dictionary<int, Tuple<string, string>> SentSms = new Dictionary<int, Tuple<string, string>>();
        #endregion

        public static void Ask_SmsDelete(int index)
        {
            Port.WriteLine("AT+CMGD=" + index);
        }

        public static void Ask_SmsSend(string phone, string message)
        {
            Gsm.Write("AT+CMGS=\"" + phone + "\"\r");
            Gsm.Write(message + ctrlz);

            //Warte auf ParseMessageReference()
            autoEvent.WaitOne(5000);

            //Liste sauber halten
            //if (SentSms.Keys..Contains(LastSmsReference))
            if (SentSms.Remove(LastSmsReference))
            {
                Console.WriteLine("SMS mit Refferenz {0} aus Merkerliste entfernt.", LastSmsReference);
            }

            //Zur Liste hinzufügen
            SentSms.Add(LastSmsReference, new Tuple<string, string>(phone, message));

            Console.WriteLine("Erwarte Empfangsbestätigung für " + SentSms.Count + " SMS");
            foreach (int item in SentSms.Keys)
            {
                Console.WriteLine("{0}\t{1}\t{2}", LastSmsReference, SentSms[item].Item1, SentSms[item].Item2);
            }
            //Console.WriteLine("SMS mit Refferenz {0} in Merkerliste geschrieben.", LastSmsReference);

            //Bereinigen für ParseMessageReference()
            LastSmsReference = 0;
            autoEvent.Reset();
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
            string[] list = input.Split(new string[] { Answer_SmsRead }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string msg in list)
            {
                string[] line = msg
                    .Replace("\r\n", "\n")
                    .Split('\n');

                string[] header = line[0]
                    .Split(',');

                //<index> Index
                int.TryParse(header[(int)HeaderSms.Index], out int index);

                //<stat> Status
                string status = header[(int)HeaderSms.MessageStatus].Trim('"');

                //<oa>/<da> OriginatingAddress/ DestinationAddress | <fo> First Oxctet
                string sender = header[(int)HeaderSms.Sender].Trim('"');

                //[<alpha>] PhoneBookentry für <oa>/<da> | <mr> MessageReference
                if (int.TryParse(header[(int)HeaderStatusReport.MessageReference], out int reference))
                {
                    //Dies ist ein Statusreport

                    //<scts> ServiceProvider TimeStamp
                    DateTime TimeUtc = ParseUtcTime(header[(int)HeaderStatusReport.ProviderDate], header[(int)HeaderStatusReport.ProviderTime]);

                    //<dt> DischargeTime
                    DateTime DischargeTimeUtc = ParseUtcTime(header[(int)HeaderStatusReport.DischargeDate], header[(int)HeaderStatusReport.DischargeTime]);

                    //<st> Status
                    int.TryParse(header[(int)HeaderStatusReport.SendStatus], out int SendStatus);

                    StatusReport Report = new StatusReport
                    {
                        RawHeader = line[0],
                        Index = index,
                        MessageStatus = status,
                        ServiceCenterTimeStampTimeUtc = TimeUtc,
                        DischargeTimeUtc = DischargeTimeUtc,
                        InternalReference = reference,
                        SendStatus = SendStatus
                    };

                    if (SentSms.ContainsKey(reference))
                    {
                        Report.Reciever = SentSms[reference].Item1;
                        Report.Message = SentSms[reference].Item2;
                        SentSms.Remove(reference);
                    }

                    StatusReportRecievedEvent?.Invoke(null, Report);
                }
                else
                {
                    //Dies ist eine SMS

                    //[<alpha>] PhoneBookentry für <oa>/<da>
                    string SenderPhoneBookEntry = header[3].Trim('"');

                    //[<scts>] Service Centre Time Stamp | [<ra>] Recipient Address
                    DateTime TimeUtc = ParseUtcTime(header[4], header[5]);

                    int.TryParse(header[6], out int numberTypeInt);

                    int.TryParse(header[7], out int textLength);

                    string messageText = string.Empty;

                    for (int i = 1; i < line.Length; i++)
                    {
                        messageText += line[i] + " ";
                    }

                    ParseSms sms = new ParseSms
                    {
                        RawHeader = line[0],
                        Index = index,
                        MessageStatus = status,
                        Sender = sender,
                        SenderPhonebookEntry = SenderPhoneBookEntry,
                        TimeUtc = TimeUtc,
                        Message = messageText.Substring(0, textLength), // Sinnvoll?
                        MessageLength = textLength,
                        PhoneNumberType = numberTypeInt
                    };

                    SmsRecievedEvent?.Invoke(null, sms);
                }

                Ask_SmsDelete(index);
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