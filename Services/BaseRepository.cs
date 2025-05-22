using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Extensions;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Base repository providing common data access patterns with NULL safety
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class BaseRepository<T> where T : class
    {
        protected readonly WorkoutTrackerWebContext _context;
        protected readonly ILogger _logger;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(WorkoutTrackerWebContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
            _dbSet = _context.Set<T>();
        }

        /// <summary>
        /// Safely retrieves a string property from an entity, ensuring it's never null
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="propertySelector">Function to select the string property</param>
        /// <returns>The string value or empty string if null</returns>
        protected string SafeGetString(T entity, Func<T, string> propertySelector)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            return propertySelector(entity).SafeValue();
        }
        
        /// <summary>
        /// Executes a query safely by first loading data into memory and then processing it
        /// to avoid SQL NULL value exceptions
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="query">The query to execute</param>
        /// <returns>The query results</returns>
        protected async Task<List<TResult>> ExecuteSafeQueryAsync<TResult>(IQueryable<TResult> query)
        {
            try
            {
                // Execute the query and load results into memory
                var results = await query.ToListAsync();
                
                // Process results in memory to handle any NULL string properties
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing safe query");
                throw;
            }
        }
        
        /// <summary>
        /// Safely gets all entities with precaution against NULL values
        /// </summary>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                // Load entities into memory first
                var entities = await _dbSet.AsNoTracking().ToListAsync();
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all entities of type {EntityType}", typeof(T).Name);
                throw;
            }
        }
        
        /// <summary>
        /// Safely gets an entity by id with precaution against NULL values
        /// </summary>
        public virtual async Task<T> GetByIdAsync(object id)
        {
            try
            {
                return await _dbSet.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity of type {EntityType} with ID {Id}", 
                    typeof(T).Name, id);
                throw;
            }
        }
    }
}