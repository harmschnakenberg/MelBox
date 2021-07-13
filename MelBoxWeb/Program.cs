using System;

namespace MelBoxWeb
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Progammstart.");

            MelBoxWeb.Server.Start();

            Console.WriteLine("Beliebige Taste zum beenden..");
            Console.ReadKey();

            MelBoxWeb.Server.Stop();
            Console.WriteLine("Progammende.");
        }
    }
}
