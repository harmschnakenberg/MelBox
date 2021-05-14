using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;

namespace MelBoxCore
{
    partial class Program
    {
        public static ulong AdminPhone { get; set; } = 4916095285304;

        #region Täglicher Timer
        static TimeSpan CalcLeftTime(int hourOfDay = 8)
        {
            TimeSpan day = new TimeSpan(24, 00, 00);                        // 24 hours in a day.
            TimeSpan now = TimeSpan.Parse(DateTime.Now.ToString("HH:mm"));  // The current time in 24 hour format
            TimeSpan activationTime = new TimeSpan(hourOfDay, 0, 0);                // z.B 08:00:00

            TimeSpan timeLeftUntilFirstRun = ((day - now) + activationTime);
            if (timeLeftUntilFirstRun.TotalHours > 24)
                timeLeftUntilFirstRun -= day;            // Deducts a day from the schedule so it will run today.

            return timeLeftUntilFirstRun;
        }


        public void SetDailyTimer()
        {
            Timer execute = new Timer
            {
                Interval = CalcLeftTime().TotalMilliseconds
            };
            execute.Elapsed += new ElapsedEventHandler(DailyTrigger);    // Event to do your tasks.
            execute.AutoReset = false;
            execute.Start();
        }

        private void DailyTrigger(object sender, ElapsedEventArgs e)
        {
            //1) Kontroll-SMS versenden
            MelBoxGsm.Gsm.Ask_SmsSend($"+{MelBoxGsm.Gsm.AdminPhone}", "SMS-Zentrale Routinemeldung");

            //2) Melde überfällige Alarmsender
            System.Data.DataTable overdue = MelBoxSql.Sql.Overdue_View();

            for (int i = 0; i < overdue.Rows.Count; i++)
            {
                string name = overdue.Rows[i]["Name"].ToString();
                string company = overdue.Rows[i]["Firma"].ToString();
                string due = overdue.Rows[i]["Fällig_seit"].ToString();

                string text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + $" Inaktivität >{name}<, >{company}< Meldung fällig seit >{due}< Melsys vor Ort prüfen.";

                Email.Send(null, text, $"Inaktivität >{name}<, >{company}<");                
            }

            //3) Backup der Datenbank
            MelBoxSql.Sql.DbBackup();

            //Starte Täglichen Timer erneut
            SetDailyTimer();
        }

        #endregion

       
    }
}
