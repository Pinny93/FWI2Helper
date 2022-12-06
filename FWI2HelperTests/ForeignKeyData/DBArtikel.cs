using System;

namespace FWI2HelperTests.ForeignKeyData
{
    public class DBArtikel : DBAccess<Artikel>
    {
        public override MySqlEntityFactory<Artikel> GetFactory()
        {
            // Configure Factory
            MySqlEntityFactory<Artikel> maFact = new(MySqlOpenDB, "webshop_artikel");
            maFact.CreateMapping()
                .AddPrimaryKey(e => e.Id, "id", MySqlDbType.Int32)
                .AddField(e => e.Bezeichnung, "bezeichnung", MySqlDbType.VarChar)
                .AddField(e => e.Beschreibung, "beschreibung", MySqlDbType.Text)
                .AddField(e => e.Kundenbewertung, "kundenbewertung", MySqlDbType.Int32)
                .AddField(e => e.Preis, "preis", MySqlDbType.Decimal)
                .AddField(e => e.Image, "image", MySqlDbType.Blob);

            return maFact;
        }

        public static DBArtikel GetInstance()
        {
            return new DBArtikel();
        }
    }
}