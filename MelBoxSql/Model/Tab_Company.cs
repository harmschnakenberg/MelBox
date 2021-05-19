using System;
using System.Collections.Generic;
using System.Text;

namespace MelBoxSql
{
    public static class Tab_Company
    {  
        internal const string TableName = "Company";

        private static Dictionary<string, object> ToDictionary(Company company)
        {
            Dictionary<string, object> set = new Dictionary<string, object>();

            if (company.Id > 0) set.Add(nameof(company.Id), company.Id);
            if (company.Name != null) set.Add(nameof(company.Name), company.Name);
            if (company.Address != null) set.Add(nameof(company.Address), company.Address);
            if (company.City != null) set.Add(nameof(company.City), company.City);

            return set;
        }

        public static bool CreateTable()
        {           
            Dictionary<string, string> columns = new Dictionary<string, string>
                {
                    { nameof(Company.Id), "INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT" },
                    { nameof(Company.Name), "TEXT NOT NULL" },
                    { nameof(Company.Address), "TEXT" },
                    { nameof(Company.City), "TEXT" }
                };

            return Sql.CreateTable(TableName, columns);
        }

        public static bool Insert(Company company)
        {    
            return Sql.Insert(TableName, ToDictionary(company));
        }

        public static bool Update(Company set, Company where)
        {    
            return Sql.Update(TableName, ToDictionary(set), ToDictionary(where));
        }

        public static bool Delete(Company where)
        {
            return Sql.Delete(TableName, ToDictionary(where));
        }

        public static System.Data.DataTable Select(Company where)
        {
            Dictionary<string, object> columns = ToDictionary(where);

            string query = "SELECT * FROM " + TableName + " WHERE ";
            query += Sql.ColNameAlias(columns.Keys, " AND ");

            return Sql.SelectDataTable("Firma", query, Sql.Alias(columns));
        }

        public static Company SelectCompany(int Id)
        {
            string query = "SELECT * FROM " + TableName + " WHERE Id = " + Id + "; ";

            System.Data.DataTable dt = Sql.SelectDataTable("Einzelfirma", query, null);

            Company company = new Company();

            if (dt.Rows.Count == 0) return company;

            string name = dt.Rows[0][nameof(company.Name)].ToString();
            string address = dt.Rows[0][nameof(company.Address)].ToString();
            string city = dt.Rows[0][nameof(company.City)].ToString();
            

            company.Id = Id;
            company.Name = name;
            company.Address = address;
            company.City = city;

            return company;
        }

        public static System.Data.DataTable SelectCompanyAll(int limitId = 0)
        {
            string query = "SELECT Id, Name, City AS Ort FROM " + TableName;

            if (limitId > 0)
                query += " WHERE Id = " + limitId;

            return Sql.SelectDataTable("Firmen", query, null);
        }


        public static string SelectCompanyAllToHtmlOption(int exceptId = 0)
        {
            string companies = string.Empty;

            System.Data.DataTable dt = SelectCompanyAll();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //<option value="2">Option 2</option>

                int id = int.Parse(dt.Rows[i][0].ToString());
                string name = dt.Rows[i][1].ToString();
                string city = dt.Rows[i][2].ToString();
                city = System.Text.RegularExpressions.Regex.Replace(city, @"\d", "");

                if (id != exceptId)               
                    companies +=$"<option value='{id}'>{name}, {city}</option>" + Environment.NewLine;
            }

            return companies;
        }

    }

    public class Company
    {
        public Company()
        { }

        public Company(int id)
        { 
            Id = id;
        }

        public Company(string name, string address, string city)
        {
            Name = name;
            Address = address;
            City = city;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public string City { get; set; }
    }
}
