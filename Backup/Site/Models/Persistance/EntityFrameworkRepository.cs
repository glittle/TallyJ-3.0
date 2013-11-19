using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Entity;
using System.Linq;

namespace TallyJ.Models.Persistance
{
    public class EntityFrameworkRepository : IRepository, IDisposable
    {
        private readonly ConcurrentDictionary<Type, object> _dbSets =
            new ConcurrentDictionary<Type, object>();

        private TallyJ2dContext _context;

        public EntityFrameworkRepository(TallyJ2dContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        public T Get<T>(int id) where T : Entity
        {
            return GetDbSet<T>().Find(id);
        }

        public IQueryable<T> GetAll<T>() where T : Entity
        {
            return GetDbSet<T>();
        }

        public void SaveOrUpdate<T>(T entity) where T : Entity
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                GetDbSet<T>().Add(entity);
            }
            else
            {
                // force it to be modified?
                _context.Entry(entity).State = EntityState.Modified;
            }

            _context.SaveChanges();
        }

        public void Delete<T>(T entity) where T : Entity
        {
            GetDbSet<T>().Remove(entity);
            _context.SaveChanges();
        }

        private DbSet<T> GetDbSet<T>() where T : Entity
        {
            return (DbSet<T>) _dbSets.GetOrAdd(typeof (T), x => _context.Set<T>());
        }
    }
}