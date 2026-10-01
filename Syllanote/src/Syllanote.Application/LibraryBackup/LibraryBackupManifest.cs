namespace Syllanote.Application.Backups;

public sealed record LibraryBackupManifest(
    string Product,
    int FormatVersion,
    DateTimeOffset CreatedAtUtc,
    string DatabaseEntry,
    long DatabaseSizeInBytes,
    string DatabaseSha256,
    string[] AppliedMigrations);
