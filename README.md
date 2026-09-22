# Syllanote

A local-first desktop note-taking application for students with organized notebooks, sections, pages, and an integrated concept dictionary.

## Tech stack

- C# and .NET 9
- WinUI 3 with Windows App SDK
- CommunityToolkit.Mvvm
- Entity Framework Core with SQLite
- Microsoft.Extensions.DependencyInjection
- MSTest
- Layered Domain, Application, Infrastructure, and Desktop projects

## Features

- Create, rename, and delete notebooks, sections, and pages
- Edit and autosave page content with persistent rich-text formatting
- Search page titles and content across all notebooks
- Create, update, and delete Concepts for each notebook
- Recognize Concepts in page content
- Highlight recognized Concepts in the page editor
- Click a highlighted Concept to view its definition
- Persist notebooks, sections, pages, and Concepts locally in SQLite
- Manually reorder notebooks and persist their order locally
