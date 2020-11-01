using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Linq;
using DapperRepo.Utils;
using Dapper;

namespace DapperRepo.Base
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly IDbConnection _connection;

        public BaseRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public virtual async Task<T> AddAsync(T toCreate)
        {
            var id = await _connection.InsertAsync(toCreate);
            var propertyInfo = toCreate.GetType().GetProperty("Id");
            if (propertyInfo != null && !(propertyInfo.GetValue(toCreate) is Guid))
                propertyInfo.SetValue(toCreate, Convert.ChangeType(id, propertyInfo.PropertyType), null);
            return toCreate;
        }

        public virtual async Task<bool> UpdateAsync(T toUpdate)
        {
            return await _connection.UpdateAsync(toUpdate);
        }

        public virtual async Task<bool> UpdateAsync<T2>(T2 toUpdate) where T2 : class
        {
            return await _connection.UpdateAsync(toUpdate);
        }

        public virtual async Task<bool> BulkSaveAsync(IEnumerable<T> dataToSave, bool checkOld = true)
        {
            var somethingChanged = false;
            var chunks = dataToSave.ToList().ToChunks(2000);
            foreach (var chunk in chunks)
            {
                if (await BatchSave(chunk, checkOld)) somethingChanged = true;
            }

            return somethingChanged;
        }

        private async Task<bool> BatchSave(IEnumerable<T> dataToSave, bool checkOld)
        {
            try
            {
                var MyAttribute = (TableAttribute) Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
                var tableName = MyAttribute.Name;
                if (checkOld)
                {
                    var builder = new SqlBuilder();
                    var selector = builder.AddTemplate($"SELECT * FROM {tableName} /**where**/");
                    // filter by Id
                    builder.Where("Id IN @Ids", new {Ids = dataToSave.Select(GetId)});
                    // filter by Name
                    var oldData = await _connection.QueryAsync<T>(selector.RawSql, selector.Parameters);
                    var missingData = dataToSave.Where(x => oldData.All(y => !EqualById(x, y)));
                    if (!missingData.Any()) return false;
                    await SaveBulk(tableName, missingData);
                    return true;
                }

                await SaveBulk(tableName, dataToSave);
                return true;
            }
            catch (SqlException ex)
            {
                if (ex.Number != 2627) throw;
                Console.WriteLine(ex.Message);
                var res = await BatchSave(dataToSave, checkOld);
                Console.WriteLine($"[Resolved] {ex.Message}");
                return res;
            }
        }

        private async Task SaveBulk(string tableName, IEnumerable<T> data)
        {
            var dataTable = new DataTable(tableName);
            InitializeDataTableStructure(dataTable);
            // transfer data to the datatable
            foreach (var rec in data)
            {
                PopulateDataTable(rec, dataTable);
            }

            await WriteToDatabaseAsync(dataTable);
        }

        private static bool EqualById(T x, T y)
        {
            return GetId(x).Equals(GetId(y));
        }

        private static object GetId(T d)
        {
            return d.GetType().GetProperty("Id")?.GetValue(d);
        }

        protected virtual void InitializeDataTableStructure(DataTable dataTable)
        {
        }

        protected virtual void PopulateDataTable(T record, DataTable dataTable)
        {
        }


        private async Task WriteToDatabaseAsync(DataTable dataTable)
        {
            await using var connection = new SqlConnection(_connection.ConnectionString);
            var bulkCopy = new SqlBulkCopy(
                connection,
                SqlBulkCopyOptions.TableLock |
                SqlBulkCopyOptions.FireTriggers |
                SqlBulkCopyOptions.UseInternalTransaction,
                null)
            {
                DestinationTableName = dataTable.TableName
            };

            await connection.OpenAsync();
            await bulkCopy.WriteToServerAsync(dataTable);
            await connection.CloseAsync();
        }

        public virtual async Task<bool> DeleteAsync(T toDelete)
        {
            return await _connection.DeleteAsync(toDelete);
        }

        public virtual async Task IncrementAsync(long Id, object columns)
        {
            var MyAttribute = (TableAttribute) Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
            var query = $"UPDATE {MyAttribute.Name} SET ";
            var propertyNames = columns.GetType().GetProperties().Select(p => p.Name).ToArray();
            foreach (var prop in propertyNames)
            {
                var propValue = columns.GetType().GetProperty(prop)?.GetValue(columns, null);
                query += $"{prop} += {propValue},";
            }

            query = query.Remove(query.Length - 1);
            query += $" where Id = {Id}";

            await _connection.ExecuteAsync(query);
        }

        public virtual async Task<T> GetAsync<T1>(T1 id)
        {
            var MyAttribute = (TableAttribute) Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
            var builder = new SqlBuilder();
            var selector = builder.AddTemplate($"SELECT * FROM {MyAttribute.Name} /**where**/");
            builder.Where("Id = @Id", new {Id = id});
            return await _connection.QuerySingleOrDefaultAsync<T>(selector.RawSql, selector.Parameters);
        }

        public virtual async Task<IEnumerable<T>> GetAsync<T1>(IEnumerable<T1> ids)
        {
            var MyAttribute = (TableAttribute) Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
            var builder = new SqlBuilder();
            var selector = builder.AddTemplate($"SELECT * FROM {MyAttribute.Name} /**where**/");
            builder.Where("Id in @Id", new {Id = ids});
            return await _connection.QueryAsync<T>(selector.RawSql, selector.Parameters);
        }

        public async Task<IEnumerable<T>> ListAsync()
        {
            var MyAttribute = (TableAttribute) Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
            var builder = new SqlBuilder();
            var selector = builder.AddTemplate($"SELECT * FROM {MyAttribute.Name}");
            return await _connection.QueryAsync<T>(selector.RawSql, selector.Parameters);
        }

        public async Task<IEnumerable<T>> ListAsync(int limit, string orderBy, string orderSort, int page = 1)
        {
            var MyAttribute = (TableAttribute) Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
            var builder = new SqlBuilder();
            var selector =
                builder.AddTemplate(
                    $"SELECT * FROM {MyAttribute.Name} ORDER BY {orderBy} {orderSort} OFFSET @Limit * (@Page - 1) ROWS FETCH NEXT @Limit ROWS ONLY");
            builder.AddParameters(new {Limit = limit, Page = page});
            return await _connection.QueryAsync<T>(selector.RawSql, selector.Parameters);
        }
    }
}