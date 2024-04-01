using System.Collections.Generic;
using System.Configuration;
using System.Data;
using DemoSite.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace DemoSite.Services
{
    public class DataBaseHelper
    {
        private readonly IConfiguration _config;

        public DataBaseHelper(IConfiguration config)
        {
            _config = config;
        }

        public List<Dictionary<string, object>> GetData(IDbConnection conn, string sql, Param[] parameters = null)
        {
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (string.IsNullOrEmpty(sql))
                return new List<Dictionary<string, object>>();

            if (parameters != null)
                AddParameters(cmd, parameters);

            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                IDataReader reader = cmd.ExecuteReader();
                return ConvertToList(reader);
            }
            finally
            {
                conn.Close();
            }
        }

        private void AddParameters(IDbCommand cmd, Param[] parameters)
        {
            foreach (var p in parameters)
            {
                var param = cmd.CreateParameter();
                param.DbType = p.DataType;
                param.ParameterName = p.Name;
                param.Value = p.Value;

                cmd.Parameters.Add(param);
            }
        }

        private List<Dictionary<string, object>> ConvertToList(IDataReader reader)
        {
            var result = new List<Dictionary<string, object>>();

            while (reader.Read())
                result.Add(CreateRow(reader));

            return result;
        }

        private Dictionary<string, object> CreateRow(IDataReader reader)
        {
            var row = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
                row.Add(reader.GetName(i), reader[i]);

            return row;
        }

        public List<List<object>> GetDataList(IDbConnection conn, string sql)
        {
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (string.IsNullOrEmpty(sql))
                return new List<List<object>>();

            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                IDataReader reader = cmd.ExecuteReader();
                return Convert(reader);
            }
            finally
            {
                conn.Close();
            }
        }

        private List<List<object>> Convert(IDataReader reader)
        {
            var result = new List<List<object>>();

            while (reader.Read())
            {
                var row = new List<object>();

                for (int i = 0; i < reader.FieldCount; i++)
                    row.Add(reader[i]);

                result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// Creates DBConnection object for MySQL database.
        /// </summary>
        /// <returns>Returns an instance of MySQLConnection.</returns>
        public IDbConnection CreateMySqlConnection()
        {
            return new MySqlConnection(_config.GetConnectionString("AdventureWorks"));
        }
    }
}