using System;
using System.Reflection;

namespace FWI2Helper
{
    public class Personengenerator
    {
        private const string FILE_FIRSTNAMES = "Häufige_Vornamen_Köln_2013.csv";
        private const string FILE_LASTNAMES = "Nachnamen_Häufigkeit_Deutschland.txt";

        private List<string> _firstNamesMale;
        private List<string> _firstNamesFemale;
        private List<string> _lastNames;
        private Random _random;

        public Personengenerator()
        {
            _random = new Random();
            _firstNamesMale = new List<string>();
            _firstNamesFemale = new List<string>();
            _lastNames = new List<string>();

            ReadFirstNames();
            ReadLastNames();
        }

        private void ReadFirstNames()
        {
            _firstNamesMale = new List<string>();
            _firstNamesFemale = new List<string>();

            var assembly = Assembly.GetExecutingAssembly();
            Stream? resource = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resouces.{FILE_FIRSTNAMES}");
            if (resource == null) { throw new FileNotFoundException($"'{FILE_FIRSTNAMES}' not found in resources!"); }

            using (StreamReader rdr = new StreamReader(resource))
            {
                // Skip Headline
                string? line = rdr.ReadLine();

                line = rdr.ReadLine();
                while (line != null)
                {
                    string[] data = line.Split(',');

                    List<string> list = data[2] switch
                    {
                        "m" => _firstNamesMale,
                        _ => _firstNamesFemale,
                    };

                    list.Add(data[0]);

                    line = rdr.ReadLine();
                }
            }
        }

        private void ReadLastNames()
        {
            _lastNames = new List<string>();

            var assembly = Assembly.GetExecutingAssembly();
            Stream? resource = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resouces.{FILE_LASTNAMES}");
            if (resource == null) { throw new FileNotFoundException($"'{FILE_LASTNAMES}' not found in resources!"); }

            using (StreamReader rdr = new StreamReader(resource))
            {
                string? line = rdr.ReadLine();
                while (line != null)
                {
                    string[] data = line.Split("   ");
                    _lastNames.Add(data[0]);

                    line = rdr.ReadLine();
                }
            }
        }

        public Person Erzeuge()
        {
            Gender gender = (Gender)_random.Next(0, 2);
            var firstNames = gender switch
            {
                Gender.Male => _firstNamesMale,
                _ => _firstNamesFemale,
            };

            DateTime startDate = new DateTime(1920, 1, 1);
            int maxDays = (int)new DateTime(2019, 12, 31).Subtract(startDate).TotalDays;

            Person createdPerson = new Person(
                _lastNames[_random.Next(0, _lastNames.Count)],
                firstNames[_random.Next(0, firstNames.Count)],
                gender, startDate.AddDays(_random.Next(0, maxDays)));

            return createdPerson;
        }

        public IEnumerable<Person> Erzeuge(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return Erzeuge();
            }
        }
    }
}