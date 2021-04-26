using System;
using System.Collections.Generic;
using System.Text;

namespace MelBoxWeb
{
    public partial class Html
    {
        /// <summary>
        /// 'Popup'-Nachrichten
        /// </summary>
        /// <param name="prio"></param>
        /// <param name="caption"></param>
        /// <param name="alarmText"></param>
        /// <returns></returns>
        public static string Alert(int prio, string caption, string alarmText)
        {
            StringBuilder builder = new StringBuilder();

            switch (prio)
            {
                case 1:
                    builder.Append("<div class='w3-panel w3-margin-left w3-pale-red w3-leftbar w3-border-red'>\n");
                    break;
                case 2:
                    builder.Append("<div class='w3-panel w3-margin-left w3-pale-yellow w3-leftbar w3-border-yellow'>\n");
                    break;
                case 3:
                    builder.Append("<div class='w3-panel w3-margin-left w3-pale-green w3-leftbar w3-border-green'>\n");
                    break;
                default:
                    builder.Append("<div class='w3-panel w3-margin-left w3-pale-blue w3-leftbar w3-border-blue'>\n");
                    break;
            }

            builder.Append(" <h3>" + caption + "</h3>\n");
            builder.Append(" <p>" + alarmText + "</p>\n");
            builder.Append("</div>\n");

            return builder.ToString();
        }

        internal static string FromTable(System.Data.DataTable dt, bool authorized = false, string root = "x")
        {
            string html = "<table class='w3-table-all w3-margin'>\n";
            //add header row
            html += "<tr>";
            
            if (authorized)
            {
                html += "<th>Edit</th>";
            }

            for (int i = 0; i < dt.Columns.Count; i++)
                html += "<th>" + dt.Columns[i].ColumnName + "</th>";


            html += "</tr>\n";

            //add rows
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                html += "<tr>";

                if (authorized)
                {
                    html += "<td>" +
                        "<a href='/" + root + "/" + dt.Rows[i][0].ToString() + "'><i class='material-icons-outlined'>build</i></a>" +
                        "</td>";
                }

                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    if (dt.Columns[j].ColumnName.Contains("Gesperrt"))
                    {
                        html += "<td>" + WeekDayCheckBox(
                            (MelBoxSql.Tab_Message.BlockedDays)int.Parse(
                                dt.Rows[i][j].ToString()
                                )
                            ) + "</td>";
                    }
                    else
                    {
                        html += "<td>" + dt.Rows[i][j].ToString() + "</td>";
                    }
                }
                html += "</tr>\n";
            }
            html += "</table>\n";
            return html;
        }

        internal static string WeekDayCheckBox(MelBoxSql.Tab_Message.BlockedDays blockedDays)
        {
            StringBuilder html = new StringBuilder("<span>");

            foreach (MelBoxSql.Tab_Message.BlockedDays day in Enum.GetValues(typeof(MelBoxSql.Tab_Message.BlockedDays)))
            {
                if (day == MelBoxSql.Tab_Message.BlockedDays.NaN || day == MelBoxSql.Tab_Message.BlockedDays.None) continue;

                string check = blockedDays.HasFlag(day) ? "checked" : string.Empty;
                html.Append($"<input name={day} class='w3-check' type='checkbox' {check}>" + Environment.NewLine);
                html.Append($"<label>{day} </label>");
            }

            return html.Append("</span>").ToString();
        }
    

    }
}
