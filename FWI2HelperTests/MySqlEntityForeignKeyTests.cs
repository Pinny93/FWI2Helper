using System;
using FWI2HelperTests.ForeignKeyData;

namespace FWI2HelperTests
{
    public class MySqlEntityForeignKeyTests
    {
        [Fact]
        public void TestCreateEntityWithForeignKey()
        {
            Kunde k = new Kunde()
            {
                Name = "Test",
                Vorname = "Automated",
                EMail = "automated@test.org",
                PasswortHash = "INVALID"
            };
            k.Create();

            Artikel art = new Artikel()
            {
                Bezeichnung = "Testartikel",
                Beschreibung = "Ein Artikel, nur zum Testen. Sollte nicht gekauft werden, da dieser nur leere Luft ist",
                Kundenbewertung = 4,
                Preis = 0.01m,
            };
            art.Create();

            Bestellung best = new Bestellung()
            {
                Kunde = k,
                Positionen =
                {
                    new BestellPos()
                    {
                        Artikel = art,
                        Menge = 5
                    }
                },
                Status = BestellStatus.Warenkorb,
            };
            best.Create();

            Bestellung bestFromDb = Bestellung.GetById(best.Id);

            Assert.Equal(bestFromDb, best);

            // Cleanup
            best.Positionen.ForEach(pos => pos.Delete());
            best.Delete();
            art.Delete();
            k.Delete();
        }

        [Fact]
        public void TestGetEntityWithForeignKey()
        {
            Bestellung best = DBBestellung.Instance.GetById(1);

            Assert.Equal(1, best.Id);
        }
    }
}