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

            string fields = $"{this.Mapping.Fields.Select(m => m.DbColumnName).ToCommaSeparatedString()}";
            string values = $"{this.Mapping.Fields.Select(m => "@" + m.DbColumnName).ToCommaSeparatedString()}";

            MySqlCommand cmd = new($"INSERT INTO {this.TableName} ({fields}) VALUES ({values})", con);
            foreach (var curMappingField in this.Mapping.Fields)
            {
                string parmName = $"@{curMappingField.DbColumnName}";
                cmd.Parameters.Add(parmName, curMappingField.DbType);
                cmd.Parameters[parmName].Value = curMappingField.GetDBValue(this.Entity);
            }

            cmd.ExecuteNonQuery();

            // Set ID of Entity from DB - If ID not already set...
            if (this.Mapping.PrimaryKey != null &&
                this.Mapping.PrimaryKey.GetDBValue(this.Entity)?.Equals(0) == true)
            {
                MySqlCommand cmd2 = new("SELECT LAST_INSERT_ID();", con);
                object id = cmd2.ExecuteScalar();

                this.Mapping.PrimaryKey?.SetNetValue(this.Entity, id);
            }
        }
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