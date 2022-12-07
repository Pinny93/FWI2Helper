using System;
using System.Reflection;

namespace FWI2HelperTests.ForeignKeyData
{
    public class DBAccess<T>
        where T : class, new()
    {
        protected static DBAccess<T>? _instance;

        private MySqlEntityFactory<T> _factory;

        public static DBAccess<T> Instance
        {
            get { return _instance ?? GetInstance() ?? throw new InvalidOperationException("GetInstance() returned null"); }
        }

        public DBAccess()
        {
            _factory = this.GetFactory();
        }

        private static DBAccess<T>? GetInstance()
        {
            foreach (var curType in Assembly.GetExecutingAssembly().GetTypes())
            {
                Type? t = curType;

                bool isA = false;
                while (t != null && t != typeof(object))
                {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(DBAccess<>) && t.GetGenericArguments()[0] == typeof(T))
                    {
                        isA = true;
                        break;
                    }
                    else
                    {
                        t = t.BaseType;
                    }
                }

                if (isA)
                {
                    var method = curType.GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static);
                    if (method == null) { throw new InvalidOperationException($"Method GetInstance() not found on type {curType.FullName}!"); }

                    return (DBAccess<T>?)(method.Invoke(null, null));
                }
            }

            return null;
        }

        public virtual MySqlEntityFactory<T> GetFactory(string factContainer = "default")
        {
            return MySqlFactContainer.GetInstance(factContainer).GetFactoryForEntity<T>();
        }

        public static void RegisterFactory(Action<MySqlEntityFactory<T>> factoryBuilder, string factContainer = "default")
        {
            MySqlEntityFactory<T> fact = new MySqlEntityFactory<T>(MySqlOpenDB);
            factoryBuilder(fact);

            MySqlFactContainer.GetInstance(factContainer).RegisterFactory(fact);
        }

        public static void RegisterFactory(string factContainer = "default")
        {
            MySqlEntityFactory<T> fact = new MySqlEntityFactory<T>(MySqlOpenDB);
            fact.CreateDefaultMapping();

            MySqlFactContainer.GetInstance(factContainer).RegisterFactory(fact);
        }

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