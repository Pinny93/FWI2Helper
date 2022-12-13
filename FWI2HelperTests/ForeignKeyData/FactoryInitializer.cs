namespace FWI2HelperTests.ForeignKeyData;

public static class FactoryInitializer
{
    public static bool _initialized = false;

    public static void InitializeFactories()
    {
        if (_initialized) { return; }

        // Configure Factory
        DBAccess<Artikel>.RegisterFactory((fact) =>
        {
            fact.CreateMapping("webshop_artikel")
                .AddPrimaryKey(e => e.Id, "id", MySqlDbType.Int32)
                .AddField(e => e.Bezeichnung, "bezeichnung", MySqlDbType.VarChar)
                .AddField(e => e.Beschreibung, "beschreibung", MySqlDbType.Text)
                .AddField(e => e.Kundenbewertung, "kundenbewertung", MySqlDbType.Int32)
                .AddField(e => e.Preis, "preis", MySqlDbType.Decimal);
        });

        DBAccess<Kunde>.RegisterFactory((fact) =>
        {
            fact.CreateMapping("webshop_kunde")
                .AddPrimaryKey(e => e.Id, "id", MySqlDbType.Int32)
                .AddField(e => e.Name, "name", MySqlDbType.VarChar)
                .AddField(e => e.Vorname, "vorname", MySqlDbType.VarChar)
                .AddField(e => e.EMail, "email", MySqlDbType.VarChar)
                .AddField(e => e.PasswortHash, "passwort", MySqlDbType.VarChar);
        });

        DBAccess<Bestellung>.RegisterFactory((fact) =>
        {
            // Configure Factory
            fact.CreateMapping("webshop_bestellung")
                .AddPrimaryKey(e => e.Id, "id", MySqlDbType.Int32)
                .AddField(e => e.Status, "status", MySqlDbType.Int32)
                .AddForeignKey(e => e.Kunde, "webshop_kunde", "idkunde", MySqlDbType.Int32)
                .AddForeignKey(e => (IEnumerable<BestellPos>)e.Positionen, "webshop_bestellpos", "idbestellung", MySqlDbType.Int32);
        });

        DBAccess<BestellPos>.RegisterFactory((fact) =>
        {
            fact.CreateMapping("webshop_bestellpos")
                .AddPrimaryKey(e => e.Id, "id", MySqlDbType.Int32)
                .AddForeignKey(e => e.Artikel, "webshop_artikel", "idartikel", MySqlDbType.Int32)
                .AddForeignKeyImport<Bestellung>(e => e.Positionen, "webshop_bestellung", "idbestellung", MySqlDbType.Int32)
                .AddField(e => e.Menge, "menge", MySqlDbType.Int32);
        });

        _initialized = true;
    }
}