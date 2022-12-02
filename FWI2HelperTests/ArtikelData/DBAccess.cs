using System;

namespace FWI2HelperTests.ArtikelData
{
    public abstract class DBAccess<T>
        where T : class, new()
    {
        private MySqlEntityFactory<T> _factory;

        public DBAccess()
        {
            _factory = this.GetFactory();
        }

        public abstract MySqlEntityFactory<T> GetFactory();

        public static MySqlConnection MySqlOpenDB()
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder()
            {
                Server = "bszw.ddns.net",
                Database = "fwi2123_pinzer",
                UserID = "fwi2123",
                Password = "geheim",
            };

            MySqlConnection con = new MySqlConnection(builder.ConnectionString);

            return con;
        }

        public IEnumerable<T> ReadAll()
        {
            return _factory.GetAll();
        }

        public T GetById(int id)
        {
            return _factory.GetEntityById(id);
        }

        public T? TryGetById(int id)
        {
            return _factory.TryGetEntityById(id);
        }

        public void Create(T entity)
        {
            _factory.FromEntity(entity)
                .Create();
        }

        public void Delete(T entity)
        {
            _factory.FromEntity(entity)
                .Delete();
        }

        public void Update(T entity)
        {
            _factory.FromEntity(entity)
                .Update();
        }
    }
}