using System;

namespace FWI2HelperTests.ForeignKeyData
{
    public class Artikel : ModelBase<Artikel, DBArtikel>
    {
        public string Beschreibung { get; set; } = "";

        public string Bezeichnung { get; set; } = "";

        public byte[]? Image { get; set; }

        public int Kundenbewertung { get; set; }

        public decimal Preis { get; set; }

        public Artikel()
        {
        }

        public static IEnumerable<Artikel> GetSample()
        {
            yield return new Artikel() { Id = 1, Bezeichnung = "Apfel, rot", Beschreibung = "Ein roter Apfel", Kundenbewertung = 4, Preis = 1.00m };
            yield return new Artikel() { Id = 2, Bezeichnung = "Apfel, gelb", Beschreibung = "Ein gelber Apfel", Kundenbewertung = 3, Preis = 0.90m };
            yield return new Artikel() { Id = 3, Bezeichnung = "Apfel, grün", Beschreibung = "Ein grüner Apfel", Kundenbewertung = 1, Preis = 0.80m };
            yield return new Artikel() { Id = 4, Bezeichnung = "Birne, grün", Beschreibung = "Eine grüne Birne", Kundenbewertung = 3, Preis = 0.90m };
            yield return new Artikel() { Id = 5, Bezeichnung = "Orange, orange", Beschreibung = "Eine organge Orange", Kundenbewertung = 5, Preis = 0.50m };
        }

        public override string ToString()
        {
            return $"{Id,5} - {Bezeichnung,-30} ({Kundenbewertung}/5): {Preis:N2} EUR  {Beschreibung,-50}";
        }
    }
}