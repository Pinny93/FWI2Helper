using System;

namespace FWI2HelperTests.ForeignKeyData
{
    public class DBKunde : DBAccess<Kunde>
    {
        public override MySqlEntityFactory<Kunde> GetFactory()
        {
            // Configure Factory
            MySqlEntityFactory<Kunde> maFact = new(MySqlOpenDB, "webshop_kunde");
            maFact.CreateMapping()
                .AddPrimaryKey(e => e.Id, "id", MySqlDbType.Int32)
                .AddField(e => e.Name, "name", MySqlDbType.VarChar)
                .AddField(e => e.Vorname, "vorname", MySqlDbType.VarChar)
                .AddField(e => e.EMail, "email", MySqlDbType.VarChar)
                .AddField(e => e.PasswortHash, "passwort", MySqlDbType.VarChar);

            return maFact;
        }

        public static DBKunde GetInstance()
        {
            return new DBKunde();
        }
    }
}