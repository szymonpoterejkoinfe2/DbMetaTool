using DbMetaTool.Common;
using DbMetaTool.Models;
using DbMetaTool.Services;

namespace DbMetaTool.Commands
{
    public class ExportScriptsCommand
    {
        private readonly string _connectionString;

        public ExportScriptsCommand(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Eksportuje domeny, tabele i procedury z bazy Firebird 5.0 do plików
        /// </summary>
        public async Task<Result> ExecuteAsync(string outputDirectory)
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                var reader = new FirebirdMetadataReader(_connectionString);
                var writer = new ScriptWriter(outputDirectory);

                var domainsResult = await ExportDomainsAsync(reader, writer);
                if (domainsResult.IsFailure)
                    return domainsResult;

                var tablesResult = await ExportTablesAsync(reader, writer);
                if (tablesResult.IsFailure)
                    return tablesResult;

                var proceduresResult = await ExportProceduresAsync(reader, writer);
                if (proceduresResult.IsFailure)
                    return proceduresResult;

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Nieoczekiwany błąd: {ex.Message}");
            }
        }

        #region Helper Methods

        /// <summary>
        /// Zarządza procesem pobierania definicji domen z bazy danych i ich eksportu do skryptu SQL.
        /// </summary>
        /// <param name="reader">Komponent odpowiedzialny za ekstrakcję metadanych domen z tabel systemowych Firebirda.</param>
        /// <param name="writer">Komponent odpowiedzialny za mapowanie typów danych i fizyczny zapis skryptu na dysku.</param>
        private async Task<Result> ExportDomainsAsync(FirebirdMetadataReader reader, ScriptWriter writer)
        {
            var domainsResult = await reader.GetDomainsAsync();
            if (domainsResult.IsFailure)
                return Result.Fail($"Błąd pobierania domen: {domainsResult.Error}");

            var writeResult = await writer.WriteDomains(domainsResult.Value);
            if (writeResult.IsFailure)
                return Result.Fail($"Błąd zapisu domen: {writeResult.Error}");

            Console.WriteLine($"Wyeksportowano {domainsResult.Value.Count} domen.");
            return Result.Success();
        }

        /// <summary>
        /// Zarządza procesem pobierania definicji tabel z bazy danych i ich zapisu do plików SQL.
        /// </summary>
        /// <param name="reader">Komponent odpowiedzialny za ekstrakcję metadanych tabel z systemowych relacji Firebirda.</param>
        /// <param name="writer">Komponent odpowiedzialny za formatowanie instrukcji CREATE TABLE i operacje wejścia/wyjścia na plikach.</param>
        private async Task<Result> ExportTablesAsync(FirebirdMetadataReader reader, ScriptWriter writer)
        {
            var tablesResult = await reader.GetTablesAsync();
            if (tablesResult.IsFailure)
                return Result.Fail($"Błąd pobierania tabel: {tablesResult.Error}");

            var writeResult = await writer.WriteTables(tablesResult.Value);
            if (writeResult.IsFailure)
                return Result.Fail($"Błąd zapisu tabel: {writeResult.Error}");

            Console.WriteLine($"Wyeksportowano {tablesResult.Value.Count} tabel.");
            return Result.Success();
        }

        /// <summary>
        /// Koordynuje proces pobierania procedur z bazy danych i ich eksportu do plików skryptów SQL.
        /// </summary>
        /// <param name="reader">Instancja klasy odpowiedzialnej za odczyt struktur i kodu źródłowego z systemowych tabel Firebirda.</param>
        /// <param name="writer">Instancja klasy odpowiedzialnej za formatowanie kodu SQL i fizyczny zapis plików na dysku.</param>
        private async Task<Result> ExportProceduresAsync(FirebirdMetadataReader reader, ScriptWriter writer)
        {
            var proceduresResult = await reader.GetProceduresAsync();
            if (proceduresResult.IsFailure)
                return Result.Fail($"Błąd pobierania procedur: {proceduresResult.Error}");

            var writeResult = await writer.WriteProcedures(proceduresResult.Value);
            if (writeResult.IsFailure)
                return Result.Fail($"Błąd zapisu procedur: {writeResult.Error}");

            Console.WriteLine($"Wyeksportowano {proceduresResult.Value.Count} procedur.");
            return Result.Success();
        }

        #endregion
    }
}
