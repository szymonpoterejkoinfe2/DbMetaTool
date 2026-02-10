using DbMetaTool.Common;
using DbMetaTool.Config;
using DbMetaTool.Services;
using System.Text.RegularExpressions;

namespace DbMetaTool.Commands
{
    public class BuildDatabaseCommand
    {
        private const int DOMAIN_NAME_INDEX = 1;
        private const int DOMAIN_TYPE_INDEX = 2;

        private const int CREATE_TABLE_NAME = 1;
        private const int CREATE_PROCEDURE_NAME = 1;
        const string END_CHAR = ";";

        public BuildDatabaseCommand()
        {
        }

        /// <summary>
        /// Tworzy nową bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public async Task<Result> ExecuteAsync(string databaseDirectory, string scriptsDirectory)
        {
            try
            {
                if (!Directory.Exists(scriptsDirectory))
                    return Result.Fail($"Katalog ze skryptami nie istnieje: {scriptsDirectory}");

                //Tworzenie pustej bazy
                Directory.CreateDirectory(databaseDirectory);
                string dbPath = Path.Combine(databaseDirectory, "database.fdb");

                var createResult = FirebirdDatabaseCreator.CreateEmptyDatabase(
                    dbPath,
                    ConnectionStringManager.BaseConnectionString
                );

                if (createResult.IsFailure)
                    return createResult;

                Console.WriteLine($"Utworzono pustą bazę: {dbPath}");


                string connStr = BuildFullConnectionString(dbPath);
                var executor = new FirebirdExecutor(connStr);

                var sqlFiles = Directory.GetFiles(scriptsDirectory, "*.sql");


                List<string> domainScripts = new();
                List<string> tableScripts = new();
                List<string> procedureScripts = new();

                //  Kategoryzacja skryptów 
                foreach (var file in sqlFiles)
                {
                    string sql = await File.ReadAllTextAsync(file);

                    if (Regex.IsMatch(sql, SqlRegexPatterns.CREATE_DOMAIN, RegexOptions.IgnoreCase))
                    {
                        domainScripts.Add(sql);
                        continue;
                    }

                    if (Regex.IsMatch(sql, SqlRegexPatterns.CREATE_TABLE, RegexOptions.IgnoreCase))
                    {
                        tableScripts.Add(sql);
                        continue;
                    }

                    if (Regex.IsMatch(sql, SqlRegexPatterns.CREATE_PROCEDURE, RegexOptions.IgnoreCase))
                    {
                        procedureScripts.Add(sql);
                        continue;
                    }
                }


                // Domeny
                foreach (var sql in domainScripts)
                {
                    var result = await ProcessDomainAsync(sql, executor);
                    if (result.IsFailure)
                        return result;
                }

                // Tabele
                foreach (var sql in tableScripts)
                {
                    var result = await ProcessTableAsync(sql, executor);
                    if (result.IsFailure)
                        return result;
                }

                // Procedury
                foreach (var sql in procedureScripts)
                {
                    var result = await ProcessProcedureAsync(sql, executor);
                    if (result.IsFailure)
                        return result;
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
        /// Wyodrębnia definicje domen z kodu SQL i asynchronicznie tworzy je w bazie danych.
        /// </summary>
        /// <param name="sql">Ciąg znaków zawierający definicje domen do przetworzenia.</param>
        /// <param name="executor">Instancja <see cref="FirebirdExecutor"/> odpowiedzialna za interakcję z bazą Firebird.</param>
        private async Task<Result> ProcessDomainAsync(string sql, FirebirdExecutor executor)
        {
            var matches = Regex.Matches(sql, SqlRegexPatterns.CREATE_DOMAIN, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                string domainName = match.Groups[DOMAIN_NAME_INDEX].Value.Trim();  
                string domainType = match.Groups[DOMAIN_TYPE_INDEX].Value.Trim();  

                string domainSql = $"CREATE DOMAIN {domainName} AS {domainType};";

                var result = await executor.ExecuteNonQueryAsync(domainSql);
                if (result.IsFailure)
                    return Result.Fail($"Błąd tworzenia domeny {domainName}: {result.Error}");

                Console.WriteLine($"Utworzono domenę {domainName}");
            }

            return Result.Success();
        }

        /// <summary>
        /// Asynchronicznie przetwarza skrypt SQL w celu utworzenia nowej tabeli w bazie danych.
        /// </summary>
        /// <param name="sql">Kod źródłowy SQL zawierający instrukcję definicji tabeli.</param>
        /// <param name="executor">Instancja <see cref="FirebirdExecutor"/> odpowiedzialna za wykonanie polecenia na bazie danych.</param>
        private async Task<Result> ProcessTableAsync(string sql, FirebirdExecutor executor)
        {
            var tableMatch = Regex.Match(sql, SqlRegexPatterns.CREATE_TABLE, RegexOptions.IgnoreCase);
            if (!tableMatch.Success)
                return Result.Success();

            string tableName = tableMatch.Groups[CREATE_TABLE_NAME].Value.Trim();

            var result = await executor.ExecuteNonQueryAsync(sql);
            if (result.IsFailure)
                return Result.Fail($"Błąd tworzenia tabeli {tableName}: {result.Error}");

            Console.WriteLine($"Utworzono tabelę {tableName}");
            return Result.Success();
        }

        /// <summary>
        /// Asynchronicznie przetwarza skrypt SQL w celu utworzenia lub aktualizacji procedury składowanej.
        /// </summary>
        /// <param name="sql">Pełny kod źródłowy instrukcji SQL definiującej procedurę.</param>
        /// <param name="executor">Instancja <see cref="FirebirdExecutor"/> wykorzystywana do komunikacji z bazą danych.</param>
        private async Task<Result> ProcessProcedureAsync(string sql, FirebirdExecutor executor)
        {
            var procMatch = Regex.Match(sql, SqlRegexPatterns.CREATE_PROCEDURE, RegexOptions.IgnoreCase);
            if (!procMatch.Success)
                return Result.Success();

            string procName = procMatch.Groups[CREATE_PROCEDURE_NAME].Value.Trim();

            var result = await executor.ExecuteNonQueryAsync(sql);
            if (result.IsFailure)
                return Result.Fail($"Błąd tworzenia procedury {procName}: {result.Error}");

            Console.WriteLine($"Utworzono procedurę {procName}");
            return Result.Success();
        }

        private string BuildFullConnectionString(string dbPath)
        {
            string baseConn = ConnectionStringManager.BaseConnectionString.Trim();

            if (!baseConn.EndsWith(END_CHAR))
                baseConn += END_CHAR;

            return baseConn + $"database={dbPath};";
        }


        #endregion
    }
}
