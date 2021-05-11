using System;
using System.Collections.Generic;
using System.Text;

namespace MelBoxSql
{
    public static class Tab_Sent
    {
        internal const string TableName = "Sent";

        public enum Confirmation
        {
            NaN = -1,
            Unknown,
            AwaitingRefernece,
            PendingAnswer,
            RetrySending,
            AbortedSending,
            SentSuccessful
        }

        private static Dictionary<string, object> ToDictionary(Sent sent)
        {
            Dictionary<string, object> set = new Dictionary<string, object>();

            if (sent.Id > 0) set.Add(nameof(sent.Id), sent.Id);
            if (sent.SentTime != null) set.Add(nameof(sent.SentTime), sent.SentTime);
            if (sent.ToVia != Tab_Contact.Communication.NaN) set.Add(nameof(sent.ToVia), sent.ToVia);
            if (sent.ToId > 0) set.Add(nameof(sent.ToId), sent.ToId);
            if (sent.ContentId > 0) set.Add(nameof(sent.ContentId), sent.ContentId);
            if (sent.Reference > 0) set.Add(nameof(sent.Reference), sent.Reference);
            if (sent.Confirmation != Confirmation.NaN) set.Add(nameof(sent.Confirmation), sent.Confirmation);

            return set;
        }

        public static bool CreateTable()
        {           
            Dictionary<string, string> columns = new Dictionary<string, string>
                {
                    { nameof(Sent.Id), "INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT" },
                    { nameof(Sent.SentTime), "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP" },
                    { nameof(Sent.ToId), "INTEGER NOT NULL" },
                    { nameof(Sent.ToVia), "INTEGER NOT NULL" },
                    { nameof(Sent.ContentId), "INTEGER NOT NULL" },
                    { nameof(Sent.Reference), "INTEGER" },
                    { nameof(Sent.Confirmation), "INTEGER" },
                };

            List<string> constrains = new List<string>()
            {
                { "CONSTRAINT fk_ToId FOREIGN KEY (" + nameof(Sent.ToId) +") REFERENCES " + Tab_Contact.TableName + "(Id) ON DELETE SET DEFAULT" },
             // { "CONSTRAINT fk_ToVia FOREIGN KEY (" + nameof(Sent.ToVia) +") REFERENCES " + Tab_Contact.Communication_TableName + "(Id) ON DELETE SET DEFAULT" },
                { "CONSTRAINT fk_ContentId FOREIGN KEY (" + nameof(Sent.ContentId) + ") REFERENCES " + Tab_Message.TableName + "(Id) ON DELETE SET DEFAULT" }
            };

            return Sql.CreateTable2(TableName, columns, constrains);
        }

        public static bool Insert(Sent sent)
        {
            return Sql.Insert(TableName, ToDictionary(sent));
        }

        public static bool UpdateSendStatus(int internalReference, Tab_Sent.Confirmation confirmation)
        {
            const string query = "UPDATE Sent SET Confirmation = @Confirmation WHERE Id IN ( SELECT Id FROM Sent WHERE Reference = @Reference ORDER BY Id DESC LIMIT 1 ); ";

            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("@Confirmation", confirmation);
            args.Add("@Reference", internalReference);

            return Sql.NonQuery(query, args);
        }

        public static bool Update(Sent set, Sent where)
        {
            return Sql.Update(TableName, ToDictionary(set), ToDictionary(where));
        }

        public static bool Delete(Sent where)
        {
            return Sql.Delete(TableName, ToDictionary(where));
        }

        public static System.Data.DataTable Select(Sent where)
        {
            Dictionary<string, object> columns = ToDictionary(where);

            string query = "SELECT * FROM " + TableName + " WHERE ";
            query += Sql.ColNameAlias(columns.Keys, " AND ");

            return Sql.SelectDataTable("Versendet", query, Sql.Alias(columns));
        }

    }

    public class Sent
    {
        public Sent()
        { }

        public Sent(int toId, int contentId, Tab_Contact.Communication toVia)
        {
            ToId = toId;
            ContentId = contentId;
            ToVia = toVia;
        }

        public int Id { get; set; }

        public System.DateTime SentTime { get; set; }

        public int ToId { get; set; }

        public int ContentId { get; set; }

        public Tab_Contact.Communication ToVia { get; set; } = Tab_Contact.Communication.NaN;

        public int Reference { get; set; }

        public Tab_Sent.Confirmation Confirmation { get; set; } = Tab_Sent.Confirmation.NaN;
    }
}
