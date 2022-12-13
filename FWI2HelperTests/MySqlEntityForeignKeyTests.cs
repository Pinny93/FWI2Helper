using System;
using FWI2HelperTests.ForeignKeyData;
using Artikel = FWI2HelperTests.ForeignKeyData.Artikel;

namespace FWI2HelperTests
{
    public class MySqlEntityForeignKeyTests
    {
        [Fact]
        public void TestForeignKeyBuilding()
        {
            FactoryInitializer.InitializeFactories();
            var fact = DBBestellung.Instance.GetFactory();

            Assert.True(fact.Mapping.Fields.Count == 4, "Invalid Field count for mapping!");
            Assert.True(fact.Mapping.Fields[0] == fact.Mapping.PrimaryKey);

            // Bestellung.Kunde
            Assert.True(fact.Mapping.Fields[2] is MySqlEntityFieldMappingForeignKey<Bestellung, Kunde> fKeyMapping &&
                        fKeyMapping.MapType == ForeignKeyMapType.SideNProperty);
            // Bestellung.Positionen
            Assert.True(fact.Mapping.Fields[3] is MySqlEntityFieldMappingForeignKey<Bestellung, BestellPos> fKeyMapping2 &&
                        fKeyMapping2.MapType == ForeignKeyMapType.Side1List);

            var factPos = DBBestellPos.Instance.GetFactory();
            Assert.True(factPos.Mapping.Fields.Count == 4);
            Assert.True(factPos.Mapping.Fields[0] == factPos.Mapping.PrimaryKey);

            Assert.True(factPos.Mapping.Fields[1] is MySqlEntityFieldMappingForeignKey<BestellPos, Artikel> fkeyArt &&
                        fkeyArt.MapType == ForeignKeyMapType.SideNProperty);
            Assert.True(factPos.Mapping.Fields[2] is MySqlEntityFieldMappingForeignKey<BestellPos, Bestellung> fkeyBest &&
                        fkeyBest.MapType == ForeignKeyMapType.SideNListImport);
        }

        [Fact]
        public void TestGetEntityWithForeignKey()
        {
            FactoryInitializer.InitializeFactories();

            Bestellung best = Bestellung.GetById(1);

            Assert.True(best.Id == 1);
            Assert.True(best.Kunde != null);
            Assert.True(best.Positionen != null);
            Assert.True(best.Positionen?.Count > 0);
            Assert.True(best.Status == BestellStatus.Warenkorb);
        }

        [Fact]
        public void TestGetMany()
        {
            FactoryInitializer.InitializeFactories();
            var bestCol = Bestellung.ReadAll().ToList();

            Assert.True(bestCol.Any());
            Assert.True(bestCol[0].Positionen.Any());
        }

        [Fact]
        public void TestInsertWithForeignKey()
        {
            FactoryInitializer.InitializeFactories();

            Bestellung best = new Bestellung()
            {
                Kunde = Kunde.GetById(1),
                Positionen =
                {
                    new BestellPos()
                    {
                        Artikel = Artikel.GetById(1),
                        Menge = 5
                    }
                },
                Status = BestellStatus.Warenkorb,
            };
            best.Create();
            int id = best.Id;

            best = Bestellung.GetById(id);
            Assert.True(best.Id > 0);
            Assert.True(best.Kunde == Kunde.GetById(1));
            Assert.True(best.Positionen.Count == 1);
            Assert.True(best.Positionen[0].Artikel != null);
            Assert.True(best.Status == BestellStatus.Warenkorb);

            // Cleanup
            best.Delete();
        }

        [Fact]
        public void TestCreateUpdateDeleteEntityWithForeignKey()
        {
            FactoryInitializer.InitializeFactories();
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

            // Check DB Entity matches created one
            Assert.Equal(bestFromDb.Id, best.Id);
            Assert.Equal(bestFromDb.Kunde, best.Kunde);
            Assert.Equal(bestFromDb.Status, best.Status);
            Assert.Equal(bestFromDb.Positionen.Count, best.Positionen.Count);
            
            for(int i = 0; i < best.Positionen.Count; i++)
            {
                Assert.Equal(best.Positionen[i].Id, bestFromDb.Positionen[i].Id);
            }

            // Add new Postions and Update

            Artikel art2 = new Artikel()
            {
                Bezeichnung = "Testartikel #2",
                Beschreibung = "Ein weiterer Artikel, nur zum Testen. Sollte nicht gekauft werden, da dieser nur leere Luft ist",
                Kundenbewertung = 4,
                Preis = 0.01m,
            };
            art2.Create();

            // Remove existing to test removal
            best.Positionen.Clear();

            best.Positionen.Add(new BestellPos() {
                Artikel = art2,
                Menge = 3,
            });

            best.Positionen.Add(new BestellPos() {
                Artikel = art,
                Menge = 8,
            });

            best.Status = BestellStatus.Bestellt;
            best.Update();

            bestFromDb = Bestellung.GetById(best.Id);
            Assert.Equal(2, bestFromDb.Positionen.Count);
            Assert.True(bestFromDb.Positionen[0].Id > 0);
            Assert.True(bestFromDb.Positionen[1].Id > 0);
            Assert.True(bestFromDb.Positionen[0].Menge == 3);
            Assert.True(bestFromDb.Positionen[1].Menge == 8);

            // Cleanup
            //best.Positionen.ForEach(pos => pos.Delete());
            best.Delete();
            art.Delete();
            art2.Delete();
            k.Delete();
        }
    }
}