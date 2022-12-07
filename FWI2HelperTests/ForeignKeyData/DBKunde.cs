using System;

namespace FWI2HelperTests.ForeignKeyData
{
    public class DBKunde : DBAccess<Kunde>
    {
        public static DBKunde GetInstance()
        {
            return new DBKunde();
        }
    }
}