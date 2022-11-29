using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace FWI2Helper
{
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
            using(var con = _connectionFactory())
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

                // Set ID of Entity from DB
                if (this.Mapping.PrimaryKey != null)
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

            using(var con = _connectionFactory())
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

            using(var con = _connectionFactory())
            {
                con.Open();

                MySqlCommand cmd = new($"DELETE FROM {this.TableName} WHERE {this.Mapping.PrimaryKey.DbColumnName} = @id", con);

                cmd.Parameters.Add("@id", this.Mapping.PrimaryKey.DbType);
                cmd.Parameters["@id"].Value = this.Mapping.PrimaryKey.GetDBValue(this.Entity);

                cmd.ExecuteNonQuery();
            }
        }
    }

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
            if (this.Mapping.PrimaryKey == null) { throw new NotSupportedException("No Primary Key Set! Update is currently only possible if there is a primary key!"); }

            using(var con = _connectionFactory())
            {
                con.Open();

                string dbColumns = $"{this.Mapping.Fields.Select(m => m.DbColumnName).ToCommaSeparatedString()}";

                MySqlCommand cmd = new($"SELECT {dbColumns} FROM {this.Mapping.TableName} WHERE {this.Mapping.PrimaryKey.DbColumnName} = @id", con);

                cmd.Parameters.Add("@id", this.Mapping.PrimaryKey.DbType);
                cmd.Parameters["@id"].Value = id;

                var rdr = cmd.ExecuteReader();
                if (!rdr.Read())
                {
                    throw new Exception($"ID '{id}' not found in {this.Mapping.TableName}!");
                }

                T newEntity = new();
                foreach (var curMapping in this.Mapping.Fields)
                {
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
                using(var con = _connectionFactory())
                {
                    con.Open();

                    string dbColumns = $"{this.Mapping.Fields.Select(m => m.DbColumnName).ToCommaSeparatedString()}";

                    MySqlCommand cmd = new($"SELECT {dbColumns} FROM {this.Mapping.TableName}", con);

                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        T newEntity = new();
                        foreach (var curMapping in this.Mapping.Fields)
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

    public class MySqlQuery<T> : IQueryable<T>
    {
        public Type ElementType => throw new NotImplementedException();

        public Expression Expression => throw new NotImplementedException();

        public IQueryProvider Provider => throw new NotImplementedException();

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class MySqlEntityMapping<T>
        where T : class
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
    }

    public class MySqlEntityFieldMapping<T>
        where T : class
    {
        public MySqlEntityFieldMapping()
        {
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

        /// <summary>
        /// Gets the value for the Database
        /// </summary>
        internal object? GetDBValue(T entity)
        {
            var propInfo = typeof(T).GetProperty(this.ClassPropertyName);
            if (propInfo == null) { throw new InvalidOperationException($"Property '{this.ClassPropertyName}' not found on class '{typeof(T).FullName}'"); }

            object? value = propInfo.GetValue(entity);
            if (propInfo.PropertyType.IsEnum)
            {
                value = value is null ? null : (int)value;
            }

            return value;
        }

        internal void SetNetValue(T entity, object? value)
        {
            var propInfo = typeof(T).GetProperty(this.ClassPropertyName);
            if (propInfo == null) { throw new InvalidOperationException($"Property '{this.ClassPropertyName}' not found on class '{typeof(T).FullName}'"); }

            object? valueToSet = value;
            if(value is DBNull) { valueToSet = null; }

            // If types not identical, try to convert
            Type netType = propInfo.PropertyType;
            if (valueToSet?.GetType() != netType)
            {
                bool isNullable = netType.IsGenericType && !netType.IsGenericTypeDefinition && netType.GetGenericTypeDefinition() == typeof(Nullable<>);
                if(isNullable)
                {
                    netType = netType.GenericTypeArguments[0];
                }
            
                // Handle Nullable -> If not nullable, null is not allowed on value types
                if(!isNullable && netType.IsValueType && valueToSet is null) 
                {
                    throw new InvalidOperationException($"A value of the value type {netType.FullName} must not be null!"); 
                }

                if (netType.IsEnum)
                {

                    Type targetType = Enum.GetUnderlyingType(netType);
                    if(valueToSet is not null) // otherwise valueToSet is already null
                    {
                        // Convert Enum to correct underlying type (can be int, uint, etc...)
                        object targetValue = Convert.ChangeType(valueToSet, targetType);

                        // Set Enum as enum type
                        valueToSet = Enum.ToObject(netType, targetValue);
                    }
                    
                }
                else if(valueToSet != null)
                {
                    valueToSet = Convert.ChangeType(valueToSet, netType);
                }
            }

            typeof(T).GetProperty(this.ClassPropertyName)?.SetValue(entity, valueToSet);
        }

        public string DbColumnName { get; set; } = "";

        public string ClassPropertyName { get; set; } = "";

        public MySqlDbType DbType { get; set; }

        public Type DotNetType { get; set; } = typeof(void);
    }
}