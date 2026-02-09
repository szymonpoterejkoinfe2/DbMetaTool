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

        private const int TABLE_INDEX_NAME = 0;


        private readonly string _connectionString;

        public FirebirdMetadataReader(string connectionString)
        {
            _connectionString = connectionString;
        }

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
                return Result<List<DomainData>>.Fail($"Failed to get domains: {ex.Message}");
            }
        }


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

                    tables.Add(new TableData(
                        Name: table,
                        Fields: fieldsResult.Value
                    ));
                }

                return Result<List<TableData>>.Success(tables);
            }
            catch (Exception ex)
            {
                return Result<List<TableData>>.Fail($"Failed to get tables: {ex.Message}");
            }
        }

        

        public async Task<Result<List<ProcedureData>>> GetProceduresAsync()
        {
            try
            {
                var executor = new FirebirdExecutor(_connectionString);

                var readerResult = await executor.ExecuteReaderAsync(FirebirdSqlTemplates.GetProcedures);
                if (readerResult.IsFailure)
                    return Result<List<ProcedureData>>.Fail(readerResult.Error);

                var result = new List<ProcedureData>();
                using var rdr = readerResult.Value;

                while (await rdr.ReadAsync())
                {
                    result.Add(new ProcedureData(
                        Name: rdr.GetString(PROCEDURE_INDEX_NAME).Trim(),
                        Source: rdr.IsDBNull(PROCEDURE_INDEX_SOURCE) ? "" : rdr.GetString(PROCEDURE_INDEX_SOURCE)
                    ));
                }

                return Result<List<ProcedureData>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<List<ProcedureData>>.Fail($"Failed to get procedures {ex.Message}");
            }
        }

        #region Helper Methods

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
                return Result<List<FieldData>>.Fail($"Failed to get columns for table {table}: {ex.Message}");
            }
        }

        #endregion
    }
}
