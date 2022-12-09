using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace FWI2Helper.Database;

public class MySqlEntity<T>
    where T : class, new()
{
    private readonly Func<MySqlConnection> _connectionFactory;

    public T Entity { get; private set; }

    public string TableName
    {
        get { return this.Mapping.TableName; }
    }

    public MySqlEntityMapping<T> Mapping { get; }

    internal MySqlEntity(T entity, MySqlEntityMapping<T> dbMapping, Func<MySqlConnection> conFact)
    {
        _connectionFactory = conFact;

        this.Entity = entity;
        this.Mapping = dbMapping;
    }

    public void Create()
    {
        this.Create<object>(null);
    }

    internal void Create<TForeignEntity>(MySqlEntityCreationContext<TForeignEntity>? context)
        where TForeignEntity : class, new()
    {
        using (var con = _connectionFactory())
        {
            con.Open();

            var cmd = GetInsertCommand<TForeignEntity>(con, context);
            cmd.ExecuteNonQuery();

            // Set ID of Entity from DB - If ID not already set...
            if (this.Mapping.PrimaryKey != null &&
                this.Mapping.PrimaryKey.GetDBValue(this.Entity)?.Equals(0) == true)
            {
                MySqlCommand cmd2 = new("SELECT LAST_INSERT_ID();", con);
                object id = cmd2.ExecuteScalar();

                if (id.Equals(0)) { throw new NotSupportedException("ID is 0. Maybe AUTO_INCREAMENT not set. Currently only tables with auto increament supported!"); }

                this.Mapping.PrimaryKey?.SetNetValue(this.Entity, id);
            }

            // All other inserts need to be done after the id was retrieved
            this.Insert1ListForeignEntites();
        }
    }

    /// <summary>
    /// Inserts assigned ForeignKeys which are assigned as Collection on this entity 
    /// </summary>
    private void Insert1ListForeignEntites()
    {
        foreach (MySqlEntityFieldMapping<T> curMappingField in this.Mapping.Fields)
        {
            if (curMappingField is MySqlEntityFieldMappingForeignKey<T> foreignKeyMapping)
            {
                foreignKeyMapping.EnsureForeignEntitesCreated(this.Entity);
            }
        }
    }

    /// <summary>
    /// Updates assigned ForeignKeys which are assigned as Collection on this entity 
    /// </summary>
    private void Update1ListForeignEntites()
    {
        foreach (MySqlEntityFieldMapping<T> curMappingField in this.Mapping.Fields)
        {
            if (curMappingField is MySqlEntityFieldMappingForeignKey<T> foreignKeyMapping)
            {
                foreignKeyMapping.EnsureForeignEntitesUpdated(this.Entity);
            }
        }
    }

    private MySqlCommand GetInsertCommand<TForeignEntity>(MySqlConnection con, MySqlEntityCreationContext<TForeignEntity>? context)
        where TForeignEntity : class, new()
    {
        var mappings = this.Mapping.Fields
                        .Where(field => field is not MySqlEntityFieldMappingForeignKey<T> foreignKeyMapping ||
                                    foreignKeyMapping.MapType != ForeignKeyMapType.Side1List);

        string fields = $"{mappings.Select(m => m.DbColumnName).ToCommaSeparatedString()}";
        string values = $"{mappings.Select(m => "@" + m.DbColumnName).ToCommaSeparatedString()}";
        MySqlCommand cmd = new($"INSERT INTO {this.TableName} ({fields}) VALUES ({values})", con);

        foreach (MySqlEntityFieldMapping<T> curMappingField in this.Mapping.Fields)
        {
            string parmName = $"@{curMappingField.DbColumnName}";
            if (curMappingField is MySqlEntityFieldMappingForeignKey<T> foreignKeyMapping)
            {
                switch (foreignKeyMapping.MapType)
                {
                    case ForeignKeyMapType.Side1List:
                        // Done in Insert1ListForeignEntites()
                        break;

                    case ForeignKeyMapType.SideNListImport:
                        if (context == null) { throw new InvalidOperationException("Individual Insert of this Entity needs an Context with the foreign entity!"); }
                        cmd.Parameters.Add(parmName, curMappingField.DbType);
                        cmd.Parameters[parmName].Value = context.GetForeignEntityPrimaryKey();
                        break;

                    case ForeignKeyMapType.SideNProperty:
                        cmd.Parameters.Add(parmName, curMappingField.DbType);
                        cmd.Parameters[parmName].Value = foreignKeyMapping.GetDBValue(this.Entity);
                        break;

                    default:
                        throw new InvalidOperationException("Unknown Mapping Type!");
                }
            }
            else
            {
                cmd.Parameters.Add(parmName, curMappingField.DbType);
                cmd.Parameters[parmName].Value = curMappingField.GetDBValue(this.Entity);
            }
        }

        return cmd;
    }

    public void Update()
    {
        if (this.Mapping.PrimaryKey == null) { throw new NotSupportedException("No Primary Key Set! Update is currently only possible if there is a primary key!"); }

        this.Update1ListForeignEntites();

        using (var con = _connectionFactory())
        {
            con.Open();

            string setFields = "";
            foreach (var curMappingField in this.Mapping.Fields)
            {
                if(curMappingField is MySqlEntityFieldMappingForeignKey<T> foreignKeyMapping)
                {
                    switch (foreignKeyMapping.MapType)
                    {
                        case ForeignKeyMapType.Side1List:
                        case ForeignKeyMapType.SideNListImport:
                            continue;

                        case ForeignKeyMapType.SideNProperty:
                            break;
                            
                        default:
                            throw new InvalidOperationException("Unknown Mapping Type!");
                    }
                }

                if (!String.IsNullOrEmpty(setFields)) { setFields += ",\r\n"; }
                setFields += $"{curMappingField.DbColumnName} = @{curMappingField.DbColumnName}";
            }

            MySqlCommand cmd = new($"UPDATE {this.TableName} SET {setFields} WHERE {this.Mapping.PrimaryKey.DbColumnName} = @id", con);

            cmd.Parameters.Add("@id", this.Mapping.PrimaryKey.DbType);
            cmd.Parameters["@id"].Value = this.Mapping.PrimaryKey.GetDBValue(this.Entity);

            foreach (var curMappingField in this.Mapping.Fields)
            {
                if(curMappingField is MySqlEntityFieldMappingForeignKey<T> foreignKeyMapping)
                {
                    switch (foreignKeyMapping.MapType)
                    {
                        // Do not Update Foreign Key on Collection Properties - These will be removed or added 
                        case ForeignKeyMapType.Side1List:
                        case ForeignKeyMapType.SideNListImport:
                            continue;

                        case ForeignKeyMapType.SideNProperty:
                            break;
                            
                        default:
                            throw new InvalidOperationException("Unknown Mapping Type!");

                    }
                }

                // Ignore Primary Keys - This should not be changed
                if (curMappingField == this.Mapping.PrimaryKey) { continue; }

                string parmName = $"@{curMappingField.DbColumnName}";
                cmd.Parameters.Add(parmName, curMappingField.DbType);
                cmd.Parameters[parmName].Value = curMappingField.GetDBValue(this.Entity);
            }

            cmd.ExecuteNonQuery();
        }
    }

    public void Delete()
    {
        if (this.Mapping.PrimaryKey == null) { throw new NotSupportedException("No Primary Key Set! Update is currently only possible if there is a primary key!"); }

        // Check if entity has any foreign associations
        foreach (var curMapping in this.Mapping.Fields)
        {
            if (curMapping is MySqlEntityFieldMappingForeignKey<T> foreignKeyMapping)
            {
                foreignKeyMapping.EnsureForeignEntitesDeleted(this.Entity);
            }
        }

        using (var con = _connectionFactory())
        {
            con.Open();

            MySqlCommand cmd = new($"DELETE FROM {this.TableName} WHERE {this.Mapping.PrimaryKey.DbColumnName} = @id", con);

            cmd.Parameters.Add("@id", this.Mapping.PrimaryKey.DbType);
            cmd.Parameters["@id"].Value = this.Mapping.PrimaryKey.GetDBValue(this.Entity);

            cmd.ExecuteNonQuery();
        }
    }
}

public class MySqlEntityCreationContext<TForeignEntity>
    where TForeignEntity : class, new()
{
    public MySqlEntityCreationContext(/*MySqlEntityFieldMappingForeignKey<TForeignEntity> foreignKey,*/ TForeignEntity baseEntity)
    {
        //this.ForeignKey = foreignKey;
        this.ForeignEntity = baseEntity;
    }

    //public MySqlEntityFieldMappingForeignKey<TForeignEntity> ForeignKey { get; }

    public TForeignEntity ForeignEntity { get; }

    public object? GetForeignEntityPrimaryKey()
    {
        return MySqlEntityFieldMappingForeignKey<TForeignEntity>.GetPrimaryKey(this.ForeignEntity);
    }
}

[Flags]
internal enum SQLCommandKind
{
    BaseEntity = 1,
    ChildEntities = 2,

    All = 3,
}