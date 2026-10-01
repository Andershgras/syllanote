namespace Syllanote.Application.Backups;

public sealed record LibraryRestorePendingMetadata(
    string SourceBackupPath,
    string SafetyBackupPath,
    DateTimeOffset PreparedAtUtc,
    LibraryBackupManifest Manifest);
