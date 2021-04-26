using System;
using System.Collections.Generic;
using System.Text;

namespace MelBoxSql
{
    public partial class Sql
    {

        /// <summary>
        /// Erzeugt eine Zeichenkette der Form KeyName = @KeyName Delemiter [..]
        /// z.B. Name = @Name AND Id = @Id
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        internal static string ColNameAlias(Dictionary<string, object>.KeyCollection columns, string delimiter)
        {
            string line = string.Empty;

            bool first = true;
            foreach (var colName in columns)
            {
                if (first) { first = false; } else { line += delimiter; }
                line += colName + " = @" + colName;
            }

            return line;
        }

        /// <summary>
        /// Setzt vor den Schlüssel ein '@'
        /// Key, Value --> @Key, Value
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static Dictionary<string, object> Alias(Dictionary<string, object> args)
        {
            Dictionary<string, object> newArgs = new Dictionary<string, object>();

            foreach (var key in args.Keys)
            {
                newArgs.Add("@" + key, args[key]);
            }

            return newArgs;
        }
    }

}
