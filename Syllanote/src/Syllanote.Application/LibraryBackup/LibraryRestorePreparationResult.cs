namespace Syllanote.Application.Backups;

public sealed record LibraryRestorePreparationResult(
    string SourceBackupPath,
    string SafetyBackupPath,
    string PendingDatabasePath,
    string PendingMetadataPath,
    DateTimeOffset PreparedAtUtc,
    LibraryBackupManifest Manifest);
