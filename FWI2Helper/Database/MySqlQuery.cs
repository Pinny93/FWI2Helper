using System.Collections;
using System.Linq.Expressions;

namespace FWI2Helper.Database;

// TO BE IMPLEMENTED...
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
