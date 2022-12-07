using System;

namespace FWI2HelperTests.ForeignKeyData
{
    public class DBBestellPos : DBAccess<BestellPos>
    {
        public static DBBestellPos GetInstance()
        {
            return new DBBestellPos();
        }
    }
}