using System.Collections.Generic;
using System.Threading.Tasks;

namespace DapperRepo.Base
{
    public interface IBaseFilterRepository<T, in T1> : IBaseRepository<T> where T : class where T1 : class
    {
        Task<T> GetAsync(T1 filter);
        Task<IEnumerable<T>> FindAsync(T1 filter, string[] returnColumns = null);
    }
}