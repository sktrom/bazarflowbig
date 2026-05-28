namespace BazarFlow.DbMigrator;

public static class MigrationExitCodes
{
    public const int Success = 0;
    public const int MissingConnectionString = 1;
    public const int SqlConnectionFailed = 2;
    public const int MigrationFailed = 3;
    public const int DuplicateBarcodeBlocksMigration = 4;
    public const int UnexpectedError = 5;
}
