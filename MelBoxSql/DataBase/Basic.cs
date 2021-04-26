using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MelBoxSql
{
    public static partial class Sql
    {
        #region Datenbank-Datei

        private static string _DbFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// Stammordner für die Datenbank
        /// </summary>
        public static string DbFolder
        {
            get
            {
                return _DbFolder;
            }
            set
            {
                if (System.IO.Directory.Exists(value))
                    _DbFolder = value;
            }
        }

        internal static string DbPath = System.IO.Path.Combine(_DbFolder, "DB", "MelBox2.db");
        private static readonly string DataSource = "Data Source=" + DbPath;

        #endregion

        #region Fundamental Communication
        internal static bool NonQuery(string query, Dictionary<string, object> args = null)
        {
            if (!isDatabaseExistent) Open();

            try
            {
                //int n = 0;

                using (var connection = new SqliteConnection(DataSource))
                {
                    SQLitePCL.Batteries.Init();
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = query;
                    if (args != null && args.Count > 0)
                    {
                        foreach (string key in args.Keys)
                        {
                            command.Parameters.AddWithValue(key, args[key]);
                        }
                    }

                    return command.ExecuteNonQuery() > 0;
                    //Console.WriteLine(n + " | " + command.CommandText + Environment.NewLine);


                    //connection.Dispose();
                }

               // return 0 < n;
            }
            catch (Exception ex)
            {
                throw new Exception("SqlNonQuery(): " + query + "\r\n" + ex.GetType() + "\r\n" + ex.Message);
            }
            finally
            {

            }
        }

        internal static DataTable SelectDataTable(string tableName, string query, Dictionary<string, object> args = null)
        {            
            if (!isDatabaseExistent) Open();

            try
            {
                DataTable myTable = new DataTable
                {
                    TableName = tableName
                };

                using (var connection = new SqliteConnection(DataSource))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = query;
                    //Console.WriteLine(command.CommandText); //TEST

                    if (args != null && args.Count > 0)
                    {
                        foreach (string key in args.Keys)
                        {
                            command.Parameters.AddWithValue(key, args[key]);
                            //Console.WriteLine(key + "\t"+ args[key]); //TEST
                        }
                    }
                                       
                    try
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            //Mit Schema einlesen
                            myTable.Load(reader);
                            return myTable;
                        }
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch
                    {
                        //Wenn Schema aus DB nicht eingehalten wird (z.B. UNIQUE Constrain in SELECT Abfragen); dann neue DataTable, alle Spalten <string>
                        using (var reader = command.ExecuteReader())
                        {
                            //zu Fuß einlesen
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                //Spalten einrichten
                                myTable.Columns.Add(reader.GetName(i), typeof(string));
                            }

                            while (reader.Read())
                            {
                                List<object> row = new List<object>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string colType = myTable.Columns[i].DataType.Name;

                                    if (reader.IsDBNull(i))
                                    {
                                        row.Add(string.Empty);
                                    }
                                    else
                                    {
                                        string r = reader.GetFieldValue<string>(i);
                                        row.Add(r);
                                    }
                                }

                                myTable.Rows.Add(row.ToArray());
                            }

                        }
                    }
#pragma warning restore CA1031 // Do not catch general exception types
                }

                return myTable;
            }
            catch (Exception ex)
            {
                throw new Exception("SqlSelectDataTable(): " + query + "\r\n" + ex.GetType() + "\r\n" + ex.Message);
            }
        }

        #endregion

        #region ExtractFromTable

        public static List<object> GetColumn(DataTable dataTable, string columnName)
        {
            List<object> col = new List<object>();

            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                col.Add(dataTable.Rows[i][columnName]);
            }

            return col;
        }

        public static string GetFirstEntry(DataTable dataTable, string columnName)
        {
            try
            {
                if (dataTable.Rows.Count > 0)
                    return dataTable.Rows[0][columnName].ToString();
                else
                    return string.Empty;
            }
            catch (Exception ex)
            {                
                throw ex;
            }
        }
        #endregion

        #region Basic SQL-Operations
        //internal static bool CreateTable(string tableName, Dictionary<string, object> columns, List<string> constrains = null)
        //{
        //    string query = "CREATE TABLE IF NOT EXISTS " + tableName + " (";

        //    foreach (var column in columns.Keys)
        //    {
        //        query += " \"" + column + "\" " + columns[column].ToString() + ",";
        //    }

        //    query = query.TrimEnd(',');

        //    if (constrains != null)
        //    {
        //        foreach (var constrain in constrains)
        //        {
        //            query += ", " + constrain;
        //        }
        //    }

        //    query += ");";

        //    Console.WriteLine("***" + query);

        //    return NonQuery(query, null);
        //}

        internal static bool CreateTable2(string tableName, Dictionary<string, string> columns, List<string> constrains = null)
        {
            string query = "CREATE TABLE IF NOT EXISTS " + tableName + " (";

            foreach (var column in columns.Keys)
            {
                query += " \"" + column + "\" " + columns[column].ToString() + ",";
            }

            query = query.TrimEnd(',');

            if (constrains != null)
            {
                foreach (var constrain in constrains)
                {
                    query += ", " + constrain;
                }
            }

            query += ");";

            return NonQuery(query, null);
        }

        internal static bool Insert(string tableName, Dictionary<string, object> set)
        {
            //INSERT OR IGNORE INTO table_name (column1, column2, column3, ...) VALUES(value1, value2, value3, ...);

            string query = "INSERT OR IGNORE INTO " + tableName + " (";

            Dictionary<string, object> args = new Dictionary<string, object>();
            string vals = string.Empty;

            foreach (var key in set.Keys)
            {
                query += key + ",";
                vals += "@" + key + ",";
                args.Add("@" + key, set[key]);
            }

            query = query.TrimEnd(',') + ") VALUES (" + vals.TrimEnd(',') + "); ";

            return NonQuery(query, args);
        }


        internal static bool Update(string tableName, Dictionary<string, object> set, Dictionary<string, object> where, string delimiter = "AND")
        {
            //UPDATE table_name SET column1 = value1, column2 = value2, ... WHERE condition;

            Dictionary<string, object> args = new Dictionary<string, object>();

            string query = "UPDATE " + tableName + " SET ";

            foreach (var key in set.Keys)
            {
                query += key + " = @" + key + ",";
                args.Add("@" + key, set[key]);
            }

            query = query.TrimEnd(',') + " WHERE ";

            bool first = true;
            foreach (var key in where.Keys)

            {
                if (first)
                    first = false;
                else
                    query += " " + delimiter + " ";

                query += key + " = @" + key;
                args.Add("@" + key, where[key]);
            }

            return NonQuery(query, args);
        }

        internal static bool Delete(string tableName, Dictionary<string, object> where)
        {
            //DELETE FROM table_name WHERE condition [ AND condition ];

            string query = "DELETE FROM " + tableName + " WHERE " + ColNameAlias(where.Keys, " AND ");

            //Dictionary<string, object> args = new Dictionary<string, object>();
            //bool first = true;
            //foreach (var key in where.Keys)
            //{
            //    if (first) { first = false; } else { query += " AND "; }
            //    query += key + " = @" + key;
            //    args.Add("@" + key, where[key]);
            //}

            return NonQuery(query, Alias(where));
        }

        #endregion
    }

}
