using System;
using System.Collections.Generic;

namespace MelBoxSql
{
    public static class Tab_Recieved
    {
        internal const string TableName = "Recieved";

        private static Dictionary<string, object> ToDictionary(Recieved recieved)
        {
            Dictionary<string, object> set = new Dictionary<string, object>();

            if (recieved.Id > 0) set.Add(nameof(recieved.Id), recieved.Id);
            if (recieved.RecTime > DateTime.MinValue) set.Add(nameof(recieved.RecTime), recieved.RecTime.ToString("yyyy-MM-dd HH:mm:ss"));
            if (recieved.FromId > 0) set.Add(nameof(recieved.FromId), recieved.FromId);
            if (recieved.ContentId > 0) set.Add(nameof(recieved.ContentId), recieved.ContentId);

            return set;
        }

        public static bool CreateTable()
        {
            Dictionary<string, string> columns = new Dictionary<string, string>
                {
                    { nameof(Recieved.Id), "INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT" },
                    { nameof(Recieved.RecTime), "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP" },
                    { nameof(Recieved.FromId), "INTEGER NOT NULL" },
                    { nameof(Recieved.ContentId), "INTEGER NOT NULL" },
                };

            List<string> constrains = new List<string>()
            {
                { "CONSTRAINT fk_FromId FOREIGN KEY (" + nameof(Recieved.FromId) +") REFERENCES " + Tab_Contact.TableName + "(Id) ON DELETE SET DEFAULT" },
                { "CONSTRAINT fk_ContentId FOREIGN KEY (" + nameof(Recieved.ContentId) + ") REFERENCES " + Tab_Message.TableName + "(Id) ON DELETE SET DEFAULT" }
            };

            return Sql.CreateTable(TableName, columns, constrains);
        }

        public static bool Insert(Recieved recieved)
        {
            return Sql.Insert(TableName, ToDictionary(recieved));
        }

        //public static bool InsertRecSms(string sender, string message, DateTime recievedUtcTime)
        //{
        //    int fromId = MelBoxSql.Tab_Contact.SelectContactId(sender);
        //    int messageId = MelBoxSql.Tab_Message.SelectOrCreateMessageId(message);

        //    Recieved recieved1 = new Recieved(fromId, messageId)
        //    {
        //        RecTime = recievedUtcTime
        //    };

        //    return Sql.Insert(TableName, ToDictionary(recieved1));
        //}

        public static bool Update(Recieved set, Recieved where)
        {
            return Sql.Update(TableName, ToDictionary(set), ToDictionary(where));
        }

        public static bool Delete(Recieved where)
        {
            return Sql.Delete(TableName, ToDictionary(where));
        }

        public static System.Data.DataTable Select(Recieved where)
        {
            Dictionary<string, object> columns = ToDictionary(where);

            string query = "SELECT * FROM " + TableName + " WHERE ";
            query += Sql.ColNameAlias(columns.Keys, " AND ");

            return Sql.SelectDataTable("Empfangen", query, Sql.Alias(columns));
        }


        public static Recieved SelectRecieved(int Id)
        {
            string query = "SELECT * FROM " + TableName + " WHERE Id = " + Id + "; ";

            System.Data.DataTable dt = Sql.SelectDataTable("Einzelempfang", query, null);

            Recieved rec = new Recieved();

            if (dt.Rows.Count == 0) return rec;

            int.TryParse(dt.Rows[0][nameof(rec.FromId)].ToString(), out int fromId);
            int.TryParse(dt.Rows[0][nameof(rec.ContentId)].ToString(), out int contentId);
            DateTime.TryParse(dt.Rows[0][nameof(rec.RecTime)].ToString(), out DateTime recTime);

            rec.Id = Id;
            rec.RecTime = recTime;
            rec.FromId = fromId;
            rec.ContentId = contentId;

            return rec;
        }

    }

    public class Recieved
    {
        public Recieved()
        { }

        public Recieved(int id)
        {
            Id = id;
        }

        public Recieved(int fromId, int contentId)
        {
            FromId = fromId;
            ContentId = contentId;
        }

        public int Id { get; set; }

        public System.DateTime RecTime { get; set; } = System.DateTime.MinValue;

        public int FromId { get; set; } = -1;

        public int ContentId { get; set; } = -1;
    }
}
