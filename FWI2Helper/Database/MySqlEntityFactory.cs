using System.Data;
using System.Diagnostics;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace FWI2Helper.Database;

public class MySqlEntityFactory<T>
    where T : class, new()
{
    private Func<MySqlConnection> _connectionFactory;

    public MySqlEntityMapping<T> Mapping { get; }

    public MySqlEntityFactory(Func<MySqlConnection> conFact, string tableName)
    {
        _connectionFactory = conFact;
        this.Mapping = new(tableName);
    }

    public MySqlEntityFactory(MySqlEntityMapping<T> mapping, Func<MySqlConnection> conFact)
    {
        _connectionFactory = conFact;
        this.Mapping = mapping;
    }

    public T GetEntityById<TPrimaryKey>(TPrimaryKey id)
    {
        T? entity = TryGetEntityById(id);
        if (entity == null)
        {
            throw new Exception($"ID '{id}' not found in {this.Mapping.TableName}!");
        }

        return entity;
    }

    public T? TryGetEntityById<TPrimaryKey>(TPrimaryKey id)
    {
        if (this.Mapping.PrimaryKey == null) { throw new NotSupportedException("No Primary Key Set! Update is currently only possible if there is a primary key!"); }

        using (var con = _connectionFactory())
        {
            con.Open();

            string dbColumns = $"{this.Mapping.Fields.Select(m => m.DbColumnName).ToCommaSeparatedString()}";

            MySqlCommand cmd = new($"SELECT {dbColumns} FROM {this.Mapping.TableName} WHERE {this.Mapping.PrimaryKey.DbColumnName} = @id", con);

            cmd.Parameters.Add("@id", this.Mapping.PrimaryKey.DbType);
            cmd.Parameters["@id"].Value = id;

            var rdr = cmd.ExecuteReader();
            if (!rdr.Read())
            {
                return null;
            }

            T newEntity = new();
            foreach (var curMapping in this.Mapping.Fields)
            {
                if (curMapping is MySqlEntityFieldMappingForeignKey<TPrimaryKey,>)

                    curMapping.SetNetValue(newEntity, rdr[curMapping.DbColumnName]);
            }

            return newEntity;
        }
    }

    public MySqlEntity<T> GetById<TPrimaryKey>(TPrimaryKey id)
    {
        return this.FromEntity(this.GetEntityById(id));
    }

    public MySqlEntity<T> FromEntity(T entity)
    {
        return new MySqlEntity<T>(entity, this.Mapping, _connectionFactory);
    }

    public IQueryable<T> GetAll()
    {
        List<T> data = new List<T>();

        // TODO: Enhance, so that no complete enumeration is done on every call... (Implement IQueryable Provider)
        IEnumerable<T> EnumerateAll()
        {
            using (var con = _connectionFactory())
            {
                con.Open();

                var fields = this.Mapping.Fields;
                string dbColumns = $"{fields.Select(m => m.DbColumnName).ToCommaSeparatedString()}";

                MySqlCommand cmd = new($"SELECT {dbColumns} FROM {this.Mapping.TableName}", con);

                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    T newEntity = new();
                    foreach (var curMapping in fields)
                    {
                        curMapping.SetNetValue(newEntity, rdr[curMapping.DbColumnName]);
                    }

                    Trace.WriteLine($"GetAll(): Enumerating through {typeof(T).FullName}:{this.Mapping?.PrimaryKey?.GetDBValue(newEntity) ?? -1}");
                    yield return newEntity;
                }

                Trace.WriteLine($"GetAll(): End of Enumeration {typeof(T).FullName}");
                yield break;
            }
        }

        return EnumerateAll().AsQueryable();
    }

    public MySqlEntityMapping<T> CreateDefaultMapping()
    {
        foreach (PropertyInfo curProperty in typeof(T).GetProperties())
        {
            MySqlEntityFieldMapping<T> mapping = new();

            mapping.DotNetType = curProperty.PropertyType;
            mapping.ClassPropertyName = curProperty.Name;
            mapping.DbColumnName = curProperty.Name.ToLower();
            mapping.DbType = MySqlEntityFieldMapping<T>.GetDefaultDbTypeFromNetType(curProperty.PropertyType);

            this.Mapping.AddField(mapping);
        }

        return this.Mapping;
    }

    public MySqlEntityMapping<T> CreateMapping()
    {
        return this.Mapping;
    }
}