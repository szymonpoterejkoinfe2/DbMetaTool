using DbMetaTool.Common;

namespace DbMetaTool.Services
{
    public class FirebirdDatabaseCreator
    {
        const string END_CHAR = ";";

        /// <summary>
        /// Tworzy nowy, pusty plik bazy danych Firebird o wskazanej lokalizacji.
        /// </summary>
        /// <param name="databaseFile">Pełna ścieżka systemowa, pod którą ma zostać utworzony plik bazy danych (np. "C:\Bazy\NowaBaza.fdb").</param>
        /// <param name="baseConnectionString">Bazowy ciąg połączenia zawierający dane serwera, użytkownika i hasło (bez parametru 'database').</param>
        public static Result CreateEmptyDatabase(string databaseFile, string baseConnectionString)
        {
            try
            {
                string connStr = baseConnectionString.Trim();

                if (!connStr.EndsWith(END_CHAR))
                    connStr += END_CHAR;

                connStr += $"database={databaseFile};";

                FirebirdSql.Data.FirebirdClient.FbConnection.CreateDatabase(
                    connStr,
                    pageSize: 8192,
                    forcedWrites: true);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail("Błąd tworzenia pustej bazy: " + ex.Message);
            }
        }

    }
}
