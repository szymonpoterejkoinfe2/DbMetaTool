namespace DbMetaTool.Config
{
    public static class ConnectionStringManager
    {
        /// <summary>
        /// Connection string bez pola "database="
        /// Np.: "server=localhost;user=sysdba;password=masterkey;charset=UTF8;"
        /// </summary>
        public static string BaseConnectionString { get; set; } = "server=localhost;user=sysdba;password=masterkey;charset=UTF8;";
    }
}
