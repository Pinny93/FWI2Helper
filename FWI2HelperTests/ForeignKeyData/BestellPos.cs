using System;

namespace FWI2HelperTests.ForeignKeyData
{
    public class BestellPos : ModelBase<BestellPos, DBBestellPos>
    {
        private int _menge;

        public Artikel? Artikel { get; set; }

        public int Menge
        {
            get
            {
                return _menge;
            }
            set
            {
                if (value < 1) { throw new ArgumentException("Menge darf nicht kleiner 1 sein!"); }
                _menge = value;
            }
        }

        public BestellPos()
        {
            this.Menge = 1;
        }

        public BestellPos(Artikel artikel, int menge)
        {
            if (artikel == null) { throw new ArgumentNullException(nameof(artikel)); }

            this.Artikel = artikel;
            this.Menge = menge;
        }
    }
}