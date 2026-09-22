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
- Write notes in a dedicated page editor with a separate title field
- Autosave page content and rich-text formatting locally
- Format paragraphs as Normal, Heading 1, or Heading 2
- Toggle bold, italic, and underline formatting from a persistent toolbar
- Create and remove bulleted and numbered lists
- Search page titles and content across all notebooks
- Create, update, and delete Concepts for each notebook
- Recognize Concepts in page content
- Highlight recognized Concepts in the page editor
- Click a highlighted Concept to view its definition
- Persist notebooks, sections, pages, and Concepts locally in SQLite
- Manually reorder notebooks and sections and persist their order locally

### Rich-text editor

The page editor stores both searchable plain text and Rich Text Format (RTF).
This keeps formatting persistent across autosave, page navigation, and app
restarts without affecting page search or Concept recognition.

The formatting toolbar currently supports:

- Normal text, Heading 1, and Heading 2 paragraph styles
- Bold, italic, and underline toggles that follow the current cursor position
- Bulleted and numbered lists that can be converted back to plain paragraphs
