namespace FWI2Helper.Database;

public class MySqlFactContainer
{
    private static Dictionary<string, MySqlFactContainer> _container = new();

    private Dictionary<Type, MySqlEntityFactory> _factories = new();

    private MySqlFactContainer()
    {
    }

    public void RegisterFactory(MySqlEntityFactory fact)
    {
        if(_factories.ContainsKey(fact.GetEntityType())) { throw new InvalidOperationException("Factory already registered!"); }
        _factories[fact.GetEntityType()] = fact;

    }

    public MySqlEntityFactory GetFactoryForEntity(Type entityType)
    {
        return _factories.GetValueOrDefault(entityType) ?? throw new KeyNotFoundException($"No factory for entityType {entityType.FullName} registered!");
    }

    public MySqlEntityFactory<TEntity> GetFactoryForEntity<TEntity>()
        where TEntity : class, new()
    {
        return ((MySqlEntityFactory<TEntity>)GetFactoryForEntity(typeof(TEntity))) ?? throw new KeyNotFoundException($"No factory for entityType {typeof(TEntity).FullName} registered!");
    }

    public static MySqlFactContainer GetInstance(string key)
    {
        if(!_container.ContainsKey(key)) { _container[key] = new(); }

        return _container[key];
    }

    public static MySqlFactContainer Default 
    {
        get => _container["default"] ??= new();
    }
}