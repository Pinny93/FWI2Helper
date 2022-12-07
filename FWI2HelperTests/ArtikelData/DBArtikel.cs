using System;

namespace FWI2HelperTests.ArtikelData
{
    public class DBArtikel : DBAccess<Artikel>
    {
        private static DBArtikel? _instance;

        public override MySqlEntityFactory<Artikel> GetFactory()
        {
            // Configure Factory
            MySqlEntityFactory<Artikel> maFact = new(MySqlOpenDB);
            maFact.CreateMapping("webshop_artikel")
                .AddPrimaryKey(e => e.Id, "id", MySqlDbType.Int32)
                .AddField(e => e.Bezeichnung, "bezeichnung", MySqlDbType.VarChar)
                .AddField(e => e.Beschreibung, "beschreibung", MySqlDbType.Text)
                .AddField(e => e.Kundenbewertung, "kundenbewertung", MySqlDbType.Int32)
                .AddField(e => e.Preis, "preis", MySqlDbType.Decimal)
                .AddField(e => e.Image, "image", MySqlDbType.Blob);

            return maFact;
        }

        public static DBArtikel Instance
        {
            get { return _instance ??= new DBArtikel(); }
        }
    }
}