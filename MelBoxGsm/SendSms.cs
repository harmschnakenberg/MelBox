using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Timers;

namespace MelBoxGsm
{
    public partial class Gsm
    {
        #region Events
        public delegate void SmsSentEventHandler(object sender, ParseSms e);
        public static event SmsSentEventHandler SmsSentEvent;
        #endregion

        static System.Timers.Timer sendTimer = null;
        public static Queue<Tuple<string, string>> SendList = new Queue<Tuple<string,string>>();
        private static Tuple<string, string> currentSendSms = null;

        /// <summary>
        /// Zu sendende SMS in der Schlange anstellen, ggf. Abarbeitung der Schlange anstoßen
        /// </summary>
        /// <param name="phone"></param>
        /// <param name="message"></param>
        public static void Ask_SmsSend(string phone, string message)
        {
            SendList.Enqueue(new Tuple<string, string>(phone, message)); //Anstellen

            if (sendTimer == null) //Timer erstellen
            {
                //SendSmsQueue(null, null);
                sendTimer = new System.Timers.Timer();
                sendTimer.Interval = 3000;
                sendTimer.Elapsed += new ElapsedEventHandler(SendSmsQueue);
                sendTimer.AutoReset = true;
                sendTimer.Start();
            }

            sendTimer.Enabled = true; //Anstoßen
        }

        /// <summary>
        /// Abarbeiten der zu sendeneden SMSen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void SendSmsQueue(object sender, ElapsedEventArgs e)
        {
            if (currentSendSms != null) return; //Wenn noch etwas in Bearbeitung ist, Abbruch; wird durch Timer erneut gestartet.

            currentSendSms = SendList.Dequeue(); //Ersten aus der Schlange nehmen + andere SMS senden blockieren 

            if (SendList.Count == 0)
            {
                sendTimer.Enabled = false; //Timer beenden, wenn Liste leer
            }

            Gsm.Write("AT+CMGS=\"" + currentSendSms.Item1 + "\"\r");
            Gsm.Write(currentSendSms.Item2 + ctrlz);

            Console.WriteLine("Versende SMS an {1}\r\n{2}", currentSendSms.Item1, currentSendSms.Item2);
        }

        //z.B.  +CMGS: 123
        private static void ParseMessageReference(string input)
        {
            string[] lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (line.StartsWith(Answer_SmsSent))
                {
                    if (!int.TryParse(line.Replace(Answer_SmsSent, string.Empty).Trim(), out int reference))
                    {
                        Console.WriteLine("Für die gesendete SMS konnte keine Referenz-Nr. ermittelt werden. Empfangsbestätigungen auf Plausibilität prüfen! Empfangen:\r\n>{0}<\r\n\r\nErwartete Empfangsbestätigung für SMS an {1}\r\n{2}",
                            line, currentSendSms.Item1, currentSendSms.Item2);

                        currentSendSms = null;
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Vergebe Referenz {0} für SMS an {1}\r\n{2}", reference, currentSendSms.Item1, currentSendSms.Item2);
                    }

                    ParseSms sentSms = new ParseSms()
                    {
                        InternalReference = reference,
                        Sender = currentSendSms.Item1,
                        TimeUtc = DateTime.UtcNow,
                        Message = currentSendSms.Item2 // Sinnvoll?
                    };

                    currentSendSms = null; //Freigeben für nächste SMS

                    SmsSentEvent?.Invoke(null, sentSms);
                }
            }
        }

        /// <summary>
        /// Timer von außen starten oder stoppen
        /// </summary>
        public static void SetTimer(bool set)
        {
            sendTimer.Enabled = set;
        }

    }
}
