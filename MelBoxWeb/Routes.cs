using Grapevine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MelBoxSql;
using System.Data;

namespace MelBoxWeb
{
    [RestResource]
    public class MelBoxRoutes
    {
        [RestRoute("Get", "/api/test")]
        public async Task Test(IHttpContext context)
        {
            await context.Response.SendResponseAsync("Successfully hit the test route!").ConfigureAwait(false);
        }

        [RestRoute("Get", "/gsm/quality")]
        public async Task GsmSignalQuality(IHttpContext context)
        {
            StringBuilder sb = new StringBuilder("{");
            sb.Append("\"" + nameof(GsmStatus.SignalQuality) + "\":" + GsmStatus.SignalQuality + ",");
            sb.Append("\"" + nameof(GsmStatus.SignalErrorRate) + "\":\"" + GsmStatus.SignalErrorRate + "\",");
            sb.Append("\"" + nameof(GsmStatus.NetworkRegistration) + "\":\"" + GsmStatus.NetworkRegistration + "\"");
            sb.Append("}");

            await context.Response.SendResponseAsync(sb.ToString()).ConfigureAwait(false);
        }


        #region Nachrichten
        [RestRoute("Get", "/in")]
        public async Task InBox(IHttpContext context)
        {
            //Später per URL Beginn und Ende definierbar?

            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            //System.Data.DataTable rec = MelBoxSql.Sql.Recieved_View(DateTime.UtcNow.AddDays(-14), DateTime.UtcNow);
            System.Data.DataTable rec = MelBoxSql.Sql.Recieved_View_Last(1000);
            string table = Html.FromTable(rec, guid != null, "blocked");

            await Server.PageAsync(context, "Eingang", table);
        }

        [RestRoute("Get", "/out")]
        public async Task OutBox(IHttpContext context)
        {
            //System.Data.DataTable sent = MelBoxSql.Sql.Sent_View(DateTime.UtcNow.AddDays(-14), DateTime.UtcNow);
            System.Data.DataTable sent = MelBoxSql.Sql.Sent_View_Last(1000);
            string table = Html.FromTable(sent);

            await Server.PageAsync(context, "Ausgang", table);
        }

        [RestRoute("Get", "/overdue")]
        public async Task OverdueShow(IHttpContext context)
        {
            System.Data.DataTable overdue = MelBoxSql.Sql.Overdue_View();

            string html;
            if (overdue.Rows.Count == 0)
            {
                html = Html.Alert(3, "Keine Zeitüberschreitung", "Kein überwachter Sender ist überfällig.");
                DataTable dt = MelBoxSql.Tab_Contact.SelectWatchedContactList();
                html += Html.FromTable(dt);
            }
            else
            {
                html = Html.FromTable(overdue);
            }

            await Server.PageAsync(context, "Überfällige Rückmeldungen", html);
        }
        #endregion


        #region Bereitschaftszeiten
        [RestRoute("Get", "/shift")]
        public async Task ShiftShow(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            if (!Server.LogedInHash.TryGetValue(guid, out Contact user))
            {
                await Home(context);
                return;
            }

            DataTable dt = Sql.Shift_View();
            string table = Html.FromShiftTable(dt, user);

            await Server.PageAsync(context, "Bereitschaftsdienste", table);
        }

        [RestRoute("Get", "/shift/{shiftId:num}")]
        public async Task ShiftFromId(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);
            bool isAdmin = false;

            if (Server.LogedInHash.TryGetValue(guid, out Contact user))
            {
                isAdmin = user.Accesslevel >= Server.Level_Admin;
            }

            var shiftIdStr = context.Request.PathParameters["shiftId"];
            int.TryParse(shiftIdStr, out int shiftId);

            Shift shift = Tab_Shift.Select(shiftId);

            string contactOptions = Tab_Contact.HtmlOptionContacts(Server.Level_Reciever, user.Id, isAdmin);

            Dictionary<string, string> pairs = new Dictionary<string, string>
            {
                { "##Id##", shift.Id.ToString() },
                { "##ContactOptions##", contactOptions },
                { "##MinDate##", DateTime.UtcNow.Date.ToString("yyyy-MM-dd") },
                { "##StartDate##", shift.Start.ToString("yyyy-MM-dd") },
                { "##EndDate##", shift.End.ToString("yyyy-MM-dd") },
                { "##StartOptions##", Tab_Shift.HtmlOptionHour(shift.Start.Hour) },
                { "##EndOptions##", Tab_Shift.HtmlOptionHour(shift.End.Hour) },
                { "##Route##", "update" }
            };

            string form = Server.Page(Server.Html_FormShift, pairs);

            DataTable dt = Sql.Shift_View();
            string table = Html.FromShiftTable(dt, user);

            await Server.PageAsync(context, "Bereitschaftsdienst", table + form);
        }

        [RestRoute("Get", "/shift/{shiftDate}")]
        public async Task ShiftFromDate(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);
           
            if (!Server.LogedInHash.TryGetValue(guid, out Contact user))
            {
                await Home(context);
                return;
            }

            bool isAdmin = user.Accesslevel >= Server.Level_Admin;

            var shiftDateStr = context.Request.PathParameters["shiftDate"];
            DateTime.TryParse(shiftDateStr, out DateTime shiftDate);

            Shift shift = new Shift(user.Id, shiftDate);

            string contactOptions = Tab_Contact.HtmlOptionContacts(Server.Level_Reciever, user.Id, isAdmin);

            DateTime endDate = shiftDate.DayOfWeek == DayOfWeek.Monday ? shiftDate.Date.AddDays(7) : shiftDate.Date.AddDays(1);

            Dictionary<string, string> pairs = new Dictionary<string, string>
            {
                //pairs.Add("##readonly##", isAdmin ? string.Empty : "readonly");
                //pairs.Add("##disabled##", isAdmin ? string.Empty : "disabled");
                { "##Id##", string.Empty },
                // pairs.Add("##ContactId##", contact.Id.ToString());
                //pairs.Add("##Name##", contact.Name);
                { "##ContactOptions##", contactOptions },
                { "##MinDate##", DateTime.UtcNow.Date.ToString("yyyy-MM-dd") },
                { "##StartDate##", shiftDate.Date.ToString("yyyy-MM-dd") },
                { "##EndDate##", endDate.ToString("yyyy-MM-dd") },
                { "##StartOptions##", Tab_Shift.HtmlOptionHour(shift.Start.Hour) },
                { "##EndOptions##", Tab_Shift.HtmlOptionHour(shift.End.Hour) },
                { "##Route##", "new" }
            };

            string form = Server.Page(Server.Html_FormShift, pairs);

            DataTable dt = Sql.Shift_View();
            string table = Html.FromShiftTable(dt, user);

            await Server.PageAsync(context, "Bereitschaftsdienst", table + form);
        }

        [RestRoute("Post", "/shift/new")]
        public async Task ShiftCreate(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            if (!Server.LogedInHash.TryGetValue(guid, out Contact user))
            {
                await Home(context);
                return;
            }

            #region Form auslesen
            Dictionary<string, string> payload = Server.Payload(context);
            payload.TryGetValue("ContactId", out string contactIdStr);
            payload.TryGetValue("StartDate", out string startDateStr);
            payload.TryGetValue("EndDate", out string endDateStr);
            payload.TryGetValue("StartTime", out string startTimeStr);
            payload.TryGetValue("EndTime", out string endTimeStr);

            DateTime start = DateTime.UtcNow;
            DateTime end = DateTime.UtcNow.AddDays(1);

            if (!int.TryParse(contactIdStr, out int shiftContactId))
                shiftContactId = user.Id;

            if (DateTime.TryParse(startDateStr, out DateTime startDate))
                if (int.TryParse(startTimeStr, out int startHour))
                    start = startDate.Date.AddHours(startHour); //Lokale Zeit!

            if (DateTime.TryParse(endDateStr, out DateTime endDate))
                if (int.TryParse(endTimeStr, out int endHour))
                    end = endDate.Date.AddHours(endHour); //Lokale Zeit!          
            #endregion

            bool success = MelBoxSql.Tab_Shift.Insert(shiftContactId, start, end);
            string alert;

            if (success)
                alert = Html.Alert(3, "Neue Bereitschaft gespeichert", $"Neue Bereitschaft vom {start.ToShortDateString()} bis {end.ToShortDateString()} wurde erfolgreich erstellt.");
            else
                alert = Html.Alert(1, "Fehler beim speichern der Bereitschaft", "Die Bereitschaft konnte nicht in der Datenbank gespeichert werden.");

            DataTable dt = Sql.Shift_View();
            string table = Html.FromShiftTable(dt, user);

            await Server.PageAsync(context, "Bereitschaftszeit erstellen", alert + table);
        }

        [RestRoute("Post", "/shift/update")]
        public async Task ShiftUpdate(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            if (!Server.LogedInHash.TryGetValue(guid, out Contact user))
            {
                await Home(context);
                return;
            }

            #region Form auslesen
            Dictionary<string, string> payload = Server.Payload(context);
            payload.TryGetValue("Id", out string shiftIdStr);
            payload.TryGetValue("ContactId", out string contactIdStr);
            payload.TryGetValue("StartDate", out string startDateStr);
            payload.TryGetValue("EndDate", out string endDateStr);
            payload.TryGetValue("StartTime", out string startTimeStr);
            payload.TryGetValue("EndTime", out string endTimeStr);

            DateTime start = DateTime.UtcNow;
            DateTime end = DateTime.UtcNow.AddDays(1);

            int.TryParse(shiftIdStr, out int shiftId);

            if (!int.TryParse(contactIdStr, out int shiftContactId))
                shiftContactId = user.Id;

            if (DateTime.TryParse(startDateStr, out DateTime startDate))
                if (int.TryParse(startTimeStr, out int startHour))
                    start = startDate.Date.AddHours(startHour); //Lokale Zeit!

            if (DateTime.TryParse(endDateStr, out DateTime endDate))
                if (int.TryParse(endTimeStr, out int endHour))
                    end = endDate.Date.AddHours(endHour); //Lokale Zeit!          
            #endregion

            Shift set = new Shift(shiftContactId, start.ToUniversalTime(), end.ToUniversalTime());
            Shift where = new Shift() { Id = shiftId };

            bool success = MelBoxSql.Tab_Shift.Update(set, where);
            string alert;

            if (success)
                alert = Html.Alert(3, "Bereitschaft geändert", $"Die Bereitschaft Nr. {shiftId} von {start.ToShortDateString()} bis {end.ToShortDateString()}  wurde erfolgreich geändert.");
            else
                alert = Html.Alert(1, "Fehler beim Ändern der Bereitschaft", "Die Bereitschaft konnte nicht in der Datenbank gespeichert werden.");

            DataTable dt = Sql.Shift_View();
            string table = Html.FromShiftTable(dt, user);

            await Server.PageAsync(context, "Bereitschaftszeit geändert", alert + table);
        }
        #endregion


        #region Gesperrte Nachrichten
        [RestRoute("Get", "/blocked/{recId:num}")]
        public async Task InBoxBlock(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            if (guid.Length == 0)
            {
                await Home(context);
                return;
            }

            var recIdStr = context.Request.PathParameters["recId"];
            int.TryParse(recIdStr, out int recId);

            MelBoxSql.Recieved recieved = MelBoxSql.Tab_Recieved.SelectRecieved(recId);
            MelBoxSql.Contact contact = MelBoxSql.Tab_Contact.SelectContact(recieved.FromId);
            MelBoxSql.Company company = MelBoxSql.Tab_Company.SelectCompany(contact.CompanyId);
            MelBoxSql.Message message = MelBoxSql.Tab_Message.SelectMessage(recieved.ContentId);

            bool mo = message.BlockedDays.HasFlag(MelBoxSql.Tab_Message.BlockedDays.Mo);
            bool tu = message.BlockedDays.HasFlag(MelBoxSql.Tab_Message.BlockedDays.Di);
            bool we = message.BlockedDays.HasFlag(MelBoxSql.Tab_Message.BlockedDays.Mi);
            bool th = message.BlockedDays.HasFlag(MelBoxSql.Tab_Message.BlockedDays.Do);
            bool fr = message.BlockedDays.HasFlag(MelBoxSql.Tab_Message.BlockedDays.Fr);
            bool sa = message.BlockedDays.HasFlag(MelBoxSql.Tab_Message.BlockedDays.Sa);
            bool su = message.BlockedDays.HasFlag(MelBoxSql.Tab_Message.BlockedDays.So);

            Dictionary<string, string> pairs = new Dictionary<string, string>
            {
                { "##MsgId##", message.Id.ToString() },
                { "##From##", contact.Name + " (" + company.Name + ")" },
                { "##Message##", message.Content },
                { "##Mo##", mo ? "checked" : string.Empty },
                { "##Tu##", tu ? "checked" : string.Empty },
                { "##We##", we ? "checked" : string.Empty },
                { "##Th##", th ? "checked" : string.Empty },
                { "##Fr##", fr ? "checked" : string.Empty },
                { "##Sa##", sa ? "checked" : string.Empty },
                { "##Su##", su ? "checked" : string.Empty },
                { "##Start##", message.StartBlockHour.ToString() },
                { "##End##", message.EndBlockHour.ToString() }
            };

            string form = Server.Page(Server.Html_FormMessage, pairs);

            await Server.PageAsync(context, "Eingang", form);
        }


        [RestRoute("Get", "/blocked")]
        public async Task BlockedMessage(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            System.Data.DataTable blocked = MelBoxSql.Sql.Blocked_View();
            string table = Html.FromTable(blocked, guid != null, "blocked");

            await Server.PageAsync(context, "Gesperrte Nachrichten", table);
        }


        [RestRoute("Post", "/blocked/update")]
        public async Task BlockedMessageUpdate(IHttpContext context)
        {
            Dictionary<string, string> payload = Server.Payload(context);

            payload.TryGetValue("MsgId", out string idStr);
            payload.TryGetValue("Message", out string message);
            payload.TryGetValue("Mo", out string mo);
            payload.TryGetValue("Tu", out string tu);
            payload.TryGetValue("We", out string we);
            payload.TryGetValue("Th", out string th);
            payload.TryGetValue("Fr", out string fr);
            payload.TryGetValue("Sa", out string sa);
            payload.TryGetValue("Su", out string su);
            payload.TryGetValue("Start", out string startStr);
            payload.TryGetValue("End", out string endStr);

            int.TryParse(idStr, out int id);
            int.TryParse(startStr, out int startHour);
            int.TryParse(endStr, out int endHour);

            MelBoxSql.Tab_Message.BlockedDays blockedDays = Tab_Message.BlockedDays.None;
            if (mo != null) blockedDays |= MelBoxSql.Tab_Message.BlockedDays.Mo;
            if (tu != null) blockedDays |= MelBoxSql.Tab_Message.BlockedDays.Di;
            if (we != null) blockedDays |= MelBoxSql.Tab_Message.BlockedDays.Mi;
            if (th != null) blockedDays |= MelBoxSql.Tab_Message.BlockedDays.Do;
            if (fr != null) blockedDays |= MelBoxSql.Tab_Message.BlockedDays.Fr;
            if (sa != null) blockedDays |= MelBoxSql.Tab_Message.BlockedDays.Sa;
            if (su != null) blockedDays |= MelBoxSql.Tab_Message.BlockedDays.So;

            Message set = new Message
            {
                BlockedDays = blockedDays,
                StartBlockHour = startHour,
                EndBlockHour = endHour
            };

            string alert;
            if (!Tab_Message.Update(set, id))
                alert = Html.Alert(1, "Nachricht aktualisieren fehlgeschlagen", $"Die Nachricht {idStr}<p/>{message}<p/>konnte nicht geändert werden.");
            else
                alert = Html.Alert(2, "Nachricht aktualisiert", $"Änderungen für die Nachricht {idStr}<p/>{message}<p/>gespeichert.");

            System.Data.DataTable sent = MelBoxSql.Sql.Blocked_View();
            string table = Html.FromTable(sent);

            await Server.PageAsync(context, "Nachricht aktualisiert", alert + table);
        }
        #endregion


        #region Benutzerkonto
        [RestRoute("Get", "/account")]
        [RestRoute("Get", "/account/{id:num}")]
        public async Task AccountShow(IHttpContext context)
        {
            #region Anfragenden Benutzer identifizieren
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            if (!Server.LogedInHash.TryGetValue(guid, out Contact user))
            {
                await Home(context);
                return;
            }

            bool isAdmin = user.Accesslevel >= Server.Level_Admin;
            DataTable dt = Tab_Contact.SelectContactList(user.Accesslevel, isAdmin ? 0 : user.Id);
            #endregion

            #region Anzuzeigenden Benutzer 
            int showId = user.Id;

            if (context.Request.PathParameters.TryGetValue("id", out string idStr))
            {
                int.TryParse(idStr, out showId);
            }

            Contact account = MelBoxSql.Tab_Contact.SelectContact(showId);
            Company company = MelBoxSql.Tab_Company.SelectCompany(account.CompanyId);
            #endregion

            Dictionary<string, string> pairs = new Dictionary<string, string>
            {
                { "##readonly##", isAdmin ? string.Empty : "readonly" },
                { "##disabled##", isAdmin ? string.Empty : "disabled" },
                { "##Id##", account.Id.ToString() },
                { "##Name##", account.Name },
                { "##Accesslevel##", account.Accesslevel.ToString() },
                { "##UserAccesslevel##", user.Accesslevel.ToString() },
                { "##CompanyId##", account.CompanyId.ToString() },
                { "##CompanyName##", company.Name },
                { "##CompanyCity##", System.Text.RegularExpressions.Regex.Replace(company.City, @"\d", "") },
                { "##viaEmail##", account.Via.HasFlag(Tab_Contact.Communication.Email) ? "checked" : string.Empty },
                { "##Email##", account.Email },
                { "##viaPhone##", account.Via.HasFlag(Tab_Contact.Communication.Sms) ? "checked" : string.Empty },
                { "##Phone##", "+" + account.Phone.ToString() },
                { "##MaxInactiveHours##", account.MaxInactiveHours.ToString() },
                { "##KeyWord##", account.KeyWord },
                { "##CompanyList##", isAdmin ? Tab_Company.SelectCompanyAllToHtmlOption(account.CompanyId) : string.Empty },

                { "##NewContact##", isAdmin ? Html.ButtonNew("account") : string.Empty }
            };

            string form = Server.Page(Server.Html_FormAccount, pairs);
            string tabel = Html.FromTable(dt, true, "account");

            await Server.PageAsync(context, "Benutzerkonto", tabel + form);
        }

        [RestRoute("Post", "/account/new")]
        public async Task AccountCreate(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            if (!Server.LogedInHash.TryGetValue(guid, out Contact user) || user.Accesslevel < Server.Level_Admin)
            {
                await Home(context);
                return;
            }

            #region Form auslesen
            Dictionary<string, string> payload = Server.Payload(context);
            //payload.TryGetValue("Id",out string idStr); //Wird automatisch vergeben
            payload.TryGetValue("name", out string name);
            payload.TryGetValue("password", out string password);
            payload.TryGetValue("CompanyId", out string CompanyIdStr);
            payload.TryGetValue("viaEmail", out string viaEmail);
            payload.TryGetValue("email", out string email);
            payload.TryGetValue("viaPhone", out string viaPhone);
            payload.TryGetValue("phone", out string phoneStr);
            //KeyWord bei Neuanlage nicht vergebbar
            payload.TryGetValue("MaxInactiveHours", out string maxInactiveHoursStr);
            payload.TryGetValue("Accesslevel", out string accesslevelStr);
            #endregion

            #region Kontakt erstellen
            Contact contact = new Contact
            {
                Name = name,
                EntryTime = DateTime.UtcNow,
                Password = Tab_Contact.Encrypt(password),
                Email = email,
            };

            if (int.TryParse(CompanyIdStr, out int companyId))
            {
                contact.CompanyId = companyId;
            }

            if (int.TryParse(maxInactiveHoursStr, out int maxInactiveHours))
            {
                contact.MaxInactiveHours = maxInactiveHours;
            }

            if (int.TryParse(accesslevelStr, out int accesslevel))
            {
                contact.Accesslevel = accesslevel;
            }

            if (ulong.TryParse(phoneStr, out ulong phone))
            {
                contact.Phone = phone;
            }

            contact.Via = Tab_Contact.Communication.Unknown;

            if (viaEmail != null) contact.Via |= Tab_Contact.Communication.Email;
            if (viaPhone != null) contact.Via |= Tab_Contact.Communication.Sms;
            #endregion

            bool success = MelBoxSql.Tab_Contact.Insert(contact);
            string alert;

            if (success)
                alert = Html.Alert(3, "Neuen Kontakt gespeichert", "Der Kontakt " + name + " wurde erfolgreich neu erstellt.");
            else
                alert = Html.Alert(1, "Fehler beim speichern des Kontakts", "Der Kontakt " + name + " konnte nicht in der Datenbank gespeichert werden.");

            await Server.PageAsync(context, "Benutzerkonto erstellen", alert);
        }

        [RestRoute("Post", "/account/update")]
        public async Task AccountUpdate(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            if (!Server.LogedInHash.TryGetValue(guid, out Contact user))
            {
                await Home(context);
                return;
            }

            #region Form auslesen
            Dictionary<string, string> payload = Server.Payload(context);
            payload.TryGetValue("Id", out string idStr);
            payload.TryGetValue("name", out string name);
            payload.TryGetValue("password", out string password);
            payload.TryGetValue("CompanyId", out string CompanyIdStr);
            payload.TryGetValue("viaEmail", out string viaEmail);
            payload.TryGetValue("email", out string email);
            payload.TryGetValue("viaPhone", out string viaPhone);
            payload.TryGetValue("phone", out string phoneStr);
            payload.TryGetValue("Keyword", out string keyWord);
            payload.TryGetValue("MaxInactiveHours", out string maxInactiveHoursStr);
            payload.TryGetValue("Accesslevel", out string accesslevelStr);
            #endregion

            #region Kontakt erstellen
            Contact where = new Contact();

            if (int.TryParse(idStr, out int Id))
            {
                where.Id = Id;
            }

            Contact set = new Contact
            {
                Name = name,
                EntryTime = DateTime.UtcNow,                
                KeyWord = keyWord
            };

            if (password.Length > 0)
            {
                set.Password = Tab_Contact.Encrypt(password);
            }

            set.Email = email;


            if (int.TryParse(CompanyIdStr, out int companyId))
            {
                set.CompanyId = companyId;
            }

            if (int.TryParse(maxInactiveHoursStr, out int maxInactiveHours))
            {
                set.MaxInactiveHours = maxInactiveHours;
            }

            if (int.TryParse(accesslevelStr, out int accesslevel))
            {
                //kann maximal eigenen Access-Level vergeben.
                if (accesslevel > user.Accesslevel)
                    accesslevel = user.Accesslevel;

                set.Accesslevel = accesslevel;
            }

            if (ulong.TryParse(phoneStr, out ulong phone))
            {
                set.Phone = phone;
            }

            set.Via = Tab_Contact.Communication.Unknown;

            if (viaEmail != null) set.Via |= Tab_Contact.Communication.Email;
            if (viaPhone != null) set.Via |= Tab_Contact.Communication.Sms;
            #endregion

            bool success = Id > 0 && MelBoxSql.Tab_Contact.Update(set, where);

            string alert;

            if (success)
                alert = Html.Alert(3, "Kontakt gespeichert", "Der Kontakt [" + Id + "] " + name + " wurde erfolgreich geändert.");
            else
                alert = Html.Alert(1, "Fehler beim speichern des Kontakts", "Der Kontakt [" + Id + "] " + name + " konnte in der Datenbank nicht geändert werden.");

            await Server.PageAsync(context, "Benutzerkonto ändern", alert);
        }
        #endregion


        #region Firmeninformation
        [RestRoute("Get", "/company/{id:num}")]
        public async Task CompanyShow(IHttpContext context)
        {
            #region Anfragenden Firma identifizieren
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            if (!Server.LogedInHash.TryGetValue(guid, out Contact user))
            {
                await Home(context);
                return;
            }

            bool isAdmin = user.Accesslevel >= Server.Level_Admin;
            int showId = 0;

            if (context.Request.PathParameters.TryGetValue("id", out string idStr))
            {
                int.TryParse(idStr, out showId);
            }

            Company company = MelBoxSql.Tab_Company.SelectCompany(showId);
            #endregion

            Dictionary<string, string> pairs = new Dictionary<string, string>
            {
                { "##readonly##", isAdmin ? string.Empty : "readonly" },
                { "##disabled##", isAdmin ? string.Empty : "disabled" },
                { "##Id##", company.Id.ToString() },
                { "##Name##", company.Name },
                { "##Address##", company.Address },
                { "##City##", company.City },

                { "##NewCompany##", isAdmin ? Html.ButtonNew("company") : string.Empty }
            };

            string form = Server.Page(Server.Html_FormCompany, pairs);

            DataTable dt = Tab_Company.SelectCompanyAll(isAdmin ? 0 : company.Id);
            string table = Html.FromTable(dt, true, "company");

            await Server.PageAsync(context, "Firmeninformation", table + form);
        }

        [RestRoute("Post", "/company/new")]
        public async Task CompanyCreate(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            if (!Server.LogedInHash.TryGetValue(guid, out Contact user) || user.Accesslevel < Server.Level_Admin)
            {
                await Home(context);
                return;
            }

            #region Form auslesen
            Dictionary<string, string> payload = Server.Payload(context);
            //payload.TryGetValue("id", out string idStr);
            payload.TryGetValue("name", out string name);
            payload.TryGetValue("address", out string address);
            payload.TryGetValue("city", out string city);

            Company set = new Company
            {
                Name = name,
                Address = address,
                City = city
            };
            #endregion

            bool success = MelBoxSql.Tab_Company.Insert(set);
            string alert;

            if (success)
                alert = Html.Alert(3, "Neue Firmeninformation gespeichert", "Die Firmeninformation " + name + " wurde erfolgreich neu erstellt.");
            else
                alert = Html.Alert(1, "Fehler beim speichern der Firmeninformation", "Die Firmeninformation zu " + name + " konnte nicht in der Datenbank gespeichert werden.");

            await Server.PageAsync(context, "Neue Firmeninformation erstellen", alert);
        }

        [RestRoute("Post", "/company/update")]
        public async Task CompanyUpdate(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            if (!Server.LogedInHash.TryGetValue(guid, out Contact user) || user.Accesslevel < Server.Level_Reciever)
            {
                await Home(context);
                return;
            }

            #region Form auslesen
            Dictionary<string, string> payload = Server.Payload(context);
            payload.TryGetValue("id", out string idStr);
            payload.TryGetValue("name", out string name);
            payload.TryGetValue("address", out string address);
            payload.TryGetValue("city", out string city);
            #endregion

            Company where = new Company(0);

            if (int.TryParse(idStr, out int Id))
            {
                where.Id = Id;
            }

            Company set = new Company
            {
                Name = name,
                Address = address,
                City = city
            };

            bool success = Id > 0 && MelBoxSql.Tab_Company.Update(set, where);

            string alert;

            if (success)
                alert = Html.Alert(3, "Firmeninformation gespeichert", "Die Firmeninformation [" + Id + "] " + name + " wurde erfolgreich geändert.");
            else
                alert = Html.Alert(1, "Fehler beim speichern der Firmeninformation", "Die Firmeninformation zu [" + Id + "] " + name + " konnte in der Datenbank nicht geändert werden.");

            await Server.PageAsync(context, "Firmeninformation ändern", alert);
        }
        #endregion


        #region Benutzerverwaltung
        [RestRoute("Post", "/register")]
        public async Task Register(IHttpContext context)
        {
            Dictionary<string, string> payload = Server.Payload(context);
            payload.TryGetValue("name", out string name);
            //payload.TryGetValue("password", out string password); //Sicherheit!

            Dictionary<string, string> pairs = new Dictionary<string, string>
            {
                { "##readonly##", "readonly" },
                { "##disabled##", string.Empty },
                { "##Name##", name },                
                { "##CompanyList##", Tab_Company.SelectCompanyAllToHtmlOption() },
                { "##NewContact##", Html.ButtonNew("account") }
            };

            string form = Server.Page(Server.Html_FormRegister, pairs);

            await Server.PageAsync(context, "Benutzerregistrierung", form);
        }

        [RestRoute("Post", "/register/thanks")]
        public async Task RegisterProcessing(IHttpContext context)
        {
            #region Form auslesen
            Dictionary<string, string> payload = Server.Payload(context);
            //payload.TryGetValue("Id",out string idStr); //Wird automatisch vergeben
            payload.TryGetValue("name", out string name);
            payload.TryGetValue("password", out string password);
            payload.TryGetValue("CompanyId", out string CompanyIdStr);
            payload.TryGetValue("viaEmail", out string viaEmail);
            payload.TryGetValue("email", out string email);
            payload.TryGetValue("viaPhone", out string viaPhone);
            payload.TryGetValue("phone", out string phoneStr);
            //KeyWord nicht vergebbar
            //payload.TryGetValue("MaxInactiveHours", out string maxInactiveHoursStr);
            //payload.TryGetValue("Accesslevel", out string accesslevelStr);
            #endregion

            #region Kontakt erstellen
            Contact contact = new Contact
            {
                Name = name
            };

            if (MelBoxSql.Tab_Contact.Select(contact).Rows.Count > 0)
            {
                string error = Html.Alert(1, "Registrierung fehlgeschlagen", $"Der Benutzername {name} ist bereits vergeben." + @"<a href='/' class='w3-bar-item w3-button w3-teal w3-margin'>Nochmal</a>");
                await Server.PageAsync(context, "Benutzerregistrierung fehlgeschlagen", error);
                return;
            }

            contact.EntryTime = DateTime.UtcNow;
            contact.Password = Tab_Contact.Encrypt(password);
            contact.Email = email;

            if (int.TryParse(CompanyIdStr, out int companyId))
            {
                contact.CompanyId = companyId;
            }

            contact.MaxInactiveHours = 0;
            contact.Accesslevel = 0;

            if (ulong.TryParse(phoneStr, out ulong phone))
            {
                contact.Phone = phone;
            }

            contact.Via = Tab_Contact.Communication.Unknown;

            if (viaEmail != null) contact.Via |= Tab_Contact.Communication.Email;
            if (viaPhone != null) contact.Via |= Tab_Contact.Communication.Sms;
            #endregion



            bool success = MelBoxSql.Tab_Contact.Insert(contact);

            string alert;

            if (success)
                alert = Html.Alert(3, "Erfolgreich registriert", "Willkommen " + name + "!<br/> Die Registrierung muss noch durch einen Administrator bestätigt werden, bevor Sie sich einloggen können. Informieren Sie einen Administrator.");
            else
                alert = Html.Alert(1, "Registrierung fehlgeschlagen", "Es ist ein Fehler bei der Registrierung aufgetreten. Wenden Sie sich an den Administrator.");


            await Server.PageAsync(context, "Benutzerregistrierung", alert);
        }

        [RestRoute("Post", "/login")]
        public async Task Login(IHttpContext context)
        {
            Dictionary<string, string> payload = Server.Payload(context);
            string name = payload["name"];
            string password = payload["password"];
            string guid = Server.CheckCredentials(name, password);

            int prio = 1;
            string titel = "Login fehlgeschlagen";
            string text = "Benutzername und Passwort prüfen." + @"<a href='/' class='w3-bar-item w3-button w3-teal w3-margin'>Nochmal</a>";

            if (guid.Length > 0)
            {
                prio = 3;
                titel = "Login erfolgreich";
                text = "Willkommen " + name;

                System.Net.Cookie cookie = new System.Net.Cookie("MelBoxId", guid, "/");

                context.Response.Cookies.Add(cookie);
            }

            string alert = Html.Alert(prio, titel, text);

            await Server.PageAsync(context, titel, alert);
        }
        #endregion

        [RestRoute("Get", "/log")]
        public async Task LoggingShow(IHttpContext context)
        {
            System.Data.DataTable log = MelBoxSql.Tab_Log.SelectLast(1000);
            string table = Html.FromTable(log);

            await Server.PageAsync(context, "Log", table);
        }

        [RestRoute("Get", "/gsm")]
        public async Task ModemShow(IHttpContext context)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>
            {
                { "##OwnName##", GsmStatus.OwnName },
                { "##OwnNumber##", GsmStatus.OwnNumber },
                { "##ServiceCenter##", GsmStatus.ServiceCenterNumber },
                { "##ProviderName##" , GsmStatus.ProviderName }
            };

            string html = Server.Page(Server.Html_FormGsm, pairs);

            await Server.PageAsync(context, "GSM-Modem", html);
        }

        [RestRoute]
        public async Task Home(IHttpContext context)
        {
            string form = Server.Page(Server.Html_FormLogin, null);

            await Server.PageAsync(context, "Login", form);
        }

    }
}
