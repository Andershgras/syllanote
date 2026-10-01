namespace Syllanote.Application.Backups;

public interface ILibraryRestoreService
{
    Task<LibraryRestorePreparationResult> PrepareAsync(
        string backupPath,
        CancellationToken cancellationToken = default);
}
