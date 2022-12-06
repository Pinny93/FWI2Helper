using System;

namespace FWI2HelperTests.ForeignKeyData
{
    public class DBBestellung : DBAccess<Bestellung>
    {
        public override MySqlEntityFactory<Bestellung> GetFactory()
        {
            // Configure Factory
            MySqlEntityFactory<Bestellung> maFact = new(MySqlOpenDB, "webshop_bestellung");
            maFact.CreateMapping()
                .AddPrimaryKey(e => e.Id, "id", MySqlDbType.Int32)
                .AddField(e => e.Status, "status", MySqlDbType.Int32)
                .AddForeignKey(e => e.Kunde, "webshop_kunde", "idkunde", MySqlDbType.Int32)
                .AddForeignKey(e => e.Positionen, "webshop_bestellpos", "idbestellung", MySqlDbType.Int32);

            return maFact;
        }

        public static DBBestellung GetInstance()
        {
            return new DBBestellung();
        }
    }
}