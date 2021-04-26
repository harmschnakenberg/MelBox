﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MelBoxSql
{
    public partial class Sql    
    {
        public static System.Data.DataTable Recieved_View(DateTime start, DateTime end, string recFrom = "", string content = "")
        {
            string query = "SELECT Nr, datetime(Empfangen, 'localtime') AS Empfangen, Von, Inhalt FROM " + Recieved_ViewName +
                            " WHERE Empfangen BETWEEN '" + start.ToUniversalTime() + "' AND '" + end.ToUniversalTime() + "' ";

            if (recFrom.Length > 2) query += " AND Von LIKE '%" + recFrom + "%' ";
            if (content.Length > 2) query += " AND Inhalt LIKE '%" + content + "%' ";

            return Sql.SelectDataTable("Empfangen", query);
        }

        public static System.Data.DataTable Recieved_View(int id)
        {
            string query = "SELECT Nr, Von, Inhalt FROM " + Recieved_ViewName +
                            " WHERE Nr = " + id + ";" ;
;

            return Sql.SelectDataTable("Empfangen", query);
        }

        public static System.Data.DataTable Sent_View(DateTime start, DateTime end, string sentTo = "", string content = "", Tab_Contact.Communication via = Tab_Contact.Communication.NaN, Tab_Sent.Confirmation status = Tab_Sent.Confirmation.SentSuccessful)
        {
            string query = "SELECT Gesendet, An, Inhalt, Via, Sendestatus FROM " + Sent_ViewName +
                            " WHERE Gesendet BETWEEN '" + start.ToUniversalTime() + "' AND '" + end.ToUniversalTime() + "' ";

            if (sentTo.Length > 2) query += " AND Von LIKE '%" + sentTo + "%' ";
            if (content.Length > 2) query += " AND Inhalt LIKE '%" + content + "%' ";
            if (via != Tab_Contact.Communication.NaN) query += " AND Via = " + via + " ";
            if (status != Tab_Sent.Confirmation.SentSuccessful) query += " AND Sendestatus = " + status + " ";

            return Sql.SelectDataTable("Gesendet", query);
        }

        public static System.Data.DataTable Overdue_View()
        {
            string query = "SELECT * FROM " + Overdue_ViewName + " ORDER BY Fällig_seit ASC;";

            return Sql.SelectDataTable("Überfällig", query);
        }

        public static System.Data.DataTable Blocked_View(string content = "")
        {
            string query = "SELECT * FROM " + Blocked_ViewName;

            if (content.Length > 2) query += " WHERE Inhalt LIKE '%" + content + "%'";

            query += " ORDER BY Nachricht ASC;";

            return Sql.SelectDataTable("Weiterleitung Gesperrt", query);
        }

        public static System.Data.DataTable Shift_View(string name = "")
        {
            string query = "SELECT * FROM " + Blocked_ViewName;

            if (name.Length > 2) query += " WHERE Name LIKE '%" + name + "%'";

            query += " ORDER BY Datum ASC;";

            return Sql.SelectDataTable("Bereitschaft", query);
        }
    }
}