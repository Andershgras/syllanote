namespace Syllanote.Application.Backups;

public static class LibraryRestorePolicy
{
    public const int SafetyBackupRetentionLimit = 3;
    public const string SafetyBackupDirectoryName = "SafetyBackups";
    public const string PendingRestoreMarker = ".restore-pending";
    public const string PendingMetadataExtension = ".json";
}
