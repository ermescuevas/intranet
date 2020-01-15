using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Data
{
    public interface IRepository : IDisposable
    {
        T GetBy<T>(object id) where T : class;
        IEnumerable<T> GetAll<T>(System.Linq.Expressions.Expression<Func<T, bool>> filter = null,
                          Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
                          string includeProperties = "",
                          int top = 0) where T : class;

        T ExecuteScalarQuery<T>(string query);
        IEnumerable<T> ExecuteQuery<T>(string query);
        void ExecuteCommand(string command);
        bool Add<T>(T t) where T : class;
        bool Update<T>(T t, object id) where T : class;
        bool Delete<T>(object id) where T : class;
        bool Delete<T>(T entityToDelete) where T : class;
    }
}
