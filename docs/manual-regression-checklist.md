# Manual WinUI Regression Checklist

Use this checklist after changes to navigation, persistence, startup, or the main workspace. Automated tests and successful builds do not replace these checks.

## Core workflow

- Create, rename, reorder, and delete a notebook, section, and page.
- Use the hierarchy context menus and confirm that selection remains on the expected item.
- Type on one page, switch pages quickly, and confirm that content is saved to the correct page.
- Apply headings, bold, italic, underline, bullets, and numbering; restart the application and confirm that formatting remains.
- Search by title and content, open a result, and confirm that the expected page is selected.
- Create, edit, and delete a concept; open a concept reference and confirm that it navigates to the expected page.
- Resize the navigation panels and move, resize, and maximize the window; restart and confirm that the workspace state is restored.
- Close the application with pending edits and confirm that the latest content is present after restart.

## Failure safety

- Make the SQLite database temporarily unavailable, then trigger a save. Confirm that the page remains dirty, an error is shown, and navigation does not overwrite another page.
- Start the application with the database unavailable or an invalid migration state. Confirm that an actionable startup message is shown and the main window does not open in a partially initialized state.
- After restoring database access, restart and repeat the core create, edit, navigation, and search workflow.

## Verification record

| Date | Automated verification | Manual core workflow | Manual failure safety |
| --- | --- | --- | --- |
| 2026-10-01 | 144/144 tests; Debug and Release builds plus x64 publish passed with 0 warnings and 0 errors | Passed — user reported that the manually tested application continued to work | Pending — forced database and startup failures have not been exercised manually |
