using System.Linq.Expressions;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace FWI2Helper.Database;

public class MySqlEntityFieldMapping<T>
    where T : class, new()
{
    public string ClassPropertyName { get; init; }

    public string DbColumnName { get; init; }

    public MySqlDbType DbType { get; init; }

    public Type DotNetType { get; init; }

    public MySqlEntityFieldMapping(string classPorpertyName, Type netType, string dbColumnName, MySqlDbType? dbType = null)
    {
        this.DotNetType = netType;
        this.ClassPropertyName = classPorpertyName;
        this.DbColumnName = dbColumnName;
        this.DbType = dbType ?? GetDefaultDbTypeFromNetType(this.DotNetType);
    }

    public MySqlEntityFieldMapping(Expression<Func<T, object?>> expression, string dbColumnName, MySqlDbType? dbType = null)
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
    }

    public static MySqlDbType GetDefaultDbTypeFromNetType(Type t)
    {
        return t.Name switch
        {
            nameof(String) => MySqlDbType.Text,
            nameof(Int32) => MySqlDbType.Int32,
            nameof(Decimal) => MySqlDbType.Decimal,
            nameof(DateOnly) => MySqlDbType.Date,
            nameof(TimeOnly) => MySqlDbType.Time,
            nameof(DateTime) => MySqlDbType.DateTime,
            _ => MySqlDbType.String,
        };
    }

    public override string ToString()
    {
        return $"FieldMapping '{this.ClassPropertyName}' ({this.DotNetType.FullName}) --> '{this.DbColumnName}' ({this.DbType})";
    }

    /// <summary>
    /// Gets the value for the Database
    /// </summary>
    internal object? GetDBValue(T entity)
    {
        var propInfo = typeof(T).GetProperty(this.ClassPropertyName);
        if (propInfo == null) { throw new InvalidOperationException($"Property '{this.ClassPropertyName}' not found on class '{typeof(T).FullName}'"); }

        object? value = propInfo.GetValue(entity);
        
        return this.NetValue2DbValue(entity, value);
    }

    internal object? GetNetValue(T entity)
    {
        var propInfo = typeof(T).GetProperty(this.ClassPropertyName);
        if (propInfo == null) { throw new InvalidOperationException($"Property '{this.ClassPropertyName}' not found on class '{typeof(T).FullName}'"); }

        object? value = propInfo.GetValue(entity);
        return value;
    }

    internal void SetNetValueFromReader(T entity, object? value)
    {
        if (this is MySqlEntityFieldMappingForeignKey<T> foreignKeyMapping)
        {
            switch(foreignKeyMapping.MapType)
            {
                case ForeignKeyMapType.Side1Import:
                    break;
                case ForeignKeyMapType.Side1Property:
                    if(value != null) { foreignKeyMapping.ResolveNetEntityById(entity, value); }
                    break;
                case ForeignKeyMapType.SideNList:
                    foreignKeyMapping.ResolveNetEntitiesById(entity);
                    break;
                default:
                    throw new NotSupportedException("Unknown Map Type");
            }
        }
        else
        {
            this.SetNetValue(entity, value);
        }
    }

    internal void SetNetValue(T entity, object? value)
    {
        typeof(T).GetProperty(this.ClassPropertyName)?.SetValue(entity, this.DbValue2NetValue(value));
    }

    internal void AddNetValueToCollection(T entity, object? value)
    {
        var propInfo = typeof(T).GetProperty(this.ClassPropertyName);
        if (propInfo == null) { throw new InvalidOperationException($"Property '{this.ClassPropertyName}' not found on class '{typeof(T).FullName}'"); }

        object? valueToGet = propInfo.GetValue(entity);
        Type? valueType = valueToGet?.GetType();

        if(valueType == null)
        {
            throw new InvalidOperationException($"Value of Property '{this.ClassPropertyName}' is null!");
        }
        else if(valueType is IList<T> listType)
        {
            listType.Add(entity);
        }
        else
        {
            throw new InvalidOperationException($"Type '{valueType.FullName}' is no supported List type!");
        }
    }

    private object? NetValue2DbValue(T entity, object? value)
    {
        var propInfo = typeof(T).GetProperty(this.ClassPropertyName);
        if (propInfo == null) { throw new InvalidOperationException($"Property '{this.ClassPropertyName}' not found on class '{typeof(T).FullName}'"); }

        object? valueToGet = propInfo.GetValue(entity);
        if (propInfo.PropertyType.IsEnum)
        {
            valueToGet = valueToGet is null ? null : (int)valueToGet;
        }

        return valueToGet;
    }

    private object? DbValue2NetValue(object? value)
    {
        var propInfo = typeof(T).GetProperty(this.ClassPropertyName);
        if (propInfo == null) { throw new InvalidOperationException($"Property '{this.ClassPropertyName}' not found on class '{typeof(T).FullName}'"); }

        object? valueToSet = value;
        if (value is DBNull) { valueToSet = null; }

        // If types not identical, try to convert
        Type netType = propInfo.PropertyType;
        if (valueToSet?.GetType() != netType)
        {
            bool isNullable = netType.IsGenericType && !netType.IsGenericTypeDefinition && netType.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (isNullable)
            {
                netType = netType.GenericTypeArguments[0];
            }

            // Handle Nullable -> If not nullable, null is not allowed on value types
            if (!isNullable && netType.IsValueType && valueToSet is null)
            {
                throw new InvalidOperationException($"A value of the value type {netType.FullName} must not be null!");
            }

            if (netType.IsEnum)
            {
                Type targetType = Enum.GetUnderlyingType(netType);
                if (valueToSet is not null) // otherwise valueToSet is already null
                {
                    // Convert Enum to correct underlying type (can be int, uint, etc...)
                    object targetValue = Convert.ChangeType(valueToSet, targetType);

                    // Set Enum as enum type
                    valueToSet = Enum.ToObject(netType, targetValue);
                }
            }
            else if (valueToSet != null)
            {
                valueToSet = Convert.ChangeType(valueToSet, netType);
            }
        }

        return valueToSet;
    }
}