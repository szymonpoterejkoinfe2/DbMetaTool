using DbMetaTool.Common;
using DbMetaTool.Models;
using System.Text.Json;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;

namespace DbMetaTool.Services
{
    public class ScriptWriter
    {
        private readonly string _folder;

        public ScriptWriter(string folder) => _folder = folder;

        public async Task<Result> WriteDomains(List<DomainData> domains)
        {
            try
            {
                string path = Path.Combine(_folder, "domains.json");
                var json = JsonSerializer.Serialize(domains, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(path, json);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Failed to write domains: {ex.Message}");
            }
        }

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
                return Result.Fail($"Failed to write tables: {ex.Message}");
            }
        }

        public async Task<Result> WriteProcedures(List<ProcedureData> procedures)
        {
            try
            {
                foreach (var p in procedures)
                {
                    string sql = string.Format(FirebirdSqlTemplates.CreateProcedure, p.Name, p.Source ?? "");
                    string path = Path.Combine(_folder, $"{p.Name}.sql");
                    await File.WriteAllTextAsync(path, sql);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Failed to write procedures: {ex.Message}");
            }
        }
    }
}
