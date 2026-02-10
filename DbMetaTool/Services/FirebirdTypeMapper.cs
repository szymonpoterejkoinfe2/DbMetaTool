namespace DbMetaTool.Services
{
    public static class FirebirdTypeMapper
    {
        /// <summary>
        /// Konwertuje wewnętrzne identyfikatory typów danych Firebird na ich tekstowe odpowiedniki SQL.
        /// </summary>
        /// <param name="fieldType">Numeryczny kod typu pola z bazy Firebird.</param>
        /// <param name="length">Długość pola (liczba znaków lub bajtów) dla typów tekstowych.</param>
        public static string ToSqlType(int fieldType, int length)
        {
            return fieldType switch
            {
                7 => "SMALLINT",
                8 => "INTEGER",
                9 => "QUAD",
                10 => "FLOAT",
                11 => "D_FLOAT",
                12 => "DATE",
                13 => "TIME",
                14 => $"CHAR({length})",
                16 => "BIGINT",                    
                27 => "DOUBLE PRECISION",
                35 => "TIMESTAMP",
                37 => $"VARCHAR({length})",
                40 => $"CSTRING({length})",
                261 => "BLOB",                     
                _ => $"UNKNOWN_TYPE_{fieldType}"
            };
        }
    }
}
