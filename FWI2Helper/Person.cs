using System;

namespace FWI2Helper
{
    public enum Gender
    {
        Male,
        Female,
    }

    public class Person
    {
        public Person()
        {
        }

        public Person(string lastName, string firstName, Gender gender, DateTime birthday)
        {
            LastName = lastName;
            FirstName = firstName;
            Gender = gender;
            Birthday = birthday;
        }

        public string LastName { get; set; } = "";

        public string FirstName { get; set; } = "";

        public Gender? Gender { get; set; }

        public DateTime Birthday { get; set; }

        public int Age
        {
            get
            {
                return Convert.ToInt32((Convert.ToInt32($"{DateTime.Today:yyyyMMdd}") - Convert.ToInt32($"{this.Birthday:yyyyMMdd}")).ToString().PadLeft(8, '0')[0..^4]);
            }
        }

        public int Age2
        {
            get
            {
                int years = DateTime.Today.Year - this.Birthday.Year;
                DateTime test = this.Birthday.AddYears(years);

                return test > DateTime.Today ? --years : years;
            }
        }

        public override string ToString()
        {
            return $"{FirstName} {LastName} {Gender} {Birthday.ToShortDateString()} {Age} {Age2}";
        }
    }
}