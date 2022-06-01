using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {

        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;

        public Repository(ApplicationDbContext db)
        {
            _db = db;
            this.dbSet = _db.Set<T>();
        }

        public void Add(T entity)
        {
            dbSet.Add(entity);
        }

        public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter=null, string? IncludeStatements = null)
        {
            IQueryable<T> query = dbSet;
            if(filter != null)
            {
                query = query.Where(filter);
            }
            if (IncludeStatements != null)
            {
                foreach(string statement in IncludeStatements.Split(",", StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(statement);
                }
            }
            return query.ToList();
        }

        public T GetFirstOrDefault(Expression<Func<T, bool>> filter, string? IncludeStatements = null, bool tracked = true)
        {
            IQueryable<T> query;
			if (tracked)
			{
                query = dbSet;
			}
			else
			{
                query = dbSet.AsNoTracking();
			}

            if (IncludeStatements != null)
            {
                foreach (string statement in IncludeStatements.Split(",", StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(statement);
                }
            }
            query = query.Where(filter);
            return query.FirstOrDefault();
        }

        public void Remove(T entity)
        {
            dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            dbSet.RemoveRange(entities);
        }
    }
}
