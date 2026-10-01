namespace Syllanote.Application.Backups;

public interface ILibraryBackupService
{
    Task<LibraryBackupResult> CreateAsync(
        string destinationPath,
        CancellationToken cancellationToken = default);
}
