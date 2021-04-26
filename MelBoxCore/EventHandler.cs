using MelBoxGsm;
using System;

namespace MelBoxCore
{
    partial class Program
    {

        private static void Gsm_GsmStatusReceived(object sender, GsmStatusArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(e.Property + ":\r\n" + e.Value);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private static void Gsm_StatusReportRecievedEvent(object sender, StatusReport e)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Status: " + e.SendStatus + "\t" + e.Reciever + ":\r\n" + e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private static void Gsm_SmsRecievedEvent(object sender, Sms e)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(e.Sender + ":\r\n" + e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
