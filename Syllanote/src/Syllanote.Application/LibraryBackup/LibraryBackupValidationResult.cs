namespace Syllanote.Application.Backups;

public sealed record LibraryBackupValidationResult(
    string FilePath,
    long SizeInBytes,
    LibraryBackupManifest Manifest);
