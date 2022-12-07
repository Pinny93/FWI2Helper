using System;

namespace FWI2HelperTests.ForeignKeyData
{
    public class DBBestellung : DBAccess<Bestellung>
    {
        public static DBBestellung GetInstance()
        {
            return new DBBestellung();
        }
    }
}