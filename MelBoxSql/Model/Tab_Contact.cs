using System;
using System.Collections.Generic;
using System.Text;

namespace MelBoxSql
{
    public static class Tab_Contact
    {
        internal const string TableName = "Contact";
      
       // [Flags]
        public enum Communication
        {
            NaN = -1,
            Unknown = 0,
            Sms = 1,
            Email = 2,
           // SmsAndEmail = 3,
            AlwaysEmail = 4
        }

        private static Dictionary<string, object> ToDictionary(Contact contact)
        {
            Dictionary<string, object> set = new Dictionary<string, object>();

            if (contact.Id > 0) set.Add(nameof(contact.Id), contact.Id);
            if (contact.EntryTime != DateTime.MinValue ) set.Add(nameof(contact.EntryTime), contact.EntryTime);
            if (contact.Name != null) set.Add(nameof(contact.Name), contact.Name);
            if (contact.Password != null) set.Add(nameof(contact.Password), contact.Password);
            if (contact.Accesslevel >= 0) set.Add(nameof(contact.Accesslevel), contact.Accesslevel);
            if (contact.CompanyId > 0) set.Add(nameof(contact.CompanyId), contact.CompanyId);
            if (contact.Email != null) set.Add(nameof(contact.Email), contact.Email);
            if (contact.Phone > 0) set.Add(nameof(contact.Phone), contact.Phone);
            if (contact.KeyWord != null) set.Add(nameof(contact.KeyWord), contact.KeyWord);
            if (contact.MaxInactiveHours >= 0) set.Add(nameof(contact.MaxInactiveHours), contact.MaxInactiveHours);
            if (contact.Via != Communication.NaN) set.Add(nameof(contact.Via), contact.Via);

            return set;
        }

        public static bool CreateTable()
        {
                Dictionary<string, string> columns = new Dictionary<string, string>
                {
                    { nameof(Contact.Id), "INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT" },
                    { nameof(Contact.EntryTime),  "TEXT  DEFAULT CURRENT_TIMESTAMP" },
                    { nameof(Contact.Name), "TEXT NOT NULL" },
                    { nameof(Contact.Password), "TEXT" },
                    { nameof(Contact.Accesslevel), "INTEGER DEFAULT 0" },
                    { nameof(Contact.CompanyId), "INTEGER" },
                    { nameof(Contact.Email), "TEXT" },
                    { nameof(Contact.Phone), "INTEGER" },
                    { nameof(Contact.KeyWord), "TEXT" },
                    { nameof(Contact.MaxInactiveHours), "INTEGER" },
                    { nameof(Contact.Via), "INTEGER DEFAULT 0" },
                };

            List<string> constrains = new List<string>()
            {
                { "CONSTRAINT fk_CompanyId FOREIGN KEY (" + nameof(Contact.CompanyId) + ") REFERENCES " + Tab_Company.TableName + "(Id) ON DELETE SET NULL" }
            };

            return Sql.CreateTable(TableName, columns, constrains);
        }

        public static bool Insert(Contact contact)
        {
            Console.WriteLine("Erstelle neuen Benutzer: " + contact.Name);
            return Sql.Insert(TableName, ToDictionary(contact));
        }


        /// <summary>
        /// Legt einen neuen Benutzer Anhand einer Telefonnummer und/oder einer empfangenen Nachricht an.
        /// </summary>
        /// <param name="phone">Telefonnummer. Andere Zeichenfolgen werden ignoriert.</param>
        /// <param name="message">Nachricht wird genutzt um KeyWord zu extrahieren</param>
        /// <returns>Neuer Eintrag erfolgreich erstellt</returns>
        public static bool InsertNewContact(string phone, string message)
        {
            Contact contact = new Contact
            {
                Accesslevel = 0,
                EntryTime = DateTime.UtcNow,
                KeyWord = Tab_Message.ExtractKeyWord(message),
                CompanyId = 1 // ohne Zuordnung aufgrund SELECT-Anweisung keine Darstellung in Weboberfläche.
            };
            contact.Name = $"NeuerBenutzer '{contact.KeyWord}' vom {contact.EntryTime.ToShortDateString()}";
          
            if (ulong.TryParse(phone.Trim('+'), out ulong _phone))
                contact.Phone = _phone;

            return MelBoxSql.Tab_Contact.Insert(contact);
        }

        public static bool Update(Contact set, Contact where)
        {
            return Sql.Update(TableName, ToDictionary(set), ToDictionary(where));
        }

        public static bool Delete(Contact where)
        {
            return Sql.Delete(TableName, ToDictionary(where));
        }

        public static System.Data.DataTable Select(Contact where)
        {
            Dictionary<string, object> columns = ToDictionary(where);

            string query = "SELECT * FROM " + TableName + " WHERE ";
            query += Sql.ColNameAlias(columns.Keys, " AND ");

            return Sql.SelectDataTable("Kontakt", query, Sql.Alias(columns));
        }

        public static Contact SelectContact(int Id)
        {
            string query = "SELECT * FROM " + TableName + " WHERE Id = " + Id + "; ";

            System.Data.DataTable dt = Sql.SelectDataTable("Einzelner Kontakt", query, null);

            Contact contact = new Contact();

            if (dt.Rows.Count == 0) return contact;

            DateTime.TryParse(dt.Rows[0][nameof(contact.EntryTime)].ToString(), out DateTime entryTime);
            string name = dt.Rows[0][nameof(contact.Name)].ToString();
           // string password = dt.Rows[0][nameof(contact.Password)].ToString();
            int.TryParse(dt.Rows[0][nameof(contact.CompanyId)].ToString(), out int companyId);
            int.TryParse(dt.Rows[0][nameof(contact.Accesslevel)].ToString(), out int accessLevel);
            string email = dt.Rows[0][nameof(contact.Email)].ToString();
            ulong.TryParse(dt.Rows[0][nameof(contact.Phone)].ToString(), out ulong phone);
            string keyWord = dt.Rows[0][nameof(contact.KeyWord)].ToString();
            int.TryParse(dt.Rows[0][nameof(contact.MaxInactiveHours)].ToString(), out int maxInactiveHours);
            int.TryParse(dt.Rows[0][nameof(contact.Via)].ToString(), out int via);

            contact.Id = Id;
            contact.EntryTime = entryTime.ToLocalTime();
            contact.Name = name;
            //contact.Password = null; // NUR FÜR LOGIN AUSLESEN!
            contact.CompanyId = companyId;
            contact.Accesslevel = accessLevel;
            contact.Email = email;
            contact.Phone = phone;
            contact.KeyWord = keyWord;
            contact.MaxInactiveHours = maxInactiveHours;
            contact.Via = (Tab_Contact.Communication)via;
         
            return contact;
        }

        /// <summary>
        /// Fidnet die Id eines Kontakts anhand von Name, Telefon, Email oder KeyWord
        /// </summary>
        /// <param name="ident">Name oder Telefon oder Email oder KeyWord</param>
        /// <returns>Id des Kontakts</returns>
        public static int SelectContactId(string ident)
        {
            if (ident == null)
            {
                Console.WriteLine("SelectContactId(): Es wurde kein Identifizierungs-Paranmeter gesetzt.");
                return 0;
            }

            ident = ident.TrimStart('+'); //Telefonnummer

            string query = $"SELECT Id FROM {TableName} WHERE Name Like '%{ident}%' OR Phone = '{ident}' OR Email = '{ident}' OR KeyWord = '{ident.ToLower()}'; ";

            System.Data.DataTable dt = Sql.SelectDataTable("Kontakt-Id", query, null);

            if (dt.Rows.Count == 0)
            {
                Console.WriteLine("SelectContactId(): Es konnte kein Benutzer mit >" + ident + "< gefunden werden.");
                return 0;
            }

            int.TryParse(dt.Rows[0][0].ToString(), out int contactId);
           
            return contactId;
        }

        public static System.Data.DataTable SelectContactList(int accesslevel, int contactId = 0, string operation = "<=")
        {          
            string query = "SELECT Contact.Id AS Id, Contact.Name AS Name, Contact.Accesslevel AS Level, Company.Name AS Firma, Company.City AS Ort" +
                " FROM " + TableName + 
                " JOIN " + Tab_Company.TableName + " ON Company.Id = Contact.CompanyId" + //IFNULL() da sonst Kontakte ohne Firmeneintrag nicht angezeigt werden.
                " WHERE Accesslevel " + operation + " " + accesslevel +
                " ORDER BY Contact.Name ";

            if (contactId > 0)
                query += " AND Contact.Id = " + contactId;

            return Sql.SelectDataTable("Kontakte", query, null);
        }

        public static System.Data.DataTable SelectWatchedContactList()
        {
            string query = "SELECT Contact.Id AS Id, Contact.Name AS Name, Company.Name AS Firma, substr(Company.City, instr(Company.City,' ') + 1) AS Ort, MaxInactiveHours || ' Std.' AS Max_Inaktiv" +
                " FROM " + TableName +
                " JOIN " + Tab_Company.TableName + " ON Company.Id = Contact.CompanyId " +
                " WHERE MaxInactiveHours > 0 " +
                " ORDER BY MaxInactiveHours;";

            return Sql.SelectDataTable("Überwachte Kontakte", query, null);
        }

        public static System.Data.DataTable SelectPermanentEmailRecievers()
        {
            string query = "SELECT Email, Name " +
                " FROM " + TableName +
                " WHERE Email LIKE '%@%' AND Via IN (4, 5, 6)" +
                " ; ";

           return Sql.SelectDataTable("Ständige EMpfänger", query, null);
        }

        public static string SelectName_Company_City(int contactId)
        {
            Contact contact = SelectContact(contactId);

            Company company = Tab_Company.SelectCompany(contact.CompanyId);

            if (contactId == 0 || contact.CompanyId < 1) return "-leer-";

            return $"{contact.Name}, {company.Name}, {System.Text.RegularExpressions.Regex.Replace(company.City, @"\d", "")}";
        }

        #region Hilfs-Methoden zu Kontakten

            public static string Encrypt(string password)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(password);
            data = new System.Security.Cryptography.SHA256Managed().ComputeHash(data);
            return System.Text.Encoding.UTF8.GetString(data);
        }

        public static bool IsEmail(string mailAddress)
        {
            System.Text.RegularExpressions.Regex mailIDPattern = new System.Text.RegularExpressions.Regex(@"[\w-]+@([\w-]+\.)+[\w-]+");

            if (!string.IsNullOrEmpty(mailAddress) && mailIDPattern.IsMatch(mailAddress))
                return true;
            else
                return false;
        }

        public static string HtmlOptionContacts(int accesslevel, int contactId, bool isAdmin)
        {
            System.Data.DataTable dt = Tab_Contact.SelectContactList(accesslevel, isAdmin ? 0 : contactId, ">=");
            //  Id, Name, Firma, Ort

            string options = string.Empty;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //<option value="2">Option 2</option>

                int id = int.Parse(dt.Rows[i][0].ToString());
                string name = dt.Rows[i][1].ToString();
                string selected = (id == contactId) ? "selected" : string.Empty;

                if (isAdmin || id == contactId)
                    options += $"<option value='{id}' {selected}>{name}</option>" + Environment.NewLine;
            }

            return options;
        }

        #endregion

        /// <summary>
        /// Identifiziert einen Kontakt.
        /// </summary>
        /// <param name="name">Benutzername</param>
        /// <param name="password">Passwort</param>
        /// <returns>Id des zugehörigen Kontakts, 0 bei Fehlschlag</returns>
        public static int Authentification(string name, string password)
        {
            string encryped_pw = Encrypt(password);

            string query = "SELECT Id FROM " + TableName + " WHERE Name = @Name AND Password = @Password AND Accesslevel > 0;";

            Dictionary<string, object> valuePairs = new Dictionary<string, object>
            {
                { "@Name", name },
                { "@Password", encryped_pw }
            };

            System.Data.DataTable dt = Sql.SelectDataTable("Kontakt", query, valuePairs);

            int.TryParse(Sql.GetFirstEntry(dt, nameof(Contact.Id)), out int result);

            return result;
        }

    }

    public class Contact
    {
        public Contact()
        { }

        public Contact(int id)
        {
            Id = id;
        }

        public Contact(string name)
        {           
            Name = name;           
        }

        public int Id { get; set; }

        private System.DateTime _EntryTime = DateTime.MinValue;
        public System.DateTime EntryTime 
        { 
            get
            {return _EntryTime;} 
            
            set
            { _EntryTime = value; }
        } 

        public string Name { get; set; } = null;

        public string Password { get; set; } = null;

        public int CompanyId { get; set; } = -1;

        public int Accesslevel { get; set; } = -1;

        private string _Email = null;
        public string Email { 
            get { 
                return _Email; 
            }
            
            set { 
                if (Tab_Contact.IsEmail(value)) 
                    _Email = value; 
            } 
        }

        public ulong Phone { get; set; }

        private string _KeyWord = null;
        public string KeyWord { 
            get { return _KeyWord; }
            set { 
                if (value != null) 
                    _KeyWord = value.ToLower();  //nur Kleinbuchstaben
            } 
        }

        public int MaxInactiveHours { get; set; } = -1;

        public Tab_Contact.Communication Via { get; set; } = Tab_Contact.Communication.NaN;
    }
}
