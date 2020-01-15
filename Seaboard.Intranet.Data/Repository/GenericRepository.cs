using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Seaboard.Intranet.Data.Repository
{
    public class GenericRepository : IRepository
    {
        private readonly DbContext _context;

        public GenericRepository(DbContext contex)
        {
            _context = contex;
        }

        public T GetBy<T>(object id) where T : class
        {
            return _context.Set<T>().Find(id);
        }

        public IEnumerable<T> GetAll<T>(System.Linq.Expressions.Expression<Func<T, bool>> filter = null,
                                 Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
                                 string includeProperties = "", int top = 0) where T : class
        {
            IQueryable<T> query = _context.Set<T>();

            if (filter != null)
                query = query.Where(filter);
            

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }

            if (top > 0)
                query = query.Take(top);

            return orderBy != null ? orderBy(query).ToList() : query.ToList();
        }

        public bool Add<T>(T t) where T : class
        {
            _context.Set<T>().Add(t);
            return _context.SaveChanges() != 0;
        }

        public bool Update<T>(T t, object id) where T : class
        {
            var entityToUpdate = _context.Set<T>().Find(id);
            _context.Entry(entityToUpdate).CurrentValues.SetValues(t);
            return _context.SaveChanges() != 0;
        }

        public virtual bool Delete<T>(object id) where T : class
        {
            var entityToDelete = GetBy<T>(id);
            return Delete(entityToDelete);
        }

        public virtual bool Delete<T>(T entityToDelete) where T : class
        {
            if (_context.Entry(entityToDelete).State == EntityState.Detached)
                _context.Set<T>().Attach(entityToDelete);
            _context.Set<T>().Remove(entityToDelete);
            return _context.SaveChanges() != 0;
        }

        public T ExecuteScalarQuery<T>(string query)
        {
            return ExecuteQuery<T>(query).FirstOrDefault();
        }

        public IEnumerable<T> ExecuteQuery<T>(string query)
        {
            var result = _context.Database.SqlQuery<T>(query).ToList();
            return result;
        }

        public void ExecuteCommand(string command)
        {
            _context.Database.ExecuteSqlCommand(command);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            _context?.Dispose();
        }
    }
}