using System.Linq.Expressions;
using System.Reflection;
using Google.Protobuf.WellKnownTypes;
using MySql.Data.MySqlClient;

namespace FWI2Helper.Database;

public class MySqlEntityFieldMappingForeignKey<TEntity> : MySqlEntityFieldMapping<TEntity>
    where TEntity : class
{
    public string ForeignTableName { get; }

    public ForeignKeyMapType MapType { get; }

    public Type ForeignKeyNetType { get; }

    public MySqlEntityFieldMappingForeignKey(string classPropertyName, Type netType, string dbColumnName, string dbForeignTableName, ForeignKeyMapType mapType, Type foreignKeyNetType MySqlDbType? dbType = null)
        : base(classPropertyName, netType, dbColumnName, dbType)
    {
        this.ForeignTableName = dbForeignTableName;
        this.MapType = mapType;
        this.ForeignKeyNetType = foreignKeyNetType;
    }

    public MySqlEntityFieldMappingForeignKey(Expression<Func<T, TForeignKey?>> expression, string foreignTableName, string dbColumnName, MySqlDbType? dbType = null)
        : this(classPropertyName, netType, dbColumnName, dbType)
    {
    }

    public MySqlEntityFieldMappingForeignKey(Expression<Func<T, IEnumerable<TForeignKey>>> expression, string foreignTableName, string dbColumnName, MySqlDbType? dbType = null)
        : base(expression, dbColumnName, dbType)
    {
    }

    public MySqlEntityFieldMappingForeignKey(Expression<Func<T, TForeignKey?>> expression, string foreignTableName, string dbColumnName, MySqlDbType? dbType = null)
        : base(expression, dbColumnName, dbType)
    {
    }

    public MySqlEntityFieldMappingForeignKey(Expression<Func<TForeignKey, IEnumerable<T>>> expression, string foreignTableName, string dbColumnName, MySqlDbType? dbType = null)
        : base(expression, dbColumnName, dbType)
    {
    }
}

public class MySqlEntityFieldMappingForeignKey<TEntity, TForeignEntity> : MySqlEntityFieldMappingForeignKey<TEntity>
    where TEntity : class
    where TForeignEntity : class
{
    public MySqlEntityFieldMappingForeignKey(Expression<Func<TEntity, object?>> expression, string dbColumnName, string dbForeignTableName, MySqlDbType? dbType = null)
        : base(expression, dbColumnName, dbType)
    {
        this.ForeignTableName = dbForeignTableName;
        this.MapType = ForeignKeyMapType.Side1Property;
    }

    public MySqlEntityFieldMappingForeignKey(Expression<Func<TForeignEntity, object?>> expression, string dbColumnName, string dbForeignTableName, MySqlDbType? dbType = null)
    {
        static MemberInfo GetMember(Expression expr)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return ((MemberExpression)expr).Member;

                case ExpressionType.Convert:
                    return GetMember(((UnaryExpression)expr).Operand);

                default:
                    throw new NotSupportedException(expr.NodeType.ToString());
            }
        }

        MemberInfo member = GetMember(expression.Body);

        this.DotNetType = member.DeclaringType ?? typeof(void);
        this.ClassPropertyName = member.Name;
        this.DbColumnName = dbColumnName;
        this.DbType = dbType ?? GetDefaultDbTypeFromNetType(this.DotNetType);

        this.ForeignTableName = dbForeignTableName;
        this.MapType = ForeignKeyMapType.SideNList;
    }

    public override string ToString()
    {
        return $"FieldMappingForeignKey '{this.ClassPropertyName}' ({this.DotNetType.FullName}) --> '{this.ForeignTableName}.{this.DbColumnName}' ({this.DbType})";
    }
}

public enum ForeignKeyMapType
{
    SideNList,
    Side1Property
}