namespace Syllanote.Application.Backups;

public interface ILibraryBackupValidator
{
    Task<LibraryBackupValidationResult> ValidateAsync(
        string backupPath,
        CancellationToken cancellationToken = default);
}
