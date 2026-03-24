using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Data;
using System.Data.Common;
using static Dapper.SqlMapper;

namespace ExpenseTrackingSystem.Infrastructure.Data.DbContext
{
    public class DapperContext : IDapperContext
    {
        private readonly string Connectionstring;
        private IConfiguration _config;

        public DapperContext(IConfiguration config)
        {
            Connectionstring = config.GetConnectionString("DefaultConnectionStr") ??
                throw new Exception($"ConnectionString not found for DataBaseConnection.");
            _config = config;
        }
        private DbConnection GetDbconnection()
        {
            return new MySqlConnection(Connectionstring);
        }


        public async Task<bool> ExecuteAsync(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure)
        {
            
                using var db = GetDbconnection();
                return await db.ExecuteAsync(sp, parms, commandType: commandType) == 0;

        }
        //public async Task SaveAPILogs(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure)
        //{
        //    using var db = new SqlConnection(_configuration.GetConnectionString("LogingDBConncetion"));
        //    await db.QueryFirstOrDefaultAsync(sp, parms, commandType: commandType);
        //}
        public async Task<T> GetSingleAsync<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure)
        {
            using var db = GetDbconnection();
            return await db.QueryFirstOrDefaultAsync<T>(sp, parms, commandType: commandType);
        }
        //public async Task<List<T>> GetListAsync<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure)
        //{
        //    using var db = GetDbconnection();
        //    var Data = await db.QueryAsync<T>(sp, parms, commandType: commandType);
        //    return Data.ToList();
        //}

        public async Task<List<T>> GetListAsync<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure)
        {
            using var db = GetDbconnection();
            if (db == null)
                throw new InvalidOperationException("Database connection is null.");

            var data = await db.QueryAsync<T>(sp, parms, commandType: commandType);

            // Null-safe conversion
            return data?.ToList() ?? new List<T>();
        }

        public async Task<IEnumerable<T>> GetListAsyncTable<T>(string query, object param = null, CommandType commandType = CommandType.Text)
        {
            using var db = GetDbconnection();
            await db.OpenAsync();

            using var transaction = db.BeginTransaction();

            try
            {
                var result = await db.QueryAsync<T>(query, param, commandType: commandType, transaction: transaction);

                transaction.Commit(); // Commit the transaction if successful

                return result;
            }
            catch (Exception)
            {
                // Handle exceptions or log errors
                transaction.Rollback(); // Rollback the transaction in case of an exception
                throw;
            }
            finally
            {
                db.Close(); // Ensure the connection is closed, whether successful or not
            }
        }

        public async Task<List<T>> ExecuteTransactionMultipleReturn<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure)
        {
            using var db = GetDbconnection();
            try
            {
                if (db.State == ConnectionState.Closed)
                {
                    db.Open();
                }
                var tran = db.BeginTransaction();
                try
                {
                    var result = await db.QueryAsync<T>(sp, parms, commandType: commandType, transaction: tran);
                    tran.Commit();
                    return result.ToList();
                }
                catch (Exception)
                {
                    tran.Rollback();
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (db.State == ConnectionState.Open)
                {
                    db.Close();
                }
            }
        }
        public async Task<T> ExecuteTransactionSingleReturn<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure)
        {
            using var db = GetDbconnection();
            try
            {
                if (db.State == ConnectionState.Closed)
                {
                    db.Open();
                }
                var tran = db.BeginTransaction();
                try
                {
                    var result = await db.QueryFirstOrDefaultAsync<T>(sp, parms, commandType: commandType, transaction: tran);
                    tran.Commit();
                    return result;
                }
                catch (Exception)
                {
                    tran.Rollback();
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (db.State == ConnectionState.Open)
                {
                    db.Close();
                }
            }
        }
        public async Task<List<object>> GetMultipleSelectsAsync(string sql, object? parameters = null, params Func<GridReader, object>[] readerFuncs)
        {
            /// <Usr>
            ///     === Example ===
            ///
            /// var foosAndBars = await GetMultipleSelectsAsync("SP_ReturnMultipleSelects", dTO,
            ///     x => x.ReadFirstOrDefaultAsync<CaseDetailsViewModel>(),
            ///     x => x.Read<GetCaseEmailsViewModel>().ToList());
            /// GetSingleCaseViewModel Result = new GetSingleCaseViewModel();
            /// Result.Case = (CaseDetailsViewModel)foosAndBars[0];
            /// Result.Emails = (List<GetCaseEmailsViewModel>)foosAndBars[1];
            ///
            /// </summary>
            var returnResults = new List<object>();
            using (IDbConnection db = GetDbconnection())
            {
                using var Result = await db.QueryMultipleAsync(sql, parameters, commandType: CommandType.StoredProcedure);
                foreach (var readerFunc in readerFuncs)
                {
                    returnResults.Add(readerFunc(Result));
                }
            }
            return returnResults;
        }
        public async Task<IEnumerable<T>> GetEnumerableAsync<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure)
        {
            using var db = GetDbconnection();
            var Data = await db.QueryAsync<T>(sp, parms, commandType: commandType);
            return Data.ToList();
        }
        public IEnumerable<T> GetEnumerable<T>(string sp, object? parms = null, CommandType commandType = CommandType.StoredProcedure)
        {
            using var db = GetDbconnection();
            var Data = db.Query<T>(sp, parms, commandType: commandType);
            return Data.ToList();
        }
    }

}
