using System;

namespace FWI2HelperTests.ForeignKeyData
{
    public class ModelBase<T, TDB> : IEquatable<T?>
        where T : ModelBase<T, TDB>, new()
        where TDB : DBAccess<T>
    {
        public int Id { get; set; }

        public static T? TryGetById(int id)
        {
            return DBAccess<T>.Instance.TryGetById(id);
        }

        public static T GetById(int id)
        {
            return DBAccess<T>.Instance.GetById(id);
        }
        public static bool operator !=(ModelBase<T, TDB>? left, ModelBase<T, TDB>? right)
        {
            return !(left == right);
        }

        public static bool operator ==(ModelBase<T, TDB>? left, ModelBase<T, TDB>? right)
        {
            return EqualityComparer<ModelBase<T, TDB>>.Default.Equals(left, right);
        }

        public static IEnumerable<T> ReadAll()
        {
            return DBAccess<T>.Instance.ReadAll();
        }

        public void Create()
        {
            DBAccess<T>.Instance.Create((T)this);
        }

        public void Delete()
        {
            DBAccess<T>.Instance.Delete((T)this);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as T);
        }

        public bool Equals(T? other)
        {
            return other is not null &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public void Update()
        {
            DBAccess<T>.Instance.Update((T)this);
        }
    }
}