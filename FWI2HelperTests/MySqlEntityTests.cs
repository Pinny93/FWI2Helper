namespace FWI2HelperTests
{
    public class MySqlEntityTests
    {
        private MySqlEntityFactory<Mitarbeiter> GetMAFactory()
        {
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
                .AddField(p => p.Birthday, "gebdatum", MySqlDbType.Date)
                .AddField(p => (int)p.Gender, "geschl", MySqlDbType.Int32);

            return maFact;
        }

        private Mitarbeiter GetNewMitarbeiter()
        {
            Random rnd = new Random();
            Mitarbeiter arb = Mitarbeiter.FromPerson(new Personengenerator().Erzeuge());
            arb.Gehalt = (decimal)(rnd.Next(530, 99000) + rnd.NextDouble());

            return arb;
        }

        private void EnsureEntityMatchesDB(Mitarbeiter ma, MySqlEntityFactory<Mitarbeiter> fact)
        {
            ma.Gehalt = Math.Round(ma.Gehalt, 2); // DB has only 2 digits

            Mitarbeiter dbMa = fact.GetEntityById(ma.ID);

            Assert.Equal(ma, dbMa);
        }

        [Fact]
        public void InsertEntityAndDelete()
        {
            var fact = this.GetMAFactory();
            Mitarbeiter arb = this.GetNewMitarbeiter();

            MySqlEntity<Mitarbeiter> newPers = fact.FromEntity(arb);

            newPers.Create();
            this.EnsureEntityMatchesDB(arb, fact);

            newPers.Delete();
        }

        [Fact]
        public void InsertUpdateAndDelete()
        {
            var fact = this.GetMAFactory();
            Mitarbeiter arb = this.GetNewMitarbeiter();

            MySqlEntity<Mitarbeiter> newPers = fact.FromEntity(arb);

            newPers.Create();
            this.EnsureEntityMatchesDB(arb, fact);

            newPers.Entity.FirstName = "Heinz";
            newPers.Entity.LastName = "Strunk";
            newPers.Entity.Gender = Gender.Male;
            newPers.Entity.Gehalt = 10000.215m;
            newPers.Entity.Birthday = DateTime.Today;
            newPers.Update();
            this.EnsureEntityMatchesDB(arb, fact);

            newPers.Delete();
        }
    }
}