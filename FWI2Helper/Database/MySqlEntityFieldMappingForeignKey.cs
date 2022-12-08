using System.Linq.Expressions;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace FWI2Helper.Database;

public abstract class MySqlEntityFieldMappingForeignKey<TEntity> : MySqlEntityFieldMapping<TEntity>
    where TEntity : class, new()
{
    public string ForeignTableName { get; init; }

    public ForeignKeyMapType MapType { get; init; }

    public Type ForeignKeyNetType { get; init; }

    public MySqlEntityFieldMappingForeignKey(string classPropertyName, Type netType, string dbColumnName, string dbForeignTableName, ForeignKeyMapType mapType, Type foreignKeyNetType, MySqlDbType? dbType = null)
        : base(classPropertyName, netType, dbColumnName, dbType)
    {
        this.ForeignTableName = dbForeignTableName;
        this.MapType = mapType;
        this.ForeignKeyNetType = foreignKeyNetType;
    }

    public abstract void ResolveNetEntityById(TEntity entity, object id);

    public abstract void ResolveNetEntitiesById(TEntity entity);

    public abstract IEnumerable<MySqlCommand> HandleInsertReferences(TEntity entity, MySqlConnection con);

    public override string ToString()
    {
        return $"FieldMappingForeignKey '{this.ClassPropertyName}' ({this.DotNetType.FullName}) --> '{this.ForeignTableName}.{this.DbColumnName}' ({this.DbType})";
    }
}

public class MySqlEntityFieldMappingForeignKey<TEntity, TForeignEntity> : MySqlEntityFieldMappingForeignKey<TEntity>
    where TEntity : class, new()
    where TForeignEntity : class, new()
{
    public MySqlEntityFieldMappingForeignKey(Expression<Func<TEntity, TForeignEntity?>> expression, string dbForeignTableName, string dbColumnName, MySqlDbType? dbType = null)
        : base(null!, null!, dbColumnName, dbForeignTableName, ForeignKeyMapType.Side1Property, typeof(TForeignEntity), dbType)
    {
        var declInfo = this.GetFromExpression(expression);
        //this.DotNetType = this.GetForeignKeyType();

        this.ClassPropertyName = declInfo.propertyName;
    }

    public MySqlEntityFieldMappingForeignKey(Expression<Func<TEntity, IEnumerable<TForeignEntity>>> expression, string dbForeignTableName, string dbColumnName, MySqlDbType? dbType = null)
        : base(null!, null!, dbColumnName, dbForeignTableName, ForeignKeyMapType.SideNList, null!, dbType)
    {
        var declInfo = this.GetFromExpression(expression);
        //this.DotNetType = this.GetForeignKeyType();

        this.ClassPropertyName = declInfo.propertyName;
    }

    public MySqlEntityFieldMappingForeignKey(Expression<Func<TForeignEntity, IEnumerable<TEntity>>> expression, string dbForeignTableName, string dbColumnName, MySqlDbType? dbType = null)
        : base(null!, null!, dbColumnName, dbForeignTableName, ForeignKeyMapType.Side1Import, typeof(TForeignEntity), dbType)
    {
        var declInfo = this.GetFromExpression(expression);
        //this.DotNetType = this.GetForeignKeyType();

        this.ClassPropertyName = declInfo.propertyName;
    }

    private (string propertyName, Type netType) GetFromExpression(LambdaExpression expression)
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

        if (member.DeclaringType == null) { throw new Exception("Declaring Type not found!"); }

        return (member.Name, member.DeclaringType);
    }

    private Type GetForeignKeyType()
    {
        var foreignMapping = MySqlFactContainer.Default
                                .GetFactoryForEntity<TForeignEntity>()
                                .Mapping.PrimaryKey;

        if (foreignMapping == null) { throw new Exception($"Mapping of Foreign Entity '{typeof(TForeignEntity).FullName}' has no Primary Key defined!"); }

        return foreignMapping.DotNetType;
    }

    private Type GetEnumerableType(Type enumerableType)
    {
        if (!enumerableType.IsGenericType || enumerableType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
        {
            throw new ArgumentException("Type is no IEnumerable<>!");
        }

        return enumerableType.GetGenericArguments()[0];
    }

    internal override object? GetDBValue(TEntity entity)
    {
        switch (this.MapType)
        {
            case ForeignKeyMapType.SideNList:
                throw new NotSupportedException("You cannot resolve a List of Enities to one DB value...");

            case ForeignKeyMapType.Side1Property:
                TForeignEntity? foreignEntity = base.GetNetValue(entity) as TForeignEntity;
                return GetForeignEntityPrimaryKey(foreignEntity);

            case ForeignKeyMapType.Side1Import:
                throw new NotSupportedException("No Net Entity for this Mapping...");
        }

        throw new InvalidOperationException("Unknown Map type!");
    }

    public override void ResolveNetEntityById(TEntity entity, object foreignId)
    {
        if (this.MapType != ForeignKeyMapType.Side1Property) { throw new Exception("Not allowed for this MapType!"); }

        TForeignEntity? foreignEntity = MySqlFactContainer.Default
                                            .GetFactoryForEntity<TForeignEntity>()
                                            .TryGetEntityById(foreignId);

        this.SetNetValue(entity, foreignEntity);
    }

    public override IEnumerable<MySqlCommand> HandleInsertReferences(TEntity entity, MySqlConnection con)
    {
        List<MySqlCommand> commands = new List<MySqlCommand>();

        IEnumerable<TForeignEntity>? list = this.GetNetValue(entity) as IEnumerable<TForeignEntity>;
        if (list == null) { throw new ArgumentException("Foreign Entity List is null!"); }

        var entityPrimaryKey = GetPrimaryKey(entity);
        var foreignFactory = MySqlFactContainer.Default.GetFactoryForEntity<TForeignEntity>();
        foreach (TForeignEntity foreignEntity in list)
        {
            var dbEntity = foreignFactory.TryGetEntityById(GetForeignEntityPrimaryKey(foreignEntity));
            if (dbEntity != null) { continue; }

            commands.AddRange(foreignFactory.FromEntity(foreignEntity).GetInsertCommands(con, SQLCommandKind.All, entityPrimaryKey));
        }

        return commands;
    }

    /// <summary>
    /// Resolves referenced Entities from Database
    /// </summary>
    /// <param name="entity"></param>
    /// <exception cref="Exception"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public override void ResolveNetEntitiesById(TEntity entity)
    {
        if (this.MapType != ForeignKeyMapType.SideNList) { throw new Exception("Not allowed for this MapType!"); }
        object entityPrimaryKey = GetPrimaryKey(entity);

        IEnumerable<TForeignEntity> foreignEntities = MySqlFactContainer.Default
                                                        .GetFactoryForEntity<TForeignEntity>()
                                                        .GetAll()
                                                        .Where(foreignEntity => entityPrimaryKey.Equals(GetForeignEntityPrimaryKey(foreignEntity)));

        foreach (TForeignEntity curEntity in foreignEntities)
        {
            this.AddNetValueToCollection(entity, curEntity);
        }
    }

    private static object GetPrimaryKey(TEntity entity)
    {
        return MySqlFactContainer.Default
                    .GetFactoryForEntity<TEntity>()
                    .Mapping.PrimaryKey?.GetNetValue(entity) ?? throw new InvalidOperationException("Primary Key must not be null!");
    }

    /// <summary>
    /// Resolves the given referenced Entity from Database
    /// </summary>
    /// <param name="foreignEntity"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static object? GetForeignEntityPrimaryKey(TForeignEntity? foreignEntity)
    {
        if (foreignEntity == null) { return null; }

        var foreignMapping = MySqlFactContainer.Default
                                        .GetFactoryForEntity<TForeignEntity>()
                                        .Mapping.PrimaryKey;

        if (foreignMapping == null) { throw new InvalidOperationException("Foreign Primary Key not found!"); }

        return foreignMapping.GetNetValue(foreignEntity);
    }
}

public enum ForeignKeyMapType
{
    SideNList,
    Side1Property,
    Side1Import
}