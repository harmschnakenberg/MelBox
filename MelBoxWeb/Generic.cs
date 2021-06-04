using System;
using System.Collections.Generic;
using System.Text;

namespace MelBoxWeb
{
    public static partial class Html
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
            string html = "<p><input oninput=\"w3.filterHTML('#table1', '.item', this.value)\" class='w3-input' placeholder='Suche nach..'></p>\r\n";

            html += "<table id='table1' class='w3-table-all'>\n";
            //add header row
            html += "<tr>";
            
            if (authorized)
            {
                html += "<th>Edit</th>";
            }

            // int rows = dt.Rows.Count;

            for (int i = 0; i < dt.Columns.Count; i++)
               // if (rows > 370) // Große Tabellen nicht sortierbar machen, da zu rechenintensiv!                
                    html += $"<th>" +
                            $"{dt.Columns[i].ColumnName.Replace('_', ' ')}" +
                            $"</th>";                
                //else                
                //    html += $"<th class='w3-hover-sand' onclick=\"w3.sortHTML('#table1', '.item', 'td:nth-child({ i + 1 })')\">" +
                //            $"{dt.Columns[i].ColumnName.Replace('_', ' ')}" +
                //            $"</th>";
                
              html += "</tr>\n";

            //add rows
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                html += "<tr class='item'>";

                if (authorized)
                {
                    html += "<td>" +
                        "<a href='/" + root + "/" + dt.Rows[i][0].ToString() + "'><i class='material-icons-outlined'>build</i></a>" +
                        "</td>";
                }

                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    if (dt.Columns[j].ColumnName.StartsWith("Gesperrt"))
                    {
                        html += "<td>" + WeekDayCheckBox(
                            (MelBoxSql.Tab_Message.BlockedDays)int.Parse(
                                dt.Rows[i][j].ToString()
                                )
                            ) + "</td>";
                    }
                    else if (dt.Columns[j].ColumnName.StartsWith("Via"))
                    {
                        if (int.TryParse(dt.Rows[i][j].ToString(), out int via))
                        {                           
                            bool phone = 0 != (via & (int)MelBoxSql.Tab_Contact.Communication.Sms);
                            bool email = 0 != (via & (int)MelBoxSql.Tab_Contact.Communication.Email);

                            html += "<td>";
                            if (phone) html += "<span class='material-icons-outlined'>smartphone</span>";
                            if (email) html += "<span class='material-icons-outlined'>email</span>";
                            html += "</td>";
                        }
                    }
                    else if (dt.Columns[j].ColumnName.Contains("Sendestatus"))
                    {
                       
                        html += "<td><span class='material-icons-outlined'>";

                        if (int.TryParse(dt.Rows[i][j].ToString(), out int confirmation))
                        {                            
                            switch ((MelBoxSql.Tab_Sent.Confirmation) confirmation)
                            {
                                case MelBoxSql.Tab_Sent.Confirmation.NaN:
                                case MelBoxSql.Tab_Sent.Confirmation.AbortedSending:
                                    html += "sms_failed";
                                    break;
                                case MelBoxSql.Tab_Sent.Confirmation.Unknown:
                                    html += "sms";
                                    break;
                                case MelBoxSql.Tab_Sent.Confirmation.AwaitingRefernece:
                                    html += "hourglass_top";
                                    break;
                                case MelBoxSql.Tab_Sent.Confirmation.PendingAnswer:
                                    html += "hourglass_bottom";
                                    break;
                                case MelBoxSql.Tab_Sent.Confirmation.RetrySending:
                                    html += "try";
                                    break; 
                                case MelBoxSql.Tab_Sent.Confirmation.SentSuccessful:
                                    html += "check";
                                    break;
                                default:
                                    html += "device_unknown";
                                    break;
                            }                            
                        }                        
                        else
                        {
                            html += "error";
                        }

                        html += "</span></td>";
                    }
                    else if (dt.Columns[j].ColumnName.StartsWith("Topic"))
                    {
                        if(int.TryParse(dt.Rows[i][j].ToString(), out int topicNo))
                        {
                            html += "<td>" + ((MelBoxSql.Tab_Log.Topic) topicNo).ToString() +"</td>";
                        }
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

        internal static string DropdownExplanation()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<div class='w3-dropdown-hover w3-right'>");
            sb.AppendLine(" <button class='w3-button w3-white'>Legende</button>");
            sb.AppendLine(" <div class='w3-dropdown-content w3-bar-block w3-border' style='right:0;width:300px;'>");
            sb.AppendLine("  <div class='w3-bar-item w3-button'><span class='material-icons-outlined'>smartphone</span>SMS versendet</div>");
            sb.AppendLine("  <div class='w3-bar-item w3-button'><span class='material-icons-outlined'>email</span>E-Mail versendet</div>");
            sb.AppendLine("  <hr>");
            sb.AppendLine("  <div class='w3-bar-item w3-button'><span class='material-icons-outlined'>check</span>Versand bestätigt</div>");
            sb.AppendLine("  <div class='w3-bar-item w3-button'><span class='material-icons-outlined'>hourglass_top</span>erwarte interne Zuweisung</div>");
            sb.AppendLine("  <div class='w3-bar-item w3-button'><span class='material-icons-outlined'>hourglass_bottom</span>erwarte externe Bestätigung</div>");
            sb.AppendLine("  <div class='w3-bar-item w3-button'><span class='material-icons-outlined'>try</span>erneuter Sendeversuch</div>");
            sb.AppendLine("  <div class='w3-bar-item w3-button'><span class='material-icons-outlined'>sms_failed</span>Senden abgebrochen</div>");
            sb.AppendLine("  <div class='w3-bar-item w3-button'><span class='material-icons-outlined'>sms</span>Status unbekannt</div>");
            sb.AppendLine("  <div class='w3-bar-item w3-button'><span class='material-icons-outlined'>device_unknown</span>keine Zuweisung</div>");
            sb.AppendLine("  <div class='w3-bar-item w3-button'><span class='material-icons-outlined'>error</span>fehlerhafte Zuweisung</div>");
            sb.AppendLine(" </div>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        internal static string FromShiftTable(System.Data.DataTable dt, MelBoxSql.Contact user)
        {
            if (user == null) return string.Empty;

            string html = "<p><input oninput=\"w3.filterHTML('#table1', '.item', this.value)\" class='w3-input' placeholder='Suche nach..'></p>\r\n";

            html += "<table class='w3-table w3-bordered'>\n";
            //add header row
            html += "<tr>";

            if (user.Id > 0)
            {
                html += "<th>Edit</th>";
            }

            //for (int i = 0; i < dt.Columns.Count; i++)
                html += "<th>Nr</th><th>Name</th><th>Via</th><th>Tag</th><th>Datum</th><th>Zeitraum</th>";

            html += "</tr>\n";

            List<DateTime> holydays = MelBoxSql.Tab_Shift.Holydays(DateTime.Now);

            //add rows
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int.TryParse(dt.Rows[i][0].ToString(), out int shiftId);
                int.TryParse(dt.Rows[i][1].ToString(), out int shiftContactId);
                string contactName = dt.Rows[i][2].ToString();
                int.TryParse(dt.Rows[i][3].ToString(), out int via);
                string day = dt.Rows[i][4].ToString();
                DateTime.TryParse(dt.Rows[i][5].ToString(), out DateTime date);
                int.TryParse(dt.Rows[i][6].ToString(), out int start);
                int.TryParse(dt.Rows[i][7].ToString(), out int end);

                if (holydays.Contains(date)) //Feiertag?
                    html += "<tr class='item w3-pale-red'>";                
                else if (day == "Sa" || day == "So") //Wochenende ?              
                    html += "<tr class='item w3-sand'>";
                else                
                    html += "<tr class='item'>";
                
                #region Editier-Button

                if (user.Accesslevel >= Server.Level_Admin || (user.Id == shiftContactId && user.Accesslevel >= Server.Level_Reciever) || shiftId == 0 )
                {
                    string route = shiftId == 0 ? date.ToShortDateString() : shiftId.ToString();

                    html += "<td>" +
                        "<a href='/shift/" + route + "'><i class='material-icons-outlined'>build</i></a>" +
                        "</td>";
                }
                else
                {
                    html += "<td>&nbsp;</td>";
                }
                #endregion

                #region Bereitschafts-Id
                html += "<td>" + shiftId + "</td>";
                #endregion

                #region Name
                html += "<td>" + contactName + "</td>";
                #endregion

                #region Sendeweg
                bool phone = 0 != (via & (int)MelBoxSql.Tab_Contact.Communication.Sms);
                bool email = 0 != (via & (int)MelBoxSql.Tab_Contact.Communication.Email);

                html += "<td>";
                if (phone) html += "<span class='material-icons-outlined'>smartphone</span>";
                if (email) html += "<span class='material-icons-outlined'>email</span>";
                html += "</td>";
                #endregion

                #region Tag
                html += "<td>" + day + "</td>";
                #endregion

                #region Datum
                html += "<td>" + date.ToShortDateString() + "</td>";
                #endregion

                #region Beginn
                double s = Math.Round(start / 0.48);
                double e = Math.Round(end / 0.48);

                if (s + e == 0) s = 50;
                string sHour = (start > 0) ? start + "&nbsp;Uhr" : string.Empty;
                string eHour = (end > 0) ? end + "&nbsp;Uhr" : string.Empty;

                html += "<td>" +
                        "<div class='w3-row w3-pale-blue' style='min-width:240px'>" +
                        $"  <div class='w3-col w3-right-align' style='width:{s - 1}%'>{sHour}&nbsp;</div>" +
                        $"  <div class='w3-col w3-teal' style='width:{50 - s}%'>&nbsp;</div>" +
                        $"  <div class='w3-col w3-teal w3-border-left' style='width:{e}%'>&nbsp;</div>" +
                        $"  <div class='w3-col w3-left-align' style='width:{50 - e}%'>&nbsp;{eHour}</div>" +
                        " </div>" +
                        "</td>";
                # endregion

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
                html.Append($"<input name={day} class='w3-check' type='checkbox' {check} disabled>" + Environment.NewLine);
                html.Append($"<label>{day} </label>");
            }

            return html.Append("</span>").ToString();
        }
    
        internal static string ButtonNew(string root)
        {
            return $"<button class='w3-button w3-block w3-blue w3-section w3-padding w3-col w3-quarter type='submit' formaction='/{root}/new'>Neu</button>";
        }

    }
}
