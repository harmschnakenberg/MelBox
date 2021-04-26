using System.Collections.Generic;

namespace MelBoxSql
{
    public static class Tab_Log
    {
        internal const string TableName = "Log";
              
        public enum Topic
        {
            None,
            Startup,
            Filesystem,
            Database,
            Gsm,
            Email,
            Web,
            InTouch
        }

        private static Dictionary<string, object> ToDictionary(Log log)
        {
            Dictionary<string, object> set = new Dictionary<string, object>();

            if (log.Id > 0) set.Add(nameof(log.Id), log.Id);
            set.Add(nameof(log.Topic), log.Topic);
            if (log.LogTime != null) set.Add(nameof(log.LogTime), log.LogTime);
            if (log.Prio > 0) set.Add(nameof(log.Prio), log.Prio);
            if (log.Content.Length > 0) set.Add(nameof(log.Content), log.Content);

            return set;
        }

        public static bool CreateTable()
        {
            Dictionary<string, string> columns = new Dictionary<string, string>
                {
                    { nameof(Log.Id) , "INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT" },
                    { nameof(Log.LogTime), "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP" },
                    { nameof(Log.Topic), "TEXT" },
                    { nameof(Log.Prio), "INTEGER NOT NULL" },
                    { nameof(Log.Content), "TEXT" }
                };

            return Sql.CreateTable2(TableName, columns);
        }

        public static bool Insert(Log company)
        {
            return Sql.Insert(TableName, ToDictionary(company));
        }

        public static bool Update(Log set, Log where)
        {
            return Sql.Update(TableName, ToDictionary(set), ToDictionary(where));
        }

        public static bool Delete(Log where)
        {
            return Sql.Delete(TableName, ToDictionary(where));
        }

        public static System.Data.DataTable Select(Log where)
        {
            Dictionary<string, object> columns = ToDictionary(where);

            string query = "SELECT * FROM " + TableName + " WHERE ";
            query += Sql.ColNameAlias(columns.Keys, " AND ");

            return Sql.SelectDataTable("Log", query, Sql.Alias(columns));
        }

    }

    public class Log
    {
        public Log()
        { }

        public Log(Tab_Log.Topic topic, int prio, string content)
        {
            LogTime = System.DateTime.UtcNow;
            Topic = topic;
            Prio = prio;
            Content = content;
        }

        public int Id { get; set; }

        public System.DateTime LogTime { get; set; }

        public Tab_Log.Topic Topic { get; set; }

        public int Prio { get; set; }

        public string Content { get; set; }
    }
}
