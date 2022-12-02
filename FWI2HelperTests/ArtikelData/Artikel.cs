using System;

namespace FWI2HelperTests.ArtikelData
{
    public class Artikel : IEquatable<Artikel?>
    {
        public string Beschreibung { get; set; } = "";

        public string Bezeichnung { get; set; } = "";

        public int Id { get; set; }

        public byte[]? Image { get; set; }

        public int Kundenbewertung { get; set; }

        public decimal Preis { get; set; }

        public Artikel()
        {
        }

        public static Artikel? TryGetById(int id)
        {
            return DBArtikel.Instance.TryGetById(id);
        }

        public static Artikel GetById(int id)
        {
            return DBArtikel.Instance.GetById(id);
        }

        public static IEnumerable<Artikel> GetSample()
        {
            yield return new Artikel() { Id = 1, Bezeichnung = "Apfel, rot", Beschreibung = "Ein roter Apfel", Kundenbewertung = 4, Preis = 1.00m };
            yield return new Artikel() { Id = 2, Bezeichnung = "Apfel, gelb", Beschreibung = "Ein gelber Apfel", Kundenbewertung = 3, Preis = 0.90m };
            yield return new Artikel() { Id = 3, Bezeichnung = "Apfel, grün", Beschreibung = "Ein grüner Apfel", Kundenbewertung = 1, Preis = 0.80m };
            yield return new Artikel() { Id = 4, Bezeichnung = "Birne, grün", Beschreibung = "Eine grüne Birne", Kundenbewertung = 3, Preis = 0.90m };
            yield return new Artikel() { Id = 5, Bezeichnung = "Orange, orange", Beschreibung = "Eine organge Orange", Kundenbewertung = 5, Preis = 0.50m };
        }

        public static bool operator !=(Artikel? left, Artikel? right)
        {
            return !(left == right);
        }

        public static bool operator ==(Artikel? left, Artikel? right)
        {
            return EqualityComparer<Artikel>.Default.Equals(left, right);
        }

        public static IEnumerable<Artikel> ReadAll()
        {
            return DBArtikel.Instance.ReadAll();
        }

        public void Create()
        {
            DBArtikel.Instance.Create(this);
        }

        public void Delete()
        {
            DBArtikel.Instance.Delete(this);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Artikel);
        }

        public bool Equals(Artikel? other)
        {
            return other is not null &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public override string ToString()
        {
            return $"{Id,5} - {Bezeichnung,-30} ({Kundenbewertung}/5): {Preis:N2} EUR  {Beschreibung,-50}";
        }

        public void Update()
        {
            DBArtikel.Instance.Update(this);
        }
    }
}