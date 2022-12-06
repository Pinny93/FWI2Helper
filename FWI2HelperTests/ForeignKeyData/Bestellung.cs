using System;

namespace FWI2HelperTests.ForeignKeyData
{
    public class Bestellung : ModelBase<Bestellung, DBBestellung>
    {
        public Kunde? Kunde { get; set; }

        public List<BestellPos> Positionen { get; } = new List<BestellPos>();

        public BestellStatus Status { get; set; }
    }
}