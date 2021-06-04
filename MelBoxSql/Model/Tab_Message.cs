using System;
using System.Collections.Generic;
using System.Text;

namespace MelBoxSql
{
    public static class Tab_Message
    {
        internal const string TableName = "Message";

       // [Flags]
        public enum BlockedDays
        {
            NaN = -1,
            None = 0,
            Mo = 1,
            Di = 2,
            Mi = 4,
            Do = 8,
            Fr = 16,
            //WeekDays = 31,
            Sa = 32,
            So = 64,
            //Weekend = 96,            
            //All = 127
        }

        public static BlockedDays BlockWeek (bool workdays = true, bool weekend = true)
        {
            BlockedDays blockedDays = BlockedDays.None;

            if (workdays)
            {
                blockedDays &= BlockedDays.Mo;
                blockedDays &= BlockedDays.Di;
                blockedDays &= BlockedDays.Mi;
                blockedDays &= BlockedDays.Do;
                blockedDays &= BlockedDays.Fr;
            }

            if (weekend)
            {
                blockedDays &= BlockedDays.Sa;
                blockedDays &= BlockedDays.So;
            }

            return blockedDays;
        }

        private static Dictionary<string, object> ToDictionary(Message message)
        {
            Dictionary<string, object> set = new Dictionary<string, object>();

            if (message.Id > 0) set.Add(nameof(message.Id), message.Id);
            if (message.EntryTime != DateTime.MinValue) set.Add(nameof(message.EntryTime), message.EntryTime);
            if (message.Content != null) set.Add(nameof(message.Content), message.Content);
            if (message.BlockedDays != BlockedDays.NaN) set.Add(nameof(message.BlockedDays), message.BlockedDays);
            if (message.StartBlockHour >= 0) set.Add(nameof(message.StartBlockHour), message.StartBlockHour);
            if (message.EndBlockHour >= 0) set.Add(nameof(message.EndBlockHour), message.EndBlockHour);

            return set;
        }

        public static bool CreateTable()
        {
            Dictionary<string, string> columns = new Dictionary<string, string>
                {
                    { nameof(Message.Id), "INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT" },
                    { nameof(Message.EntryTime), "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP" },
                    { nameof(Message.Content), "TEXT NOT NULL UNIQUE" },
                    { nameof(Message.BlockedDays), "INTEGER" },
                    { nameof(Message.StartBlockHour), "INTEGER" },
                    { nameof(Message.EndBlockHour), "INTEGER" }
                };

            return Sql.CreateTable(TableName, columns);
        }

        public static bool Insert(Message message)
        {
            return Sql.Insert(TableName, ToDictionary(message));
        }

        public static bool Update(Message set, Message where)
        {
            return Sql.Update(TableName, ToDictionary(set), ToDictionary(where));
        }

        public static bool Update(Message set, int whereId)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>
            {
                { nameof(Message.Id), whereId }
            };

            return Sql.Update(TableName, ToDictionary(set), dict);
        }

        public static bool Delete(Message where)
        {
            return Sql.Delete(TableName, ToDictionary(where));
        }

        public static System.Data.DataTable Select(Message where)
        {
            Dictionary<string, object> columns = ToDictionary(where);

            string query = "SELECT * FROM " + TableName + " WHERE ";
            query += Sql.ColNameAlias(columns.Keys, " AND ");

            return Sql.SelectDataTable("Inhalte", query, Sql.Alias(columns));
        }

        public static Message SelectMessage(int Id)
        {
            string query = "SELECT * FROM " + TableName + " WHERE Id = " + Id + "; ";
           
            System.Data.DataTable dt = Sql.SelectDataTable("Einzelnachricht", query, null);

            Message message = new Message();

            if (dt.Rows.Count == 0) return message;

            int.TryParse(dt.Rows[0][nameof(message.BlockedDays)].ToString(), out int blockedDaysInt);
            int.TryParse(dt.Rows[0][nameof(message.StartBlockHour)].ToString(), out int startBblockedHour);
            int.TryParse(dt.Rows[0][nameof(message.EndBlockHour)].ToString(), out int endBblockedHour);
            DateTime.TryParse(dt.Rows[0][nameof(message.EntryTime)].ToString(), out DateTime entryTime);
        
            message.Content = dt.Rows[0][nameof(message.Content)].ToString();
            message.BlockedDays = (BlockedDays)blockedDaysInt;
            message.EntryTime = entryTime.ToLocalTime();
            message.StartBlockHour = startBblockedHour;
            message.EndBlockHour = endBblockedHour;
            message.Id = Id;

            return message;
        }

        public static int SelectOrCreateMessageId(string Message)
        {
            string query = $"SELECT Id FROM {TableName} WHERE Content = '{Message}'; ";

            System.Data.DataTable dt = Sql.SelectDataTable("Id von Nachricht", query, null);

            if (dt.Rows.Count == 0)
            {
                Message message = new Message
                {
                    EntryTime = DateTime.UtcNow,
                    Content = Message,
                    BlockedDays = BlockedDays.None
                };

                if (!Insert(message))                
                    throw new Exception("SelectOrCreateMessageId(): Neue Nachricht \r\n" + message + "\r\nkonnte nicht in DB gespeichert werden.");                
                else                
                    return SelectOrCreateMessageId(Message); //Rekursiver Aufruf                
            }

            int.TryParse(dt.Rows[0][0].ToString(), out int messageId);
            
            return messageId;
        }

        public static string ExtractKeyWord(string message)
        {
            char[] split = new char[] { ' ', ',', '-', '.', ':', ';' };
            string[] words = message.Split(split);

            string KeyWords = words[0].Trim();

            if (words.Length > 1)
            {
                KeyWords += " " + words[1].Trim();
            }

            return KeyWords.ToLower();
        }

        //BAUSTELLE
        public static bool IsMessageBlockedNow(int messageId)
        {
            Message msg = SelectMessage(messageId);
            Console.WriteLine($"Gesperrt? Nachricht [{messageId}] {msg.Content}");

            bool blockedDay = false;
            DayOfWeek today = DateTime.Now.DayOfWeek;

            if (Tab_Shift.IsHolyday(DateTime.Now))
            {
                today = DayOfWeek.Sunday;
            }

            switch (today) 
            {               
                case DayOfWeek.Monday:
                    if ((msg.BlockedDays & BlockedDays.Mo) > 0)
                        blockedDay = true;
                        break;
                case DayOfWeek.Tuesday:
                    if ((msg.BlockedDays & BlockedDays.Di) > 0)
                        blockedDay = true;
                    break;
                case DayOfWeek.Wednesday:
                    if ((msg.BlockedDays & BlockedDays.Mi) > 0)
                        blockedDay = true;
                    break;
                case DayOfWeek.Thursday:
                    if ((msg.BlockedDays & BlockedDays.Do) > 0)
                        blockedDay = true;
                    break;
                case DayOfWeek.Friday:
                    if ((msg.BlockedDays & BlockedDays.Fr) > 0)
                        blockedDay = true;
                    break;
                case DayOfWeek.Saturday:
                    if ((msg.BlockedDays & BlockedDays.Sa) > 0)
                        blockedDay = true;
                    break;
                case DayOfWeek.Sunday:
                    if ((msg.BlockedDays & BlockedDays.So) > 0)
                        blockedDay = true;
                    break;                
            }

            if (blockedDay)
            {
                //jetzt 13 --> 17 bis 7
                if (msg.StartBlockHour >= DateTime.Now.Hour || (msg.EndBlockHour > 0 && msg.EndBlockHour < DateTime.Now.Hour) )
                    return true;
            }

            return false;
        }
    }

    public class Message
    {
       
        public int Id { get; set; }

        public System.DateTime EntryTime { get; set; } = DateTime.MinValue;

        public string Content { get; set; } = null;

        public Tab_Message.BlockedDays BlockedDays { get; set; } = Tab_Message.BlockedDays.NaN;

        public int StartBlockHour { get; set; } = -1;

        public int EndBlockHour { get; set; } = -1;
    }
}
