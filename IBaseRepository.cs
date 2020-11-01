using System.Collections.Generic;
using System.Threading.Tasks;

namespace DapperRepo.Base
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T> AddAsync(T toCreate);
        Task<bool> UpdateAsync(T toUpdate);
        Task<bool> UpdateAsync<T2>(T2 toUpdate) where T2 : class;
        Task<bool> DeleteAsync(T toUpdate);
        Task<bool> BulkSaveAsync(IEnumerable<T> dataToSave, bool checkOld = true);
        Task IncrementAsync(long Id, object colums);
        Task<T> GetAsync<T1>(T1 id);
        Task<IEnumerable<T>> GetAsync<T1>(IEnumerable<T1> ids);
        Task<IEnumerable<T>> ListAsync();
        Task<IEnumerable<T>> ListAsync(int limit, string orderBy, string orderSort, int page = 1);
    }
}