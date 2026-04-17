using static Dapper.SqlMapper;
using System.Data;

namespace SpendwiseSystem.Infrastructure.Data.DbContext
{
    public interface IDapperContext
    {
        Task<bool> ExecuteAsync(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure);
        Task<T> GetSingleAsync<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure);
        Task<List<T>> GetListAsync<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure);
        Task<IEnumerable<T>> GetListAsyncTable<T>(string query, object param = null, CommandType commandType = CommandType.Text);
        Task<IEnumerable<T>> GetEnumerableAsync<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure);
        Task<List<T>> ExecuteTransactionMultipleReturn<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure);
        Task<T> ExecuteTransactionSingleReturn<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure);
        Task<List<object>> GetMultipleSelectsAsync(string sql, object? parameters = null, params Func<GridReader, object>[] readerFuncs);
        IEnumerable<T> GetEnumerable<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure);
    }
}


