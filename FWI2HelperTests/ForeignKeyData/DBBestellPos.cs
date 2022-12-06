using System;

namespace FWI2HelperTests.ForeignKeyData
{
    public class DBBestellPos : DBAccess<BestellPos>
    {
        public override MySqlEntityFactory<BestellPos> GetFactory()
        {
            // Configure Factory
            MySqlEntityFactory<BestellPos> maFact = new(MySqlOpenDB, "webshop_bestellpos");
            maFact.CreateMapping()
                .AddPrimaryKey(e => e.Id, "id", MySqlDbType.Int32)
                .AddForeignKey(e => e.Artikel, "webshop_artikel", "idartikel", MySqlDbType.Int32)
                .AddForeignKeyImport<Bestellung>(e => e.Positionen, "webshop_bestellung", "idbestellung", MySqlDbType.Int32)
                .AddField(e => e.Menge, "menge", MySqlDbType.Int32);

            return maFact;
        }

        public static DBBestellPos GetInstance()
        {
            return new DBBestellPos();
        }
    }
}