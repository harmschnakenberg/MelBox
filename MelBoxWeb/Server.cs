using Grapevine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MelBoxWeb
{
    public partial class Server
    {
        private static string _serverPort = PortFinder.FindNextLocalOpenPort(1234);

        public static string Html_Skeleton { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Skeleton.html");
        public static string Html_FormLogin { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "FormLogin.html");
        public static string Html_FormMessage { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "FormMessage.html");
        public static string Html_FormAccount { get; } = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "FormAccount.html");

        /// <summary>
        /// GUID - User-Id
        /// </summary>
        public static Dictionary<string, int> LogedInHash = new Dictionary<string, int>();

        public static void Start()
        {

            using (var server = RestServerBuilder.UseDefaults().Build())
            {
                server.Prefixes.Add($"http://localhost:{_serverPort}/");

                Console.WriteLine(string.Join(", ", server.Prefixes));

                server.AfterStarting += (s) =>
                {
                    Process.Start("explorer", s.Prefixes.First());
                };

                server.AfterStopping += (s) =>
                {
                    Console.WriteLine("Web-Server beendet.");
                };

                //server.Prefixes.Add("https://*:443/");
                server.Start();


                Console.WriteLine("Press enter to stop the server");
                Console.ReadLine();
            }

        }





    }
}
