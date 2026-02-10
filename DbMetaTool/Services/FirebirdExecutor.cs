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

        /// <summary>
        /// Wykonuje asynchronicznie polecenie SQL niebędące zapytaniem (np. DDL lub DML).
        /// </summary>
        /// /// <param name="sql">Tekst polecenia SQL do wykonania.</param>
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
                return Result.Fail($"Błąd skryptu SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Wykonuje asynchronicznie zapytanie SQL bez parametrów i zwraca czytnik danych <see cref="FbDataReader"/>.
        /// </summary>
        /// <param name="sql">Tekst zapytania SQL do wykonania.</param>
        public async Task<Result<FbDataReader>> ExecuteReaderAsync(string sql)
        {
            return await ExecuteReaderAsync(sql, null);
        }

        /// <summary>
        /// Wykonuje asynchronicznie zapytanie SQL i zwraca czytnik danych <see cref="FbDataReader"/>.
        /// </summary>
        /// <param name="sql">Tekst zapytania SQL do wykonania.</param>
        /// <param name="parameters">Opcjonalny słownik parametrów (klucz-wartość), które zostaną bezpiecznie wstrzyknięte do zapytania.</param>
        public async Task<Result<FbDataReader>> ExecuteReaderAsync(string sql, Dictionary<string, object>? parameters)
        {
            try
            {
                var con = new FbConnection(_connectionString);
                await con.OpenAsync();

                var cmd = new FbCommand(sql, con);

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
                return Result<FbDataReader>.Fail($"Błąd przy wykonaniu zapytania: {ex.Message}");
            }
        }
    }
}
