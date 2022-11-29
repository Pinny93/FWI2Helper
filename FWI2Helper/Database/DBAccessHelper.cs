using System;
using MySql.Data.MySqlClient;

namespace FWI2Helper.Database
{
    public static class DBAccessHelper
    {
        public static MySqlConnection MySqlOpenDB()
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder()
            {
                Server = "bszw.ddns.net",
                Database = "fwi2123_pinzer",
                UserID = "fwi2123",
                Password = "geheim",
            };

            MySqlConnection con = new MySqlConnection(builder.ConnectionString);

            return con;
        }

        public static void MySqlCloseDB(MySqlConnection con)
        {
            con.Close();
        }

        public static int MySqlExecuteNonQuery(string sql)
        {
            int anz;

            MySqlConnection con = MySqlOpenDB();
            MySqlCommand cmd = new MySqlCommand(sql, con);

            anz = cmd.ExecuteNonQuery();

            MySqlCloseDB(con);

            return anz;
        }
    }
}