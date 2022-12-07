using System;

namespace FWI2HelperTests.ForeignKeyData
{
    public class DBArtikel : DBAccess<Artikel>
    {
        public static DBArtikel GetInstance()
        {
            return new DBArtikel();
        }
    }
}