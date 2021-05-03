using Grapevine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MelBoxWeb
{
    public partial class Server
    {
        public static string Page(string path, Dictionary<string, string> insert)
        {
            if (!File.Exists(path))
                return "<p>Datei nicht gefunden: <i>" + path + "</i><p>";

            string template = System.IO.File.ReadAllText(path);

            StringBuilder sb = new StringBuilder(template);
            
            if (insert != null)
            {
                foreach (var key in insert.Keys)
                {
                    sb.Replace(key, insert[key]);
                }
            }

            return sb.ToString();
        }

        public static Dictionary<string, string> ReadCookies(IHttpContext context)
        {
            Dictionary<string, string> cookies = new Dictionary<string, string>();

            foreach (Cookie cookie in context.Request.Cookies)
            {
                cookies.Add(cookie.Name, cookie.Value);
            }

            return cookies;
        }

        public static async System.Threading.Tasks.Task PageAsync(IHttpContext context, string titel, string body)
        {
#if DEBUG

            ReadCookies(context).TryGetValue("MelBoxId", out string guid);
#else
            string guid = string.Empty;
#endif
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("##Titel##", titel);
            pairs.Add("##Id##", guid != null ? string.Empty : "Gastzugang");
            pairs.Add("##Inhalt##", body);

            string html = Server.Page(Server.Html_Skeleton, pairs);

            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }

        /// <summary>
        /// POST-Inhalte lesen
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Key-Value-Pair</returns>
        public static Dictionary<string, string> Payload(IHttpContext context)
        {
            System.IO.Stream body = context.Request.InputStream;
            System.IO.StreamReader reader = new System.IO.StreamReader(body);
                      
            string[] pairs = reader.ReadToEnd().Split('&');

            Dictionary<string, string> payload = new Dictionary<string, string>();

            foreach (var pair in pairs)
            {
                string[] item = pair.Split('=');

                if (item.Length > 1)             
                    payload.Add(item[0], WebUtility.UrlDecode(item[1]));                                
            }

            return payload;
        }

        private static string HtmlDecode(string encoded)
        {
            //Ändert z.B. &lt; in <        
            //return WebUtility.HtmlDecode(encoded);
            return WebUtility.UrlDecode(encoded);
        }

        internal static string CheckCredentials(string name, string password)
        {
            int id;

            try
            {
                id = MelBoxSql.Tab_Contact.Authentification(name, password);
            }
            catch (Exception ex)
            {
                throw ex;
                // Was tun?
            }

            if (id > 0)
            {
                while (LogedInHash.Count > 20) //Max. 20 Benutzer gleichzetig eingelogged
                {
                    LogedInHash.Remove(LogedInHash.Keys.GetEnumerator().Current);
                }

                string guid = Guid.NewGuid().ToString("N");

                MelBoxSql.Contact user = MelBoxSql.Tab_Contact.SelectContact(id); 

                LogedInHash.Add(guid, user);

                return guid;
            }

            return string.Empty;
        }

    }
}
