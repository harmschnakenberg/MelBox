using Grapevine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MelBoxWeb
{
    /*
     *
     */

    public partial class Server
    {
        
        private static IRestServer restServer;

        public static int Level_Admin { get; set; } = 9000; //Benutzerverwaltung u. -Einteilung
        public static int Level_Reciever { get; set; } = 2000; //Empfänger bzw. Bereitschaftsnehmer

        //public static List<string> Urls { get; private set; }

        public static string Html_Skeleton { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Skeleton.html");
        public static string Html_FormLogin { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "FormLogin.html");
        public static string Html_FormMessage { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "FormMessage.html");
        public static string Html_FormAccount { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "FormAccount.html");
        public static string Html_FormCompany { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "FormCompany.html");
        public static string Html_FormRegister { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "FormRegister.html");
        public static string Html_FormShift { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "FormShift.html");
        public static string Html_FormGsm { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "FormGsm.html");

        /// <summary>
        /// GUID - User-Id
        /// </summary>
        public static Dictionary<string, MelBoxSql.Contact> LogedInHash = new Dictionary<string, MelBoxSql.Contact>();

        public static void Start()
        {
            if (restServer != null && restServer.IsListening) return;

            try
            {
                restServer = RestServerBuilder.From<Startup>().Build();
                //restServer = RestServerBuilder.UseDefaults().Build();

                //Urls = restServer.Prefixes.ToList();

                restServer.AfterStarting += (s) =>
                {
                    Process.Start("explorer", s.Prefixes.First());
                };

                restServer.AfterStopping += (s) =>
                {
                    Console.WriteLine("Web-Server beendet.");
                };

                //Urls = restServer.Prefixes.ToList();
                restServer.Start();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void Stop()
        {
            if (restServer != null)
                restServer.Stop();
        }
    }
}
