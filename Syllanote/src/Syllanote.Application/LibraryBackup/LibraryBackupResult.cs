namespace Syllanote.Application.Backups;

public sealed record LibraryBackupResult(
    string FilePath,
    long SizeInBytes,
    DateTimeOffset CreatedAtUtc);
