using Grapevine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MelBoxSql;

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

        
        [RestRoute("Get", "/in")]
        public async Task InBox(IHttpContext context)
        {
            //Später per URL Beginn und Ende definierbar?

            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            System.Data.DataTable rec = MelBoxSql.Sql.Recieved_View(DateTime.UtcNow.AddDays(-14), DateTime.UtcNow);
            string table = Html.FromTable(rec, guid != null, "blocked");

            await Server.PageAsync(context, "Eingang", table);
        }


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

            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("##MsgId##", message.Id.ToString());
            pairs.Add("##From##", contact.Name + " (" + company.Name + ")");
            pairs.Add("##Message##", message.Content);
            pairs.Add("##Mo##", mo ? "checked" : string.Empty);
            pairs.Add("##Tu##", tu ? "checked" : string.Empty);
            pairs.Add("##We##", we ? "checked" : string.Empty);
            pairs.Add("##Th##", th ? "checked" : string.Empty);
            pairs.Add("##Fr##", fr ? "checked" : string.Empty);
            pairs.Add("##Sa##", sa ? "checked" : string.Empty);
            pairs.Add("##Su##", su ? "checked" : string.Empty);
            pairs.Add("##Start##", message.StartBlockHour.ToString());
            pairs.Add("##End##", message.EndBlockHour.ToString());

            string form = Server.Page(Server.Html_FormMessage, pairs);

            await Server.PageAsync(context, "Eingang", form);
        }


        [RestRoute("Get", "/blocked")]
        public async Task BlockedMessage(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            System.Data.DataTable blocked = MelBoxSql.Sql.Blocked_View();
            string table = Html.FromTable(blocked, guid != null, "blocked");

            await Server.PageAsync(context, "Eingang", table);
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

            Message set = new Message();
            set.BlockedDays = blockedDays;
            set.StartBlockHour = startHour;
            set.EndBlockHour = endHour;

            string alert;
            if (!Tab_Message.Update(set, id))            
                alert = Html.Alert(1, "Nachricht aktualisieren fehlgeschlagen", $"Die Nachricht {idStr} konnte nicht geändert werden.");            
            else
                alert = Html.Alert(2, "Nachricht aktualisiert", $"Änderungen für die Nachricht {idStr} gespeichert.");

            System.Data.DataTable sent = MelBoxSql.Sql.Blocked_View();
            string table = Html.FromTable(sent);

            await Server.PageAsync(context, "Nachricht aktualisiert", alert + table);
        }


        [RestRoute("Get", "/out")]
        public async Task OutBox(IHttpContext context)
        {           
            System.Data.DataTable sent = MelBoxSql.Sql.Sent_View(DateTime.UtcNow.AddDays(-14), DateTime.UtcNow);
            string table = Html.FromTable(sent);

            await Server.PageAsync(context, "Ausgang", table);
        }


        [RestRoute("Get", "/account")]
        public async Task Account(IHttpContext context)
        {
            Server.ReadCookies(context).TryGetValue("MelBoxId", out string guid);

            if (!Server.LogedInHash.TryGetValue(guid, out int contactId))
            {
                await Home(context);
                return;
            }

            Contact contact = MelBoxSql.Tab_Contact.SelectContact(contactId);
            Company company = MelBoxSql.Tab_Company.SelectCompany(contact.CompanyId);
    
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("##Id##", contact.Id.ToString());
            pairs.Add("##Name##", contact.Name);
            pairs.Add("##Accesslevel##", contact.Accesslevel.ToString());
            pairs.Add("##CompanyId##", contact.Accesslevel.ToString());
            pairs.Add("##CompanyName##", company.Name);
            pairs.Add("##viaEmail##", contact.Via.HasFlag(Tab_Contact.Communication.Email) ? "checked" : string.Empty);
            pairs.Add("##CEmail##", contact.Email);

            string form = Server.Page(Server.Html_FormAccount, pairs);

            await Server.PageAsync(context, "Benutzerkonto", form);
        }


        [RestRoute("Post", "/register")]
        public async Task Register(IHttpContext context)
        {
            Dictionary<string, string> payload = Server.Payload(context);
            string name = payload["name"];
            string password = payload["password"];
           
            string form = $"<p class='w3-pink'>noch nicht implementiert</p><h2>{name}</h2><h3>{password}</h3>";

            await Server.PageAsync(context, "Benutzerregistrierung", form);
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
            string text = "Benutzername und Passwort prüfen." + @"<a href='/' class='w3-bar-item w3-button w3-teal w3-margin'>Nochmal</a>"; ;

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


        [RestRoute]
        public async Task Home(IHttpContext context)
        {
            string form = Server.Page(Server.Html_FormLogin, null);

            await Server.PageAsync(context, "Login", form);
        }

    }
}
