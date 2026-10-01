namespace Syllanote.Application.Backups;

public sealed class InvalidLibraryBackupException : Exception
{
    public InvalidLibraryBackupException(string message)
        : base(message)
    {
    }

    public InvalidLibraryBackupException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
