using Syllanote.Application.Backups;

namespace Syllanote.Infrastructure.Backups;

internal static class LibraryRestorePaths
{
    public static string GetPendingDatabasePath(string liveDatabasePath)
    {
        var directory = Path.GetDirectoryName(liveDatabasePath)!;
        var extension = Path.GetExtension(liveDatabasePath);
        var fileName = Path.GetFileNameWithoutExtension(liveDatabasePath) +
            LibraryRestorePolicy.PendingRestoreMarker +
            extension;
        return Path.Combine(directory, fileName);
    }

    public static string GetPendingMetadataPath(string liveDatabasePath)
    {
        return GetPendingDatabasePath(liveDatabasePath) +
            LibraryRestorePolicy.PendingMetadataExtension;
    }
}
