namespace Syllanote.Application.Backups;

public sealed record LibraryRestorePreparationResult(
    string SourceBackupPath,
    string SafetyBackupPath,
    string PendingDatabasePath,
    DateTimeOffset PreparedAtUtc,
    LibraryBackupManifest Manifest);
