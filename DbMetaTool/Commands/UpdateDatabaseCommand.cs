using DbMetaTool.Common;
using DbMetaTool.Models;
using DbMetaTool.Services;
using System.Text.RegularExpressions;

namespace DbMetaTool.Commands
{
    public class UpdateDatabaseCommand
    {
        private const int CREATE_TABLE_NAME = 1;
        private const int CREATE_PROCEDURE_NAME = 1;
        private const int COLUMN_NAME = 1;
        private const int DOMAIN_NAME = 1;
        private const string FILE_EXTENSION = "*.sql";

        private readonly string _connectionString;

        public UpdateDatabaseCommand(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Różnicowa aktualizacja bazy danych. Domeny, tabele, procedury
        /// </summary>
        /// <param name="scriptsDirectory">Katalog ze skryptami SQL</param>
        public async Task<Result> ExecuteAsync(string scriptsDirectory)
        {
            try
            {
                if (!Directory.Exists(scriptsDirectory))
                    return Result.Fail($"Katalog ze skryptami nie istnieje: {scriptsDirectory}");

                var executor = new FirebirdExecutor(_connectionString);
                var reader = new FirebirdMetadataReader(_connectionString);

                // 1. Pobranie aktualnej struktury bazy (Stan początkowy)
                var existingTables = await reader.GetTablesAsync();
                if (existingTables.IsFailure) return Result.Fail(existingTables.Error);

                var existingProcedures = await reader.GetProceduresAsync();
                if (existingProcedures.IsFailure) return Result.Fail(existingProcedures.Error);

                var existingDomains = await reader.GetDomainsAsync();
                if (existingDomains.IsFailure) return Result.Fail(existingDomains.Error);

                // 2. Wczytanie wszystkich skryptów do pamięci
                var sqlFiles = Directory.GetFiles(scriptsDirectory, FILE_EXTENSION);
                var scripts = new List<string>();
                foreach (var file in sqlFiles)
                {
                    scripts.Add(await File.ReadAllTextAsync(file));
                }

                //FAZA: DOMENY 
                foreach (var sql in scripts)
                {
                    var result = await ProcessDomainAsync(sql, executor, existingDomains.Value);
                    if (result.IsFailure) return result;
                }

                //FAZA: TABELE
                foreach (var sql in scripts)
                {
                    var result = await ProcessTableAsync(sql, executor, existingTables.Value);
                    if (result.IsFailure) return result;
                }

                //FAZA: PROCEDURY
                foreach (var sql in scripts)
                {
                    var result = await ProcessProcedureAsync(sql, executor, existingProcedures.Value);
                    if (result.IsFailure) return result;
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Nieoczekiwany błąd: {ex.Message}");
            }
        }

        #region Helper Methods

        /// <summary>
        /// Przetwarza skrypt SQL w celu utworzenia nowej tabeli lub aktualizacji struktury istniejącej o brakujące kolumny.
        /// </summary>
        /// <param name="sql">Pełny kod SQL definicji tabeli.</param>
        /// <param name="executor">Instancja <see cref="FirebirdExecutor"/> służąca do komunikacji z bazą danych.</param>
        /// <param name="existingTables">Lista aktualnych metadanych tabel pobranych z bazy w celu weryfikacji różnicowej.</param>
        private async Task<Result> ProcessTableAsync(string sql, FirebirdExecutor executor, List<TableData> existingTables)
        {
            var tableMatch = Regex.Match(sql, SqlRegexPatterns.CREATE_TABLE, RegexOptions.IgnoreCase);
            if (!tableMatch.Success)
                return Result.Success(); 

            string tableName = tableMatch.Groups[CREATE_TABLE_NAME].Value.Trim();
            bool tableExists = existingTables.Exists(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));

            if (tableExists)
            {

                //Odszukanie istniejących kolumn
                int firstParen = sql.IndexOf('(');
                int lastParen = sql.LastIndexOf(')');

                if (firstParen == -1 || lastParen == -1)
                    return Result.Fail($"Niepoprawna składnia SQL dla tabeli {tableName}");

                string tableBody = sql.Substring(firstParen + 1, lastParen - firstParen - 1);

                var columnsMatches = Regex.Matches(tableBody, SqlRegexPatterns.TABLE_COLUMNS, RegexOptions.IgnoreCase);
                var existingTable = existingTables.Find(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));

                foreach (Match colMatch in columnsMatches)
                {
                    string colName = colMatch.Groups[COLUMN_NAME].Value.Trim();

                    if (string.IsNullOrEmpty(colName) || colName.Equals("CONSTRAINT", StringComparison.OrdinalIgnoreCase))
                        continue;

                    bool colExists = existingTable.Fields.Exists(f => f.Name.Equals(colName, StringComparison.OrdinalIgnoreCase));

                    if (!colExists)
                    {
                        ///Utworzenie nowej kolumny
                        string addColumnSql = string.Format(FirebirdSqlTemplates.AddColumn, tableName, colMatch.Value.Trim());

                        var result = await executor.ExecuteNonQueryAsync(addColumnSql);
                        if (result.IsFailure)
                            return Result.Fail($"Błąd dodawania kolumny {colName} w tabeli {tableName}: {result.Error}");

                        Console.WriteLine($"Dodano kolumnę {colName} w tabeli {tableName}");
                    }
                }
            }
            else
            {
                // tworzenie nowej tabeli
                var result = await executor.ExecuteNonQueryAsync(sql);
                if (result.IsFailure)
                    return Result.Fail($"Błąd tworzenia tabeli {tableName}: {result.Error}");
                else
                    Console.WriteLine($"Utworzono tabelę {tableName}");
            }

            return Result.Success();
        }

        /// <summary>
        /// Analizuje skrypt SQL w celu zidentyfikowania i utworzenia nowej procedury składowanej, jeśli ta jeszcze nie istnieje.
        /// </summary>
        /// <param name="sql">Pełny kod źródłowy procedury (skrypt SQL).</param>
        /// <param name="executor">Instancja <see cref="FirebirdExecutor"/> odpowiedzialna za komunikację z bazą danych.</param>
        /// <param name="existingProcedures">Kolekcja aktualnie zdefiniowanych procedur w bazie danych, używana do weryfikacji istnienia obiektu.</param>
        private async Task<Result> ProcessProcedureAsync(string sql, FirebirdExecutor executor, List<ProcedureData> existingProcedures)
        {
            var procMatch = Regex.Match(sql, SqlRegexPatterns.CREATE_PROCEDURE, RegexOptions.IgnoreCase);
            if (!procMatch.Success)
                return Result.Success(); 

            string procName = procMatch.Groups[CREATE_PROCEDURE_NAME].Value.Trim();
            bool procExists = existingProcedures.Exists(p => p.Name.Equals(procName, StringComparison.OrdinalIgnoreCase));

            if (!procExists)
            {
                var result = await executor.ExecuteNonQueryAsync(sql);
                if (result.IsFailure)
                    return Result.Fail($"Błąd tworzenia procedury {procName}: {result.Error}");
                else
                    Console.WriteLine($"Utworzono procedurę {procName}");
            }
            else
            {
                Console.WriteLine($"Procedura {procName} już istnieje – pominięto.");
            }

            return Result.Success();
        }


        /// <summary>
        /// Przetwarza blok instrukcji SQL w celu zidentyfikowania i utworzenia brakujących domen w bazie danych.
        /// </summary>
        /// <param name="sql">Ciąg znaków zawierający jedną lub wiele definicji domen (skrypt SQL).</param>
        /// <param name="executor">Instancja <see cref="FirebirdExecutor"/> służąca do wykonywania poleceń na bazie danych.</param>
        /// <param name="existingDomains">Lista aktualnie istniejących domen pobranych z metadanych bazy danych.</param>
        private async Task<Result> ProcessDomainAsync(string sql, FirebirdExecutor executor, List<DomainData> existingDomains)
        {

            var individualDomainCommands = Regex.Split(sql, @"(?=CREATE\s+DOMAIN)", RegexOptions.IgnoreCase)
                                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                                .ToList();

            foreach (var command in individualDomainCommands)
            {
                var domainMatch = Regex.Match(command, SqlRegexPatterns.CREATE_DOMAIN, RegexOptions.IgnoreCase);

                if (!domainMatch.Success)
                    continue;

                string domainName = domainMatch.Groups[DOMAIN_NAME].Value.Trim();

                bool domainExists = existingDomains.Any(d => d.Name.Equals(domainName, StringComparison.OrdinalIgnoreCase));

                if (!domainExists)
                {
                    string singleSql = command.Trim();

                    var result = await executor.ExecuteNonQueryAsync(singleSql);
                    if (result.IsFailure)
                        return Result.Fail($"Błąd tworzenia domeny {domainName}: {result.Error}");

                    Console.WriteLine($"Utworzono domenę {domainName}");
                }
                else
                {
                    Console.WriteLine($"Domena {domainName} już istnieje – pominięto.");
                }
            }

            return Result.Success();
        }

        #endregion
    }
}
