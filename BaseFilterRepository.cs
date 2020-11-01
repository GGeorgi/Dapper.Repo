using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DapperRepo.Base
{
    public abstract class BaseFilterRepository<T, T1> : BaseRepository<T>, IBaseFilterRepository<T, T1> where T : class where T1 : class
    {
        protected BaseFilterRepository(IDbConnection connection) : base(connection) { }

        public virtual async Task<T> GetAsync(T1 filter)
        {
            var data = await FindAsync(filter);
            return data.FirstOrDefault();
        }

        public abstract Task<IEnumerable<T>> FindAsync(T1 filter, string[] returnColumns = null);
    }
}
