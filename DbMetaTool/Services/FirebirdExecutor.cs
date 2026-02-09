using DbMetaTool.Common;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Services
{
    public class FirebirdExecutor
    {
        private readonly string _connectionString;

        public FirebirdExecutor(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Result> ExecuteNonQueryAsync(string sql)
        {
            try
            {
                using var con = new FbConnection(_connectionString);
                await con.OpenAsync();

                using var cmd = new FbCommand(sql, con);
                await cmd.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail($"SQL execution error: {ex.Message}");
            }
        }

        // wersja bez parametrów (domyślna)
        public async Task<Result<FbDataReader>> ExecuteReaderAsync(string sql)
        {
            return await ExecuteReaderAsync(sql, null);
        }

        // wersja z parametrami
        public async Task<Result<FbDataReader>> ExecuteReaderAsync(string sql, Dictionary<string, object>? parameters)
        {
            try
            {
                var con = new FbConnection(_connectionString);
                await con.OpenAsync();

                var cmd = new FbCommand(sql, con);

                // jeśli podano parametry, dodajemy je
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }

                var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection);

                return Result<FbDataReader>.Success(reader);
            }
            catch (Exception ex)
            {
                return Result<FbDataReader>.Fail($"Failed to execute query: {ex.Message}");
            }
        }
    }
}
