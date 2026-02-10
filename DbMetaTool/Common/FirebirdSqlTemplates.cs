namespace DbMetaTool.Common
{
    public static class FirebirdSqlTemplates
    {
        public const string PARAM_TABLE_NAME = "@T";

        public const string GetDomains = @"
            SELECT RDB$FIELD_NAME, RDB$FIELD_TYPE, RDB$FIELD_LENGTH
            FROM RDB$FIELDS
            WHERE RDB$SYSTEM_FLAG = 0
            AND RDB$FIELD_NAME NOT STARTING WITH 'RDB$'
        ";

        public const string GetTables = @"
            SELECT RDB$RELATION_NAME 
            FROM RDB$RELATIONS 
            WHERE RDB$SYSTEM_FLAG = 0 
            AND RDB$VIEW_SOURCE IS NULL
            AND RDB$RELATION_NAME NOT LIKE 'RDB$%'
        ";

        public const string GetProcedures = @"
            SELECT 
            p.RDB$PROCEDURE_NAME, 
            p.RDB$PROCEDURE_SOURCE,
            pp.RDB$PARAMETER_NAME,
            f.RDB$FIELD_TYPE,
            pp.RDB$PARAMETER_TYPE
            FROM RDB$PROCEDURES p
            LEFT JOIN RDB$PROCEDURE_PARAMETERS pp ON p.RDB$PROCEDURE_NAME = pp.RDB$PROCEDURE_NAME
            LEFT JOIN RDB$FIELDS f ON pp.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
            WHERE p.RDB$SYSTEM_FLAG = 0
            ORDER BY p.RDB$PROCEDURE_NAME, pp.RDB$PARAMETER_TYPE, pp.RDB$PARAMETER_NUMBER;
        ";


        public const string GetFields = @"
            SELECT RDB$FIELD_NAME, RDB$FIELD_SOURCE 
            FROM RDB$RELATION_FIELDS 
            WHERE RDB$RELATION_NAME = @T
            AND RDB$FIELD_SOURCE NOT LIKE 'RDB$%'
            ORDER BY RDB$FIELD_POSITION
        ";


        public const string CreateTable = "CREATE TABLE {0} (\n{1}\n);";
        public const string CreateField = "    {0} {1}";
        public const string CreateProcedure = "CREATE PROCEDURE {0} AS\n{1};";
        public const string CreateDomain = "CREATE DOMAIN {0} AS {1}";
        public const string AddColumn = "ALTER TABLE {0} ADD {1};";
    }
}
