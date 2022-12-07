using System.Collections.ObjectModel;
using System.Linq.Expressions;
using MySql.Data.MySqlClient;

namespace FWI2Helper.Database;

public class MySqlEntityMapping<T>
    where T : class, new()
{
    private List<MySqlEntityFieldMapping<T>> _fields = new();

    public MySqlEntityFieldMapping<T>? PrimaryKey { get; set; }

    public ReadOnlyCollection<MySqlEntityFieldMapping<T>> Fields { get; }

    public string TableName { get; internal set; } = "defaultTable";

    public MySqlEntityMapping()
    {
        this.Fields = new(_fields);
    }

    public MySqlEntityMapping(string tableName)
        : this()
    {
        this.TableName = tableName;
    }

    internal MySqlEntityMapping<T> AddField(MySqlEntityFieldMapping<T> field)
    {
        _fields.Add(field);
        return this;
    }

    public MySqlEntityMapping<T> AddField(Expression<Func<T, object?>> expression, string dbColumnName, MySqlDbType? dbType = null)
    {
        _fields.Add(new MySqlEntityFieldMapping<T>(expression, dbColumnName, dbType));
        return this;
    }

    public MySqlEntityMapping<T> AddPrimaryKey(Expression<Func<T, object?>> expression, string dbColumnName, MySqlDbType? dbType = null)
    {
        if (this.PrimaryKey != null) { throw new InvalidOperationException("Primary key already set!"); }

        var field = new MySqlEntityFieldMapping<T>(expression, dbColumnName, dbType);

        this.PrimaryKey = field;
        _fields.Add(field);
        return this;
    }

    public MySqlEntityMapping<T> AddForeignKey<TForeignKey>(Expression<Func<T, TForeignKey?>> expression, string dbForeignTableName, string dbColumnName, MySqlDbType? dbType = null)
        where TForeignKey : class, new()
    {
        var field = new MySqlEntityFieldMappingForeignKey<T, TForeignKey>(expression, dbForeignTableName, dbColumnName, dbType);

        _fields.Add(field);
        return this;
    }

    public MySqlEntityMapping<T> AddForeignKey<TForeignKey>(Expression<Func<T, IEnumerable<TForeignKey>>> expression, string dbForeignTableName, string dbColumnName, MySqlDbType? dbType = null)
        where TForeignKey : class, new()
    {
        var field = new MySqlEntityFieldMappingForeignKey<T, TForeignKey>(expression, dbForeignTableName, dbColumnName, dbType);

        _fields.Add(field);
        return this;
    }

    public MySqlEntityMapping<T> AddForeignKeyImport<TForeignKey>(Expression<Func<TForeignKey, IEnumerable<T>>> expression, string dbForeignTableName, string dbColumnName, MySqlDbType? dbType = null)
        where TForeignKey : class, new()
    {
        var field = new MySqlEntityFieldMappingForeignKey<T, TForeignKey>(expression, dbForeignTableName, dbColumnName, dbType);

        _fields.Add(field);
        return this;
    }

    public override string ToString()
    {
        return $"Mapping for '{typeof(T).FullName}' to Table '{this.TableName}'";
    }
}