using System;
using System.Collections.Generic;

namespace MelBoxSql
{
    public partial class Sql
    {

        /// <summary>
        /// Erzeugt eine Zeichenkette der Form KeyName = @KeyName Delemiter [..]
        /// z.B. Name = @Name AND Id = @Id
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        internal static string ColNameAlias(Dictionary<string, object>.KeyCollection columns, string delimiter)
        {
            string line = string.Empty;

            bool first = true;
            foreach (var colName in columns)
            {
                if (first) { first = false; } else { line += delimiter; }
                line += colName + " = @" + colName;
            }

            return line;
        }

        /// <summary>
        /// Setzt vor den Schlüssel ein '@'
        /// Key, Value --> @Key, Value
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static Dictionary<string, object> Alias(Dictionary<string, object> args)
        {
            Dictionary<string, object> newArgs = new Dictionary<string, object>();

            foreach (var key in args.Keys)
            {
                newArgs.Add("@" + key, args[key]);
            }

            return newArgs;
        }


        internal static int GetIso8601WeekOfYear(DateTime time)
        {
            // This presumes that weeks start with Monday.
            // Week 1 is the 1st week of the year with a Thursday in it.
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            DayOfWeek day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }


        /// <summary>
        /// Für Hilfstabeelle Empfangsbestätigungen (Später löschen?)
        /// </summary>
        /// <param name="dischargeTime"></param>
        /// <param name="internalReference"></param>
        /// <returns></returns>
        public static bool InsertReportProtocoll(DateTime dischargeTime, int internalReference)
        {
            Dictionary<string, object> set = new Dictionary<string, object>
            {
                { "DischargeTime", dischargeTime },
                { "InternalReference", internalReference }
            };

            return Sql.Insert("Reports", set);
        }


    }

}
