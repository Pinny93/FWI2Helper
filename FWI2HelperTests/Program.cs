using System.Globalization;
using FWI2Helper;
using FWI2HelperTests;
using MySql.Data.MySqlClient;

namespace PersonenverwaltungDB
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Random rnd = new Random();

            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder()
            {
                Server = "bszw.ddns.net",
                Database = "fwi2123_pinzer",
                UserID = "fwi2123",
                Password = "geheim",
            };

            MySqlConnection con = new MySqlConnection(builder.ConnectionString);

            // Configure Factory
            MySqlEntityFactory<Mitarbeiter> maFact = new(con, "person");
            maFact.CreateMapping()
                .AddPrimaryKey(p => p.ID, "id", MySqlDbType.Int32)
                .AddField(p => p.LastName, "nachname", MySqlDbType.VarChar)
                .AddField(p => p.FirstName, "vorname", MySqlDbType.VarChar)
                .AddField(p => p.Gehalt, "gehalt", MySqlDbType.Decimal)
                .AddField(p => p.Birthday, "gebdatum", MySqlDbType.Date);

            try
            {
                con.Open();

                Mitarbeiter arb = Mitarbeiter.FromPerson(new Personengenerator().Erzeuge());
                arb.Gehalt = (decimal)(rnd.Next(530, 99000) + rnd.NextDouble());

                MySqlEntity<Mitarbeiter> newPers = maFact.FromEntity(arb);
                newPers.Create();

                newPers.Entity.LastName = "Huber";
                newPers.Update();

                newPers.Delete();

                var newPers2 = maFact.GetById(50);

                //await CreatePersonen(50, con);
                ShowMitarbeiter(con);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                con.Clone();
            }

            await Task.CompletedTask;
        }

        private static async Task CreatePersonen(int anz, MySqlConnection con)
        {
            Random rnd = new Random();
            Personengenerator gen = new Personengenerator();
            string sql = "INSERT INTO person VALUES \r\n";

            foreach (var curPerson in gen.Erzeuge(anz))
            {
                double gehalt = rnd.Next(530, 99000);
                gehalt += rnd.NextDouble();

                sql += $"(id, '{curPerson.LastName}', '{curPerson.FirstName}', {gehalt.ToString("0.00", CultureInfo.InvariantCulture)},'{curPerson.Birthday:yyyy-MM-dd}'),\r\n";
            }

            sql = sql.Remove(sql.Length - 3);
            sql += ";";

            Console.WriteLine(sql);

            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, con);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void ShowMitarbeiter(MySqlConnection con)
        {
            MySqlCommand cmd = new("SELECT * FROM person", con);

            try
            {
                var rdr = cmd.ExecuteReader(); //await Task.Factory.FromAsync(cmd.BeginExecuteReader(), cmd.EndExecuteReader);
                while (rdr.Read())
                {
                    int id = rdr.GetInt32("id");
                    String vn = rdr.GetString("vorname");
                    String nn = rdr.GetString("nachname");
                    DateTime datum = rdr.GetDateTime("gebdatum");
                    double gehalt = rdr.GetDouble("gehalt");
                    Console.WriteLine($"{id} {nn} {vn} {datum:dd.MM.yyyy} {gehalt}");
                }
                rdr.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}