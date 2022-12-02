using System;
using FWI2HelperTests.ArtikelData;

namespace FWI2HelperTests
{
    public class MySqlEntityTestArtikel
    {
        [Fact]
        public void TestCreateAndDeleteArtikel()
        {
            Artikel a = new Artikel()
            {
                Bezeichnung = "Kaki, Braun",
                Kundenbewertung = 5,
                Beschreibung = "Eine braune Kaki",
                Preis = 1.00m
            };

            a.Create();

            Assert.NotEqual(0, a.Id);

            a.Delete();
        }
    }
}