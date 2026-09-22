using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;
using Syllanote.Infrastructure.Repositories;

namespace Syllanote.Tests.Application.Pages;

[TestClass]
public class PageSearchSqliteTests
{
    [TestMethod]
    public async Task Search_FindsTitleAndContentAcrossNotebooksWithCorrectPaths()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SyllanoteDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new SyllanoteDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var firstNotebook = new Notebook("Machine Learning");
        var secondNotebook = new Notebook("Databases");
        var firstSection = new Section(firstNotebook.Id, "Classification");
        var secondSection = new Section(secondNotebook.Id, "SQL");
        var titleMatch = new Page(firstSection.Id, "Confusion Matrix");
        var contentMatch = new Page(secondSection.Id, "Indexes");
        contentMatch.UpdateContent(
            "A confusion matrix example.",
            @"{\rtf1 A \b confusion matrix\b0 example.}");
        var otherPage = new Page(secondSection.Id, "Transactions");
        context.Notebooks.AddRange(firstNotebook, secondNotebook);
        context.Sections.AddRange(firstSection, secondSection);
        context.Pages.AddRange(titleMatch, contentMatch, otherPage);
        await context.SaveChangesAsync();

        var results = await new PageRepository(context).SearchAsync("confusion matrix");

        Assert.AreEqual(2, results.Count);
        var titleResult = results.Single(result => result.PageId == titleMatch.Id);
        Assert.AreEqual(firstSection.Id, titleResult.SectionId);
        Assert.AreEqual(firstSection.Name, titleResult.SectionName);
        Assert.AreEqual(firstNotebook.Id, titleResult.NotebookId);
        Assert.AreEqual(firstNotebook.Name, titleResult.NotebookName);
        var contentResult = results.Single(result => result.PageId == contentMatch.Id);
        Assert.AreEqual(secondSection.Id, contentResult.SectionId);
        Assert.AreEqual(secondNotebook.Id, contentResult.NotebookId);
        Assert.AreEqual("Indexes", contentResult.PageTitle);
        Assert.AreEqual(0, (await new PageRepository(context).SearchAsync("missing")).Count);
    }

    [TestMethod]
    public async Task Search_TreatsSqlWildcardsAsLiteralText()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SyllanoteDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new SyllanoteDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var notebook = new Notebook("Notebook");
        var section = new Section(notebook.Id, "Section");
        var percentPage = new Page(section.Id, "100% complete");
        var underscorePage = new Page(section.Id, "snake_case");
        var plainPage = new Page(section.Id, "100 complete snake case");
        var backslashPage = new Page(section.Id, "folder\\note");
        var danishPage = new Page(section.Id, "Årsplan");
        context.Notebooks.Add(notebook);
        context.Sections.Add(section);
        context.Pages.AddRange(percentPage, underscorePage, plainPage, backslashPage, danishPage);
        await context.SaveChangesAsync();

        var repository = new PageRepository(context);
        var percentResults = await repository.SearchAsync("%");
        var underscoreResults = await repository.SearchAsync("_");
        var backslashResults = await repository.SearchAsync("\\");
        var lowercaseDanishResults = await repository.SearchAsync("årsplan");

        Assert.AreEqual(percentPage.Id, percentResults.Single().PageId);
        Assert.AreEqual(underscorePage.Id, underscoreResults.Single().PageId);
        Assert.AreEqual(backslashPage.Id, backslashResults.Single().PageId);
        Assert.AreEqual(0, lowercaseDanishResults.Count);
    }
}
