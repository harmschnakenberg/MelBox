using System;
using System.Timers;

namespace MelBoxCore
{
    partial class Program
    {
        public static int HourOfDailyTasks { get; set; } = 8;

        /// <summary>
        /// Starte zu jeder vollen Stunde
        /// </summary>
        public static void SetHourTimer()
        {
            //Zeit bis zur nächsten vollen Stunde 
            int min = 59 - DateTime.Now.Minute;
            int sec = 59 - DateTime.Now.Second;

            TimeSpan span = new TimeSpan(0, min, sec);

            Timer execute = new Timer
            {
                Interval = span.TotalMilliseconds
            };

            execute.Elapsed += new ElapsedEventHandler(HourlySenderCheck);
            execute.Elapsed += new ElapsedEventHandler(DailyNotification);
            execute.Elapsed += new ElapsedEventHandler(DailyBackup);

            execute.AutoReset = false;
            execute.Start();

            
        }

        /// <summary>
        /// Tägliche Kontroll-SMS versenden
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DailyNotification(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.Hour != HourOfDailyTasks) return;

            Console.WriteLine($"Versende tägliche Kontroll-SMS an " + $"+{MelBoxGsm.Gsm.AdminPhone}");
            MelBoxGsm.Gsm.Ask_SmsSend($"+{MelBoxGsm.Gsm.AdminPhone}", "SMS-Zentrale Routinemeldung");
        }

        /// <summary>
        /// Prüft, ob ein aktuelles Backup der Datenbank vorhanden ist und erstellt ggf. eins
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DailyBackup(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.Hour != HourOfDailyTasks) return;

            Console.WriteLine("Prüfe / erstelle Backup der Datenbank.");
            MelBoxSql.Sql.DbBackup();
        }

        /// <summary>
        /// Stündliche Prüfung auf Inaktivität
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void HourlySenderCheck(object sender, ElapsedEventArgs e)
        {
            if (
                DateTime.Now.DayOfWeek == DayOfWeek.Saturday ||
                DateTime.Now.DayOfWeek == DayOfWeek.Sunday ||
                MelBoxSql.Tab_Shift.IsHolyday(DateTime.Now)
                )
                return; //Nur an Werktagen

            Console.WriteLine($"Prüfe auf inaktive Sender.");

            //Melde überfällige Alarmsender
            System.Data.DataTable overdue = MelBoxSql.Sql.Overdue_View();

            for (int i = 0; i < overdue.Rows.Count; i++)
            {
                string name = overdue.Rows[i]["Name"].ToString();
                string company = overdue.Rows[i]["Firma"].ToString();
                string due = overdue.Rows[i]["Fällig_seit"].ToString();

                string text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + $" Inaktivität >{name}<, >{company}<. Meldung fällig seit >{due}<. Melsys bzw. Segno vor Ort prüfen.";

                Email.Send(Email.Admin, text, $"Inaktivität >{name}<, >{company}<");
            }

            //Starte Timer erneut
            SetHourTimer();
        }

    }
}
