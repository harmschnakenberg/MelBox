using System;
using System.Collections.Generic;
using System.Text;

namespace MelBoxSql
{
    public static partial class Sql
    {
        const string Recieved_ViewName = "ViewRecieved";
        const string Sent_ViewName = "ViewSent";
        const string Overdue_ViewName = "ViewOverdue";
        const string Blocked_ViewName = "ViewBlocked";
        const string Shift_ViewName = "ViewShift";

        internal static void Views_Create()
        {
            #region Views

            //Hilfs-VIEW: Listet alle Tagesdaten mit Wochentag von Heute bis in einem Jahr - verwendet bei Berietschafts-VIEW 
            const string query1 = "CREATE VIEW ViewYearFromToday AS " +
                                   "SELECT CASE(CAST(strftime('%w', d) AS INT) +6) % 7 WHEN 0 THEN 'Mo' WHEN 1 THEN 'Di' WHEN 2 THEN 'Mi' WHEN 3 THEN 'Do' WHEN 4 THEN 'Fr' WHEN 5 THEN 'Sa' ELSE 'So' END AS Tag, d FROM(WITH RECURSIVE dates(d) AS(VALUES(date('now')) " +
                                   "UNION ALL " +
                                   "SELECT date(d, '+1 day') FROM dates WHERE d<date('now', '+1 year')) SELECT d FROM dates ) WHERE d NOT IN(SELECT date(Start) FROM Shift WHERE date(Start) >= date('now') ); ";
            NonQuery(query1);

            const string query2 = "CREATE VIEW " + Recieved_ViewName + " AS " +
                                    "SELECT r.Id As Nr, strftime('%Y-%m-%d %H:%M:%S', RecTime) AS Empfangen, c.Name AS Von, (SELECT Content FROM Message WHERE Id = r.ContentId) AS Inhalt " +
                                    "FROM Recieved AS r " +
                                    "JOIN Contact AS c " +
                                    "ON FromId = c.Id; ";
            Sql.NonQuery(query2);

            const string query3 = "CREATE VIEW " + Sent_ViewName + " AS SELECT strftime('%Y-%m-%d %H:%M:%S',SentTime) AS Gesendet, c.name AS An, Content AS Inhalt, Via AS Via, Confirmation AS Sendestatus " +
                                    "FROM Sent AS ls JOIN Contact AS c ON ToId = c.Id JOIN Message AS mc ON mc.id = ls.ContentId; ";
            Sql.NonQuery(query3);

            const string query4 = "CREATE VIEW " + Overdue_ViewName + " AS " +
                                    "SELECT FromId AS Id, Contact.Name, Company.Name AS Firma, MaxInactiveHours || ' Std.' AS Max_Inaktiv, strftime('%Y-%m-%d %H:%M:%S',RecTime) AS Letzte_Nachricht, Content AS Inhalt, " +
                                    "CAST( (strftime('%s', 'now') - strftime('%s', RecTime, '+' || MaxInactiveHours || ' hours')) / 3600 AS INTEGER) || ' Std.' AS Fällig_seit " +
                                    "FROM Recieved " +
                                    "JOIN Contact ON Contact.Id = Recieved.FromId " +
                                    "JOIN Company ON Company.Id = Contact.CompanyId " +
                                    "JOIN Message ON Message.Id = ContentId WHERE MaxInactiveHours > 0 AND DATETIME(RecTime, '+' || MaxInactiveHours || ' hours') < Datetime('now') " +
                                    "ORDER BY RecTime DESC; ";
            Sql.NonQuery(query4);

            const string query5 = "CREATE VIEW " + Blocked_ViewName + " AS " +
                                    "SELECT Message.Id AS Id, Content As Nachricht, StartBlockHour || ' Uhr' As Beginn, EndBlockHour || ' Uhr' As Ende, " +
                                    "BlockedDays As Gesperrt " +
                                    //"(SELECT BlockedDays & 1 > 0) AS Mo, (SELECT BlockedDays & 2 > 0) AS Di, (SELECT BlockedDays & 4 > 0) AS Mi, (SELECT BlockedDays & 8 > 0) AS Do, (SELECT BlockedDays & 16 > 0) AS Fr, (SELECT BlockedDays & 32 > 0) AS Sa, (SELECT BlockedDays & 64 > 0) AS So " +
                                    "FROM Message WHERE BlockedDays > 0";
            Sql.NonQuery(query5);

            const string query6 = "CREATE VIEW " + Shift_ViewName + " AS " +
                                    "SELECT Shift.Id AS Nr, Contact.Id AS ContactId, Contact.Name AS Name, Via, CASE(CAST(strftime('%w', Start) AS INT) + 6) % 7 WHEN 0 THEN 'Mo' WHEN 1 THEN 'Di' WHEN 2 THEN 'Mi' WHEN 3 THEN 'Do' WHEN 4 THEN 'Fr' WHEN 5 THEN 'Sa' ELSE 'So' END AS Tag, date(Start) AS Datum, CAST(strftime('%H', Start, 'localtime') AS INTEGER) AS Beginn, CAST(strftime('%H', End, 'localtime') AS INTEGER) AS Ende " +
                                    "FROM Shift JOIN Contact ON ContactId = Contact.Id WHERE Datum >= date('now', '-1 day') " +
                                    "UNION " +
                                    "SELECT NULL AS Nr, NULL AS ContactId, NULL AS Name, 0 AS Via, CASE(CAST(strftime('%w', d) AS INT) + 6) % 7 WHEN 0 THEN 'Mo' WHEN 1 THEN 'Di' WHEN 2 THEN 'Mi' WHEN 3 THEN 'Do' WHEN 4 THEN 'Fr' WHEN 5 THEN 'Sa' ELSE 'So' END AS Tag, d AS Datum, NULL AS Beginn, NULL AS Ende " +
                                    "FROM ViewYearFromToday WHERE Datum >= date('now', '-1 day') " +
                                    "ORDER BY Datum; ";
            Sql.NonQuery(query6);

            #endregion
        }
    }

}
