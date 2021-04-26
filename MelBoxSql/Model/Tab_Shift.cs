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
            if (shift.EntryTime != null) set.Add(nameof(shift.EntryTime), shift.EntryTime);
            if (shift.ContactId > 0) set.Add(nameof(shift.ContactId), shift.ContactId);
            if (shift.Start != null) set.Add(nameof(shift.Start), shift.Start);
            if (shift.End != null) set.Add(nameof(shift.End), shift.End);

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

        public static bool Update(Shift set, Shift where)
        {
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

    }

    public class Shift
    {
        public Shift()
        { }

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
