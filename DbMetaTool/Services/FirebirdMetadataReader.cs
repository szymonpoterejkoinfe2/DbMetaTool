using DbMetaTool.Common;
using DbMetaTool.Models;

namespace DbMetaTool.Services
{
    public class FirebirdMetadataReader
    {
        private const int FIELD_INDEX_NAME = 0;
        private const int FIELD_INDEX_DOMAIN = 1;
        private const int FIELD_INDEX_FIELD_TYPE = 1;
        private const int FIELD_INDEX_LENGTH = 2;

        private const int PROCEDURE_INDEX_NAME = 0;
        private const int PROCEDURE_INDEX_SOURCE = 1;

        private const int PROCEDURE_PARAMS_NAME = 2;
        private const int PROCEDURE_PARAMS_TYPE_CODE = 3;
        private const int PROCEDURE_PARAMS_TYPE_DIRECTION = 4;

        private const int TABLE_INDEX_NAME = 0;


        private readonly string _connectionString;

        public FirebirdMetadataReader(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Pobiera z bazy danych listę wszystkich domen zdefiniowanych przez użytkownika.
        /// </summary>
        public async Task<Result<List<DomainData>>> GetDomainsAsync()
        {
            try
            {
                var executor = new FirebirdExecutor(_connectionString);

                var readerResult = await executor.ExecuteReaderAsync(FirebirdSqlTemplates.GetDomains);
                if (readerResult.IsFailure)
                    return Result<List<DomainData>>.Fail(readerResult.Error);

                var result = new List<DomainData>();
                using var rdr = readerResult.Value;

                while (await rdr.ReadAsync())
                {
                    result.Add(new DomainData(
                        Name: rdr.GetString(FIELD_INDEX_NAME).Trim(),
                        FieldType: rdr.GetInt32(FIELD_INDEX_FIELD_TYPE),
                        Length: rdr.GetInt32(FIELD_INDEX_LENGTH)
                    ));
                }

                return Result<List<DomainData>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<List<DomainData>>.Fail($"Błąd przy pobieraniu domen: {ex.Message}");
            }
        }

        /// <summary>
        /// Pobiera z bazy danych listę wszystkich tabel użytkownika wraz z ich kompletną strukturą pól.
        /// </summary>
        public async Task<Result<List<TableData>>> GetTablesAsync()
        {
            try
            {
                var executor = new FirebirdExecutor(_connectionString);

                var readerResult = await executor.ExecuteReaderAsync(FirebirdSqlTemplates.GetTables);
                if (readerResult.IsFailure)
                    return Result<List<TableData>>.Fail(readerResult.Error);

                var tables = new List<TableData>();
                using var rdr = readerResult.Value;

                var tableNames = new List<string>();
                while (await rdr.ReadAsync())
                    tableNames.Add(rdr.GetString(TABLE_INDEX_NAME).Trim());

                foreach (var table in tableNames)
                {
                    var fieldsResult = await GetFieldsAsync(table);
                    if (fieldsResult.IsFailure)
                        return Result<List<TableData>>.Fail(fieldsResult.Error);

                    if (fieldsResult.Value.Count == 0)
                        continue;

                    tables.Add(new TableData(
                        Name: table,
                        Fields: fieldsResult.Value
                    ));
                }

                return Result<List<TableData>>.Success(tables);
            }
            catch (Exception ex)
            {
                return Result<List<TableData>>.Fail($"Błąd przy pobieraniu tabeli: {ex.Message}");
            }
        }

        /// <summary>
        /// Pobiera z bazy danych listę wszystkich procedur użytkownika wraz z ich kodem źródłowym i kompletną listą parametrów.
        /// </summary>
        public async Task<Result<List<ProcedureData>>> GetProceduresAsync()
        {
            try
            {
                var executor = new FirebirdExecutor(_connectionString);

                var readerResult = await executor.ExecuteReaderAsync(FirebirdSqlTemplates.GetProcedures);
                if (readerResult.IsFailure)
                    return Result<List<ProcedureData>>.Fail(readerResult.Error);

                var resultDict = new Dictionary<string, ProcedureData>();
                using var rdr = readerResult.Value;

                while (await rdr.ReadAsync())
                {
                    string procName = rdr.GetString(PROCEDURE_INDEX_NAME).Trim();

                    if (!resultDict.TryGetValue(procName, out var procedure))
                    {
                        procedure = new ProcedureData(
                            Name: procName,
                            Source: rdr.IsDBNull(PROCEDURE_INDEX_SOURCE) ? "" : rdr.GetString(PROCEDURE_INDEX_SOURCE).Trim()
                        );
                        resultDict.Add(procName, procedure);
                    }

                    if (!rdr.IsDBNull(PROCEDURE_PARAMS_NAME))
                    {
                        procedure.Parameters.Add(new ParameterData(
                            Name: rdr.GetString(PROCEDURE_PARAMS_NAME).Trim(),
                            TypeCode: rdr.GetInt32(PROCEDURE_PARAMS_TYPE_CODE),
                            Direction: rdr.GetInt32(PROCEDURE_PARAMS_TYPE_DIRECTION)
                        ));
                    }
                }

                return Result<List<ProcedureData>>.Success(resultDict.Values.ToList());
            }
            catch (Exception ex)
            {
                return Result<List<ProcedureData>>.Fail($"Failed to get procedures {ex.Message}");
            }
        }

        #region Helper Methods

        /// <summary>
        /// Pobiera listę pól (kolumn) wraz z przypisanymi do nich domenami dla wskazanej tabeli.
        /// </summary>
        /// <param name="table">Nazwa tabeli, dla której mają zostać pobrane definicje pól.</param>
        private async Task<Result<List<FieldData>>> GetFieldsAsync(string table)
        {
            try
            {
                var executor = new FirebirdExecutor(_connectionString);


                var parameters = new Dictionary<string, object>
                {
                    { FirebirdSqlTemplates.PARAM_TABLE_NAME, table }
                };


                var readerResult = await executor.ExecuteReaderAsync(FirebirdSqlTemplates.GetFields, parameters);
                if (readerResult.IsFailure)
                    return Result<List<FieldData>>.Fail(readerResult.Error);

                var result = new List<FieldData>();
                using var rdr = readerResult.Value;

                while (await rdr.ReadAsync())
                {
                    result.Add(new FieldData(
                        Name: rdr.GetString(FIELD_INDEX_NAME).Trim(),
                        Domain: rdr.GetString(FIELD_INDEX_DOMAIN).Trim()
                    ));
                }

                return Result<List<FieldData>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<List<FieldData>>.Fail($"Błąd przy pobieraniu kolumn tabeli: {table}: {ex.Message}");
            }
        }

        #endregion
    }
}
