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
        using (var con = _connectionFactory())
        {
            con.Open();

            List<MySqlCommand> commands = GetInsertCommands(con, SQLCommandKind.BaseEntity);
            var originalInsert = commands.Single();

            originalInsert.ExecuteNonQuery();

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
            commands = GetInsertCommands(con, SQLCommandKind.ChildEntities);
            for (int i = 0; i < commands.Count; i++)
            {
                commands[i].ExecuteNonQuery();
            }
        }
    }

    internal List<MySqlCommand> GetInsertCommands(MySqlConnection con, SQLCommandKind kind, object? foreignBaseEntityKey = null)
    {
        List<MySqlCommand> subCommands = new List<MySqlCommand>();

        var mappings = this.Mapping.Fields
                        .Where(field => field is not MySqlEntityFieldMappingForeignKey<T> foreignKeyMapping ||
                                    foreignKeyMapping.MapType != ForeignKeyMapType.SideNList);

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
                    case ForeignKeyMapType.SideNList:
                        if (kind.HasFlag(SQLCommandKind.ChildEntities)) { subCommands.AddRange(foreignKeyMapping.HandleInsertReferences(this.Entity, con)); }
                        break;

                    case ForeignKeyMapType.Side1Import:
                        if (foreignBaseEntityKey == null) { throw new InvalidOperationException("Individual Insert of this Entity not allowed!"); }
                        cmd.Parameters.Add(parmName, curMappingField.DbType);
                        cmd.Parameters[parmName].Value = foreignBaseEntityKey;
                        break;

                    case ForeignKeyMapType.Side1Property:
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

        if (kind.HasFlag(SQLCommandKind.BaseEntity))
        {
            subCommands.Insert(0, cmd);
        }

        return subCommands;
    }

    public void Update()
    {
        if (this.Mapping.PrimaryKey == null) { throw new NotSupportedException("No Primary Key Set! Update is currently only possible if there is a primary key!"); }

        using (var con = _connectionFactory())
        {
            con.Open();

            string setFields = "";
            foreach (var curMappingField in this.Mapping.Fields)
            {
                if (!String.IsNullOrEmpty(setFields)) { setFields += ",\r\n"; }
                setFields += $"{curMappingField.DbColumnName} = @{curMappingField.DbColumnName}";
            }

            MySqlCommand cmd = new($"UPDATE {this.TableName} SET {setFields} WHERE {this.Mapping.PrimaryKey.DbColumnName} = @id", con);

            cmd.Parameters.Add("@id", this.Mapping.PrimaryKey.DbType);
            cmd.Parameters["@id"].Value = this.Mapping.PrimaryKey.GetDBValue(this.Entity);

            foreach (var curMappingField in this.Mapping.Fields)
            {
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

[Flags]
internal enum SQLCommandKind
{
    BaseEntity = 1,
    ChildEntities = 2,

    All = 3,
}