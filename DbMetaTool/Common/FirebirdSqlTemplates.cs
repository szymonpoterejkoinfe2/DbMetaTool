namespace DbMetaTool.Common
{
    public static class FirebirdSqlTemplates
    {
        public const string PARAM_TABLE_NAME = "@T";

        public const string GetDomains = @"
            SELECT RDB$FIELD_NAME, RDB$FIELD_TYPE, RDB$FIELD_LENGTH
            FROM RDB$FIELDS
            WHERE RDB$SYSTEM_FLAG = 0
        ";

        public const string GetTables = @"
            SELECT RDB$RELATION_NAME 
            FROM RDB$RELATIONS 
            WHERE RDB$SYSTEM_FLAG = 0 AND RDB$VIEW_SOURCE IS NULL
        ";

        public const string GetProcedures = @"
            SELECT RDB$PROCEDURE_NAME, RDB$PROCEDURE_SOURCE 
            FROM RDB$PROCEDURES
            WHERE RDB$SYSTEM_FLAG = 0
        ";

        public const string GetFields = @"
            SELECT RDB$FIELD_NAME, RDB$FIELD_SOURCE 
            FROM RDB$RELATION_FIELDS 
            WHERE RDB$RELATION_NAME = @T
            ORDER BY RDB$FIELD_POSITION
        ";

        public const string CreateTable = "CREATE TABLE {0} (\n{1}\n);";
        public const string CreateField = "    {0} {1}";
        public const string CreateProcedure = "CREATE PROCEDURE {0} AS\n{1};";
    }
}
