using DbMetaTool.Common;
using DbMetaTool.Models;
using System.Text;
using System.Text.Json;

namespace DbMetaTool.Services
{
    public class ScriptWriter
    {
        private const int PARAM_DIRECTION_INPUT = 0;
        private const int PARAM_DIRECTION_OUTPUT = 1;
        private const int DEFAULT_VARCHAR_LENGTH = 100; 

        private readonly string _folder;

        public ScriptWriter(string folder) => _folder = folder;

        /// <summary>
        /// Eksportuje definicje wszystkich domen użytkownika do jednego zbiorczego pliku SQL.
        /// </summary>
        /// <param name="domains">Lista obiektów metadanych domen zawierająca nazwy, kody typów oraz ich długości.</param>
        public async Task<Result> WriteDomains(List<DomainData> domains)
        {
            try
            {
                string path = Path.Combine(_folder, "DOMAINS.sql");
                var sb = new StringBuilder();

                foreach (var d in domains)
                {
                    string sqlType = FirebirdTypeMapper.ToSqlType(d.FieldType, d.Length);

                    string sql = string.Format(FirebirdSqlTemplates.CreateDomain, d.Name, sqlType);

                    sb.AppendLine(sql);
                    sb.AppendLine();
                }

                await File.WriteAllTextAsync(path, sb.ToString());

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Błąd przy zapisie domen: {ex.Message}");
            }
        }

        /// <summary>
        /// Eksportuje definicje struktur tabel do indywidualnych plików SQL (.sql).
        /// </summary>
        /// <param name="tables">Lista obiektów metadanych tabel zawierających definicje kolumn.</param>
        public async Task<Result> WriteTables(List<TableData> tables)
        {
            try
            {
                foreach (var table in tables)
                {
                    var fieldsSql = string.Join(",\n",
                        table.Fields.Select(f => string.Format(FirebirdSqlTemplates.CreateField, f.Name, f.Domain)));

                    string sql = string.Format(FirebirdSqlTemplates.CreateTable, table.Name, fieldsSql);

                    string path = Path.Combine(_folder, $"{table.Name}.sql");
                    await File.WriteAllTextAsync(path, sql);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Błąd przy zapisie tablei: {ex.Message}");
            }
        }

        /// <summary>
        /// Eksportuje listę procedur do indywidualnych plików SQL (.sql).
        /// </summary>
        /// <param name="procedures">Lista obiektów danych procedur pobranych z metadanych bazy danych.</param>
        public async Task<Result> WriteProcedures(List<ProcedureData> procedures)
        {
            try
            {
                foreach (var p in procedures)
                {
                    var sb = new StringBuilder();

                    sb.Append($"CREATE OR ALTER PROCEDURE {p.Name.Trim()}");

                    var inputParams = p.Parameters.Where(x => x.Direction == PARAM_DIRECTION_INPUT).ToList();
                    if (inputParams.Any())
                    {
                        sb.AppendLine(" (");
                        sb.Append(string.Join(",\n", inputParams.Select(x =>
                            $"    {x.Name.Trim()} {FirebirdTypeMapper.ToSqlType(x.TypeCode, DEFAULT_VARCHAR_LENGTH)}")));
                        sb.Append(")");
                    }


                    var outputParams = p.Parameters.Where(x => x.Direction == PARAM_DIRECTION_OUTPUT).ToList();
                    if (outputParams.Any())
                    {
                        sb.AppendLine("\nRETURNS (");
                        sb.Append(string.Join(",\n", outputParams.Select(x =>
                            $"    {x.Name.Trim()} {FirebirdTypeMapper.ToSqlType(x.TypeCode, DEFAULT_VARCHAR_LENGTH)}")));
                        sb.Append(")");
                    }

                    sb.AppendLine("\nAS");


                    sb.AppendLine("BEGIN");
                    sb.AppendLine(p.Source?.Trim());
                    sb.AppendLine("END;");

                    string sql = sb.ToString();
                    string path = Path.Combine(_folder, $"{p.Name.Trim()}.sql");
                    await File.WriteAllTextAsync(path, sql);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Błąd przy zapisie procedury: {ex.Message}");
            }
        }
    }
}
