using System.Linq.Expressions;
using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace MagicVilla_VillaAPI.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;

        public Repository(ApplicationDbContext db)
        {
            _db = db;
            //_db.VillaNumbers.Include(v => v.Villa).ToList(); // Include the Villa navigation property when querying VillaNumbers (to get villa info also)
            this.dbSet = _db.Set<T>(); // Set the dbSet to the type of T (automaticly the dbset will be based on the entity we pass)
        }

        // Create a new Villa and save it to the database
        public async Task CreateAsync(T entity)
        {
            await dbSet.AddAsync(entity);
            await SaveAsync(); // Save changes to the database (custom method I have made)
        }

        public async Task<T> GetAsync(Expression<Func<T, bool>> filter = null, bool tracked = true, string? includeProperties = null)
        {
            IQueryable<T> query = dbSet; // Get all villas from the database
            if (!tracked) // If not tracked, use AsNoTracking to avoid tracking changes
            {
                query = query.AsNoTracking();
            }
            if (filter != null) // If a filter is provided, apply it to the query
            {
                query = query.Where(filter); // Apply the filter to the query
            }
            if (includeProperties != null)
            {
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }
            return await query.FirstOrDefaultAsync(); // Execute the query and return the first villa that matches the filter
        }

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null,
           int pageSize = 0, int pageNumber = 1)
        {
            IQueryable<T> query = dbSet; // Get all villas from the database
            if (filter != null) // If a filter is provided, apply it to the query
            {
                query = query.Where(filter); // Apply the filter to the query
            }
            if (pageSize > 0)
            {
                if (pageSize > 100)
                {
                    pageSize = 100; // Limit the page size to a maximum of 100
                }
                query = query.Skip(pageSize * (pageNumber - 1)).Take(pageSize); // Apply pagination to the query
            }
            if (includeProperties != null)
            {
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }
            return await query.ToListAsync(); // Execute the query and return the list of villas
        }

        public async Task RemoveAsync(T entity)
        {
            dbSet.Remove(entity);
            await SaveAsync();
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}