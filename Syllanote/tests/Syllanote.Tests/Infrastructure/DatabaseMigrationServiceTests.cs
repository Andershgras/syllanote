using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Infrastructure.Persistence;

namespace Syllanote.Tests.Infrastructure;

[TestClass]
public class DatabaseMigrationServiceTests
{
    [TestMethod]
    public async Task MigrateAsync_AppliesAllMigrations()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        var migrationService = new DatabaseMigrationService(context);

        await migrationService.MigrateAsync();

        var expectedMigrations = context.Database.GetMigrations().ToArray();
        var appliedMigrations =
            (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        CollectionAssert.AreEqual(expectedMigrations, appliedMigrations);
    }

    [TestMethod]
    public async Task MigrateAsync_WhenDatabaseCannotBeOpened_PropagatesFailure()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"syllanote-{Guid.NewGuid():N}",
            "missing",
            "syllanote.db");
        var options = new DbContextOptionsBuilder<SyllanoteDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
        await using var context = new SyllanoteDbContext(options);
        var migrationService = new DatabaseMigrationService(context);

        await Assert.ThrowsExceptionAsync<SqliteException>(
            async () => await migrationService.MigrateAsync());
    }

    private static SyllanoteDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<SyllanoteDbContext>()
            .UseSqlite(connection)
            .Options;

        return new SyllanoteDbContext(options);
    }
}
