using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace FWI2Helper
{
    public class MySqlEntity<T>
        where T : class, new()
    {
        private readonly MySqlConnection _connection;

        public T Entity { get; private set; }

        public string TableName
        { get { return this.Mapping.TableName; } }

        public SqlEntityMapping<T> Mapping { get; }

        internal MySqlEntity(T entity, SqlEntityMapping<T> dbMapping, MySqlConnection con)
        {
            _connection = con;

            this.Entity = entity;
            this.Mapping = dbMapping;
        }

        public void Create()
        {
            _connection.Open();

            try
            {
                string fields = $"{this.Mapping.Fields.Select(m => m.DbColumnName).ToCommaSeparatedString()}";
                string values = $"{this.Mapping.Fields.Select(m => "@" + m.DbColumnName).ToCommaSeparatedString()}";

                MySqlCommand cmd = new($"INSERT INTO {this.TableName} ({fields}) VALUES ({values})", _connection);
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
                    MySqlCommand cmd2 = new("SELECT LAST_INSERT_ID();", _connection);
                    object id = cmd2.ExecuteScalar();

                    this.Mapping.PrimaryKey?.SetNetValue(this.Entity, id);
                }
            }
            finally
            {
                _connection.Close();
            }
        }

        public void Update()
        {
            if (this.Mapping.PrimaryKey == null) { throw new NotSupportedException("No Primary Key Set! Update is currently only possible if there is a primary key!"); }

            _connection.Open();

            try
            {
                string setFields = "";
                foreach (var curMappingField in this.Mapping.Fields)
                {
                    if (!String.IsNullOrEmpty(setFields)) { setFields += ",\r\n"; }
                    setFields += $"{curMappingField.DbColumnName} = @{curMappingField.DbColumnName}";
                }

                MySqlCommand cmd = new($"UPDATE {this.TableName} SET {setFields} WHERE {this.Mapping.PrimaryKey.DbColumnName} = @id", _connection);

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
            finally
            {
                _connection.Close();
            }
        }

        public void Delete()
        {
            if (this.Mapping.PrimaryKey == null) { throw new NotSupportedException("No Primary Key Set! Update is currently only possible if there is a primary key!"); }

            _connection.Open();

            try
            {
                MySqlCommand cmd = new($"DELETE FROM {this.TableName} WHERE {this.Mapping.PrimaryKey.DbColumnName} = @id", _connection);

                cmd.Parameters.Add("@id", this.Mapping.PrimaryKey.DbType);
                cmd.Parameters["@id"].Value = this.Mapping.PrimaryKey.GetDBValue(this.Entity);

                cmd.ExecuteNonQuery();
            }
            finally
            {
                _connection.Close();
            }
        }
    }

    public class MySqlEntityFactory<T>
        where T : class, new()
    {
        private MySqlConnection _connection;

        public SqlEntityMapping<T> Mapping { get; }

        public MySqlEntityFactory(MySqlConnection con, string tableName)
        {
            _connection = con;
            this.Mapping = new(tableName);
        }

        public MySqlEntityFactory(SqlEntityMapping<T> mapping, MySqlConnection con)
        {
            _connection = con;
            this.Mapping = mapping;
        }

        public T GetEntityById<TPrimaryKey>(TPrimaryKey id)
        {
            if (this.Mapping.PrimaryKey == null) { throw new NotSupportedException("No Primary Key Set! Update is currently only possible if there is a primary key!"); }

            try
            {
                _connection.Open();

                string dbColumns = $"{this.Mapping.Fields.Select(m => m.DbColumnName).ToCommaSeparatedString()}";

                MySqlCommand cmd = new($"SELECT {dbColumns} FROM {this.Mapping.TableName} WHERE {this.Mapping.PrimaryKey.DbColumnName} = @id", _connection);

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
            finally
            {
                _connection.Close();
            }
        }

        public MySqlEntity<T> GetById<TPrimaryKey>(TPrimaryKey id)
        {
            return this.FromEntity(this.GetEntityById(id));
        }

        public MySqlEntity<T> FromEntity(T entity)
        {
            return new MySqlEntity<T>(entity, this.Mapping, _connection);
        }

        public IEnumerable<T> GetAll()
        {
            // TODO
            yield break;
        }

        public SqlEntityMapping<T> CreateDefaultMapping()
        {
            foreach (PropertyInfo curProperty in typeof(T).GetProperties())
            {
                SqlEntityFieldMapping<T> mapping = new();

                mapping.DotNetType = curProperty.PropertyType;
                mapping.ClassPropertyName = curProperty.Name;
                mapping.DbColumnName = curProperty.Name.ToLower();
                mapping.DbType = SqlEntityFieldMapping<T>.GetDefaultDbTypeFromNetType(curProperty.PropertyType);

                this.Mapping.AddField(mapping);
            }

            return this.Mapping;
        }

        public SqlEntityMapping<T> CreateMapping()
        {
            return this.Mapping;
        }
    }

    public class SqlEntityMapping<T>
        where T : class
    {
        private List<SqlEntityFieldMapping<T>> _fields = new();

        public SqlEntityFieldMapping<T>? PrimaryKey { get; set; }

        public ReadOnlyCollection<SqlEntityFieldMapping<T>> Fields { get; }

        public string TableName { get; internal set; } = "defaultTable";

        public SqlEntityMapping()
        {
            this.Fields = new(_fields);
        }

        public SqlEntityMapping(string tableName)
            : this()
        {
            this.TableName = tableName;
        }

        internal SqlEntityMapping<T> AddField(SqlEntityFieldMapping<T> field)
        {
            _fields.Add(field);
            return this;
        }

        public SqlEntityMapping<T> AddField(Expression<Func<T, object>> expression, string dbColumnName, MySqlDbType? dbType = null)
        {
            _fields.Add(new SqlEntityFieldMapping<T>(expression, dbColumnName, dbType));
            return this;
        }

        public SqlEntityMapping<T> AddPrimaryKey(Expression<Func<T, object>> expression, string dbColumnName, MySqlDbType? dbType = null)
        {
            if (this.PrimaryKey != null) { throw new InvalidOperationException("Primary key already set!"); }

            var field = new SqlEntityFieldMapping<T>(expression, dbColumnName, dbType);

            this.PrimaryKey = field;
            _fields.Add(field);
            return this;
        }
    }

    public class SqlEntityFieldMapping<T>
        where T : class
    {
        public SqlEntityFieldMapping()
        {
        }

        public SqlEntityFieldMapping(Expression<Func<T, object>> expression, string dbColumnName, MySqlDbType? dbType = null)
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

            // If types not identical, try to convert
            if (value?.GetType() != propInfo.PropertyType)
            {
                if (propInfo.PropertyType.IsEnum)
                {
                    if (value == null) { throw new InvalidOperationException("Enum value must not be null!"); }

                    // Convert Enum to correct underlying type (can be int, uint, etc...)
                    Type targetType = Enum.GetUnderlyingType(propInfo.PropertyType);
                    object targetValue;

                    targetValue = Convert.ChangeType(value, targetType);

                    // Set Enum as enum type
                    valueToSet = Enum.ToObject(propInfo.PropertyType, targetValue);
                }
                else
                {
                    valueToSet = Convert.ChangeType(value, propInfo.PropertyType);
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