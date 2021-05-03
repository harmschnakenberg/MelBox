using System;
using System.Collections.Generic;
using System.Text;

namespace MelBoxSql
{
    public static class Tab_Shift
    {
        internal const string TableName = "Shift";

        private static Dictionary<string, object> ToDictionary(Shift shift)
        {
            Dictionary<string, object> set = new Dictionary<string, object>();

            if (shift.Id > 0) set.Add(nameof(shift.Id), shift.Id);
            if (shift.EntryTime != null && shift.EntryTime != DateTime.MinValue) set.Add(nameof(shift.EntryTime), shift.EntryTime);
            if (shift.ContactId > 0) set.Add(nameof(shift.ContactId), shift.ContactId);
            if (shift.Start != null && shift.EntryTime != DateTime.MinValue) set.Add(nameof(shift.Start), shift.Start);
            if (shift.End != null && shift.EntryTime != DateTime.MinValue) set.Add(nameof(shift.End), shift.End);

            return set;
        }

        public static bool CreateTable()
        {
            Dictionary<string, string> columns = new Dictionary<string, string>
                {
                    { nameof(Shift.Id), "INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT" },
                    { nameof(Shift.EntryTime), "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP" },
                    { nameof(Shift.ContactId), "INTEGER NOT NULL" },
                    { nameof(Shift.Start), "TEXT" },
                    { nameof(Shift.End), "TEXT" }
                };

            return Sql.CreateTable2(TableName, columns);
        }

        public static bool Insert(Shift shift)
        {
            return Sql.Insert(TableName, ToDictionary(shift));
        }

        public static bool Insert(int ContactId, DateTime start, DateTime end)
        {
            bool success = true;


            DateTime s = start;
            DateTime e = end;

            do
            {
                if ( DateTime.Compare(s.AddDays(1), e) < 0) // <0: t1 liegt vor t2.
                {
                    e = s.AddDays(1);
                }

                Shift shift = new Shift();
                shift.ContactId = ContactId;
                shift.EntryTime = DateTime.UtcNow;
                shift.Start = ShiftStart(s).ToUniversalTime();
                shift.End = ShiftEnd(s).ToUniversalTime();
                s = e;
                e = s.AddDays(1);

                success &= Sql.Insert(TableName, ToDictionary(shift));

                Console.WriteLine("Schicht von " + shift.Start + " bis " + shift.End);
            }
            while (DateTime.Compare(s, end) < 0);
            

            return success;
        }
    
        public static bool Update(Shift set, Shift where)
        {
            if (DateTime.Compare(set.Start, set.End) > 0) //t1 ist später als t2.
                return false;

            return Sql.Update(TableName, ToDictionary(set), ToDictionary(where));
        }

        public static bool Delete(Shift where)
        {
            return Sql.Delete(TableName, ToDictionary(where));
        }

        public static System.Data.DataTable Select(Shift where)
        {
            Dictionary<string, object> columns = ToDictionary(where);

            string query = "SELECT * FROM " + TableName + " WHERE ";
            query += Sql.ColNameAlias(columns.Keys, " AND ");

            return Sql.SelectDataTable("Bereitschaft", query, Sql.Alias(columns));
        }

        public static Shift Select(int shiftId)
        {           
            string query = "SELECT * FROM " + TableName + " WHERE Id = " + shiftId;
            System.Data.DataTable dt = Sql.SelectDataTable("Eine Nacht", query, null);

            Shift shift = new Shift();

            if (dt.Rows.Count == 0) return shift;

            DateTime.TryParse(dt.Rows[0][nameof(shift.EntryTime)].ToString(), out DateTime entryTime);
            int.TryParse(dt.Rows[0][nameof(shift.ContactId)].ToString(), out int contactId);
            DateTime.TryParse(dt.Rows[0][nameof(shift.Start)].ToString(), out DateTime start);
            DateTime.TryParse(dt.Rows[0][nameof(shift.End)].ToString(), out DateTime end);

            shift.Id = shiftId;
            shift.EntryTime = entryTime;
            shift.ContactId = contactId;
            shift.Start = start;
            shift.End = end;
          
            return shift;            
        }


        // Beachte: SELECT LAST_INSERT_ROWID();


        public static DateTime ShiftStart(DateTime date)
        {
            int hour = 17;
            DayOfWeek day = date.DayOfWeek;
            if (day == DayOfWeek.Friday) hour = 15;
            if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday || IsHolyday(date)) hour = 7;

            return date.Date.AddHours(hour);
        }

        public static DateTime ShiftEnd(DateTime date)
        {
            int hour = 7;

            return date.Date.AddDays(1).AddHours(hour);
        }

        #region Feiertage

        // Aus VB konvertiert
        private static DateTime DateOsterSonntag(DateTime pDate)
        {
            int viJahr, viMonat, viTag;
            int viC, viG, viH, viI, viJ, viL;

            viJahr = pDate.Year;
            viG = viJahr % 19;
            viC = viJahr / 100;
            viH = (viC - viC / 4 - (8 * viC + 13) / 25 + 19 * viG + 15) % 30;
            viI = viH - viH / 28 * (1 - 29 / (viH + 1) * (21 - viG) / 11);
            viJ = (viJahr + viJahr / 4 + viI + 2 - viC + viC / 4) % 7;
            viL = viI - viJ;
            viMonat = 3 + (viL + 40) / 44;
            viTag = viL + 28 - 31 * (viMonat / 4);

            return new DateTime(viJahr, viMonat, viTag);
        }

        // Aus VB konvertiert
        public static List<DateTime> Holydays(DateTime pDate)
        {
            int viJahr = pDate.Year;
            DateTime vdOstern = DateOsterSonntag(pDate);
            List<DateTime> feiertage = new List<DateTime>
            {
                new DateTime(viJahr, 1, 1),    // Neujahr
                new DateTime(viJahr, 5, 1),    // Erster Mai
                vdOstern.AddDays(-2),          // Karfreitag
                vdOstern.AddDays(1),           // Ostermontag
                vdOstern.AddDays(39),          // Himmelfahrt
                vdOstern.AddDays(50),          // Pfingstmontag
                new DateTime(viJahr, 10, 3),   // TagderDeutschenEinheit
                new DateTime(viJahr, 10, 31),  // Reformationstag
                new DateTime(viJahr, 12, 24),  // Heiligabend
                new DateTime(viJahr, 12, 25),  // Weihnachten 1
                new DateTime(viJahr, 12, 26),  // Weihnachten 2
                new DateTime(viJahr, 12, DateTime.DaysInMonth(viJahr, 12)) // Silvester
            };

            return feiertage;
        }

        public static bool IsHolyday(DateTime date)
        {
            return Holydays(date).Contains(date);
        }

        #endregion

        /// <summary>
        /// Html: Optionen für Uhrzeit-Auswahl
        /// z.B. <option value="17" selected>17 Uhr</option>
        /// </summary>
        /// <param name="hourSelected"></param>
        /// <returns>html optionen für select </returns>
        public static string HtmlOptionHour(int hourSelected)
        {
            string options = string.Empty;
            string selected;

            for (int i = 0; i <= 24; i++)
            {
                selected = (i == hourSelected) ? "selected" : string.Empty;
                options += $"<option value='{i}' {selected}>{i} Uhr</option>" + Environment.NewLine;
            }

            return options;
        }

    }

    public class Shift
    {
        public Shift()
        { }

        public Shift(int contactId, DateTime date)
        {
            EntryTime = System.DateTime.UtcNow;
            ContactId = contactId;
            Start = Tab_Shift.ShiftStart(date);
            End = Tab_Shift.ShiftEnd(date);
        }

        public Shift(int contactId, DateTime start, DateTime end)
        {
            EntryTime = System.DateTime.UtcNow;
            ContactId = contactId;
            Start = start;
            End = end;
        }

        public int Id { get; set; }

        public System.DateTime EntryTime { get; set; }

        public int ContactId { get; set; }

        public System.DateTime Start { get; set; }

        public System.DateTime End { get; set; }
    }

}
