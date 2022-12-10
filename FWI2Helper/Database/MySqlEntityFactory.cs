using System.Data;
using System.Diagnostics;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace FWI2Helper.Database;

public abstract class MySqlEntityFactory 
{
    public abstract Type GetEntityType();
}

public class MySqlEntityFactory<T> : MySqlEntityFactory
    where T : class, new()
{
    private Func<MySqlConnection> _connectionFactory;

    public MySqlEntityMapping<T> Mapping { get; private set; }

    public MySqlEntityFactory(Func<MySqlConnection> conFact)
    {
        _connectionFactory = conFact;
        this.Mapping = new();
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

            var dbColumns = this.Mapping.Fields
                .Where(field => !(field is MySqlEntityFieldMappingForeignKey<T> fkey && fkey.MapType == ForeignKeyMapType.Side1List))
                .Select(m => m.DbColumnName)
                .ToCommaSeparatedString();

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
                curMapping.SetNetValueFromReader(newEntity, rdr);
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

    internal IQueryable<T> GetAllWithForeignKey<TListEntity>(MySqlEntityFieldMappingForeignKey<T, TListEntity> mapping, TListEntity foreignEntity)
        where TListEntity : class, new()
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

                MySqlCommand cmd = new($"SELECT {dbColumns} FROM {this.Mapping.TableName} WHERE {mapping.DbColumnName} = @foreignId", con);

                cmd.Parameters.Add("@foreignId", mapping.DbType);
                cmd.Parameters["@foreignId"].Value = MySqlEntityFieldMappingForeignKey<T, TListEntity>.GetForeignEntityPrimaryKey(foreignEntity);

                MySqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    T newEntity = new();
                    foreach (var curMapping in fields)
                    {
                        curMapping.SetNetValueFromReader(newEntity, rdr);
                    }

                    Trace.WriteLine($"GetAll(): Enumerating through {typeof(T).FullName}:{this.Mapping?.PrimaryKey?.GetDBValue(newEntity) ?? -1}");
                    yield return newEntity;
                }

                Trace.WriteLine($"GetAll(): End of Enumeration {typeof(T).FullName}");
            }
        }

        return EnumerateAll().AsQueryable();
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

                MySqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    T newEntity = new();
                    foreach (var curMapping in fields)
                    {
                        curMapping.SetNetValueFromReader(newEntity, rdr);
                    }

                    Trace.WriteLine($"GetAll(): Enumerating through {typeof(T).FullName}:{this.Mapping?.PrimaryKey?.GetDBValue(newEntity) ?? -1}");
                    yield return newEntity;
                }

                Trace.WriteLine($"GetAll(): End of Enumeration {typeof(T).FullName}");
            }
        }

        return EnumerateAll().AsQueryable();
    }

    public MySqlEntityMapping<T> CreateDefaultMapping()
    {
        foreach (PropertyInfo curProperty in typeof(T).GetProperties())
        {
            MySqlEntityFieldMapping<T> mapping = 
                new MySqlEntityFieldMapping<T>(curProperty.Name, curProperty.PropertyType, 
                    curProperty.Name.ToLower(), MySqlEntityFieldMapping<T>.GetDefaultDbTypeFromNetType(curProperty.PropertyType));

            this.Mapping.AddField(mapping);
        }

        return this.Mapping;
    }

    public MySqlEntityMapping<T> CreateMapping(string tableName)
    {
        this.Mapping = new MySqlEntityMapping<T>(tableName);
        return this.Mapping;
    }

    public override Type GetEntityType()
    {
        return typeof(T);
    }
}