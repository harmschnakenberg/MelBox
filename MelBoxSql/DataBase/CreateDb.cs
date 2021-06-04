using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MelBoxSql
{
    public partial class Sql
    {
        private static bool isDatabaseExistent = false;

        public static bool CheckDb()
        {
            if (!isDatabaseExistent) Open();

            return isDatabaseExistent;
        }

        private static void Open()
        {
            #region Prüfe Datenbank-Datei 
            //Datenbak prüfen / erstellen
            if (!System.IO.File.Exists(DbPath))
            {
                CreateNewDataBase();
            }

            FileInfo dbFileInfo = new FileInfo(DbPath);

            if (IsFileLocked(dbFileInfo))
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\r\n\r\n*** ZUGRIFFSFEHLER ***\r\n\r\n" +
                    "Die Datenbankdatei\r\n" + DbPath +
                    "\r\nist durch ein anderes Programm blockiert.\r\n\r\n" +
                    "Das Programm wird beendet\r\n\r\n" +
                    "*** PROGRAMM WIRD BEENDET***");
                System.Threading.Thread.Sleep(10000);
                Environment.Exit(0);
            }
            else
            {
                isDatabaseExistent = true;
            }
            #endregion
        }


        static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
#pragma warning restore CA1031 // Do not catch general exception types

            //file is not locked
            return false;
        }

        private static void CreateNewDataBase()
        {
            Console.WriteLine("Erstelle eine neue Datenbank.");
            try
            {
                //Erstelle Datenbank-Datei und öffne einmal zum Testen
                Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
                FileStream stream = File.Create(DbPath);
                stream.Close();

                //Erzeuge Tabellen in neuer Datenbank-Datei
                //Zeiten in UTC im Format TEXT (Lesbarkeit Rohdaten)

                #region Log
                
                Tab_Log.CreateTable();

                Log log = new Log(Tab_Log.Topic.Startup, 3, "Datenbank neu erstellt.");
                
                Tab_Log.Insert(log);

                #endregion

                #region Company

                Tab_Company.CreateTable();

                Company company1 = new Company("Kreutzträger Kältetechnik GmbH & Co. KG", "Theodor-Barth-Str. 21", "28307 Bremen");
                
                Tab_Company.Insert(company1);

                #endregion

                #region Contact

                Tab_Contact.CreateTable();

                Contact contact = new Contact
                {
                    Id = 1,
                    Name = "SMSZentrale",
                    Password =  Tab_Contact.Encrypt("7307"),
                    Accesslevel = 9000,
                    CompanyId = 1,
                    Email = "smszentrale@kreutztraeger.de",
                    Phone = 4915142265412,
                    Via = Tab_Contact.Communication.Email,
                    MaxInactiveHours = 4
                };

                Tab_Contact.Insert(contact);

                contact = new Contact
                {
                    Id = 2,
                    Name = "Bereitschaftshandy",
                    Password = Tab_Contact.Encrypt("7307"),
                    Accesslevel = 2000,
                    CompanyId = 1,
                    Email = "bereitschaftshandy@kreutztraeger.de",
                    Phone = 491728362586,
                    Via = Tab_Contact.Communication.Sms                    
                };

                Tab_Contact.Insert(contact);

                contact = new Contact
                {
                    Id = 3,
                    Name = "Kreutzträger Service",
                    Password = Tab_Contact.Encrypt("7307"),
                    Accesslevel = 9000,
                    CompanyId = 1,
                    Email = "service@kreutztraeger.de",
                    Via = Tab_Contact.Communication.Email
                };

                Tab_Contact.Insert(contact);

                contact = new Contact
                {
                    Name = "Henry Kreutzträger",
                    Password = Tab_Contact.Encrypt("7307"),
                    Accesslevel = 9000,
                    CompanyId = 1,
                    Email = "henry.kreutztraeger@kreutztraeger.de",
                    Phone = 491727889419,
                    Via = Tab_Contact.Communication.Sms
                };

                Tab_Contact.Insert(contact);

                contact = new Contact
                {
                    Name = "Bernd Kreutzträger",
                    Password = Tab_Contact.Encrypt("7307"),
                    Accesslevel = 9000,
                    CompanyId = 1,
                    Email = "bernd.kreutztraeger@kreutztraeger.de",
                    Phone = 491727875067,
                    Via = Tab_Contact.Communication.Sms
                };

                Tab_Contact.Insert(contact);

                #endregion

                #region Message

                Tab_Message.CreateTable();

                Message message1 = new Message
                {
                    Content = "Datenbank neu erstellt.",
                    BlockedDays = Tab_Message.BlockWeek(),
                    StartBlockHour = 8,
                    EndBlockHour = 8
                };

                Tab_Message.Insert(message1);

                #endregion

                #region Recieved
                
                Tab_Recieved.CreateTable();

                Recieved recieved1 = new Recieved(1, 1)
                {
                    RecTime = DateTime.UtcNow
                };

                Tab_Recieved.Insert(recieved1);

                #endregion

                #region Sent

                Tab_Sent.CreateTable();

                Sent sent1 = new Sent(1, 1, Tab_Contact.Communication.Unknown)
                {
                    SentTime = DateTime.UtcNow
                };

                Tab_Sent.Insert(sent1);

                #endregion

                #region Bereitschaft

                Tab_Shift.CreateTable();

                Shift shift1 = new Shift(1, DateTime.Now);
                Tab_Shift.Insert(shift1);

                #endregion

                Views_Create();

                #region Hilfstabelle

                Dictionary<string, string> columns = new Dictionary<string, string>
                {
                    { "Id", "INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT" },
                    { "DischargeTime",  "TEXT" },
                    { "InternalReference", "INTEGER" }
                };

                Sql.CreateTable("Reports", columns);

                #endregion

            }
            catch (Exception ex)
            {
                throw new Exception("Sql-Fehler CreateNewDataBase()\r\n" + ex.Message + "\r\n" + ex.InnerException);
            }
        }


      
    }

}
