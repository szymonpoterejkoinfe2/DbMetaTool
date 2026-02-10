namespace DbMetaTool.Common
{
    public static class SqlRegexPatterns
    {
        public const string CREATE_TABLE = @"CREATE\s+TABLE\s+(\w+)";
        public const string TABLE_COLUMNS = @"\s*(\w+)\s+\w+";
        public const string CREATE_PROCEDURE = @"CREATE(?:\s+OR\s+ALTER)?\s+PROCEDURE\s+(\w+)";
        public const string CREATE_DOMAIN = @"CREATE\s+DOMAIN\s+([A-Z0-9_$]+)\s+AS\s+([\w\d\(\)]+)";

    }
}
