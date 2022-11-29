using FWI2Helper;

namespace FWI2HelperTests
{
    public class Mitarbeiter : Person, IEquatable<Mitarbeiter?>
    {
        public decimal Gehalt { get; set; }

        public int ID { get; set; }

        public static Mitarbeiter FromPerson(Person p)
        {
            return new Mitarbeiter()
            {
                FirstName = p.FirstName,
                LastName = p.LastName,
                Birthday = p.Birthday,
                Gender = p.Gender,
                Gehalt = 0.0m,
            };
        }

        public static bool operator !=(Mitarbeiter? left, Mitarbeiter? right)
        {
            return !(left == right);
        }

        public static bool operator ==(Mitarbeiter? left, Mitarbeiter? right)
        {
            return EqualityComparer<Mitarbeiter>.Default.Equals(left, right);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Mitarbeiter);
        }

        public bool Equals(Mitarbeiter? other)
        {
            return other is not null &&
                   LastName == other.LastName &&
                   FirstName == other.FirstName &&
                   Gender == other.Gender &&
                   Birthday == other.Birthday &&
                   Age == other.Age &&
                   ID == other.ID &&
                   Gehalt == other.Gehalt;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LastName, FirstName, Gender, Birthday, Age, Age2, ID, Gehalt);
        }
    }
}