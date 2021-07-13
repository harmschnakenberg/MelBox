using System.Collections.Generic;

namespace MelBoxSql
{
    public static class Tab_Status
    {
        internal const string TableName = "Status";

        public static bool CreateTable()
        {
            Dictionary<string, string> columns = new Dictionary<string, string>
                {
                    { "Id", "INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT" },
                    { "Property",  "TEXT" },
                    { "Value", "TEXT" }
                };

            return Sql.CreateTable(TableName, columns);
        }

        public static bool Insert(string property, string value)
        {
            Dictionary<string, object> roles = new Dictionary<string, object>
                {
                    { property, value }
                };

            return Sql.Insert(TableName, roles);
        }

        public static bool Update(string property, string value)
        {
            Dictionary<string, object> set = new Dictionary<string, object>
                {
                    { "Value", value }
                };

            Dictionary<string, object> where = new Dictionary<string, object>
                {
                    { "Property", property }
                };

            return Sql.Update(TableName, set, where);
        }

        public static string Select(string property)
        {
            string query = $"SELECT Value FROM {TableName} WHERE Property = '{property}'";

            System.Data.DataTable dt = Sql.SelectDataTable("Bereitschaft", query);

            return dt.Rows[0][0].ToString();
        }
    }

    //public static class GsmStatus
    //{
    //    const string init = "-unbekannt-";
    //    public static int SignalQuality { get; set; } = -1; //Mobilfunktsiganlqualität
    //    public static double SignalErrorRate { get; set; } = -1; //BitError-Rate (nur für Sprachanrufe interessant?)
    //    public static string OwnName { get; set; } = init; //Im Sim-Telefonbuch hinterlegter Name
    //    public static string OwnNumber { get; set; } = init; // Telefonnumer der iengelegten Sim-Karte
    //    public static string NetworkRegistration { get; set; } = init; //Text zum Anmeldestatus im Mobilfunknetz
    //    public static string ServiceCenterNumber { get; set; } = init; //SMS-Serviceneter-Nummer
    //    public static string ProviderName { get; set; } = init; //Netzbetreibername z.B. Telekom
    //    public static ulong RelayNumber { get; set; } = 0; //Nummer, an die Sprachanrufe weitergeleitet werden.
    //    public static string PinStatus { get; set; } = init; //Gibt an, ob die PIN richtig gesetzt ist
    //}
}