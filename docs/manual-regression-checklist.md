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

## Accessibility and keyboard readiness

Complete this section without using a mouse. Repeat the Narrator checks with Windows Narrator running.

### Keyboard-only workflow

- Use `Tab` and `Shift+Tab` through the top bar, notebooks and sections, pages, formatting controls, editor, and Concept Dictionary. Confirm that the order is predictable and keyboard focus is always visible.
- Create, rename, reorder, and delete a notebook, section, and page. Open hierarchy actions with `Shift+F10` or the Menu key and confirm that `Escape` closes the menu or dialog without changing data.
- Move between notebooks, sections, and pages with Enter, Space, and the arrow keys where appropriate. Confirm that page content is saved to the correct page.
- Press `Ctrl+F`, enter a query, and press `Enter`. Use the arrow keys to move between search results without opening them, then press `Enter` to open the selected result. Confirm that focus remains in a useful location after navigation.
- Reach every formatting control from the keyboard, apply each format, and continue typing in the editor without losing the selection or insertion point. Verify `Ctrl+B`, `Ctrl+I`, and `Ctrl+U` as well.
- Open the Concept Dictionary, create and edit a concept, open a reference, and return to the page editor using only the keyboard.
- After creating, renaming, deleting, cancelling a dialog, or following a search or concept reference, confirm that focus is visible and returns to a sensible related control.

### Narrator workflow

- Confirm that backup, restore, search, create, formatting, save, delete, and back controls have meaningful names and control types.
- Confirm that notebook and section controls announce their name plus current selection and expanded or collapsed status.
- Confirm that page, search-result, concept, and concept-reference lists announce their purpose, current item, and selection changes.
- Trigger a search message, concept validation message, successful operation, and save error. Confirm that each important status is announced once and is understandable without visual context.
- Open each create, rename, delete, unsaved-changes, and restore dialog. Confirm that its title, input label, buttons, and default action are announced meaningfully.

### Display and contrast

- Inspect the complete workspace at 125%, 150%, and 200% Windows display scaling. Confirm that controls remain reachable, text is not clipped, and important content can scroll.
- Enable a Windows high-contrast theme and confirm that text, selection, keyboard focus, borders, editor formatting controls, errors, and disabled states remain distinguishable.
- Return Windows to the original scaling and theme, then repeat a short create, edit, navigate, and autosave check.

## Verification record

| Date | Automated verification | Manual core workflow | Manual failure safety |
| --- | --- | --- | --- |
| 2026-10-01 | 144/144 tests; Debug and Release builds plus x64 publish passed with 0 warnings and 0 errors | Passed — user reported that the manually tested application continued to work | Passed — unavailable-database startup and locked-database autosave recovery were manually verified |

### Milestone 3 accessibility verification record

| Date | Keyboard-only | Narrator | Scaling | High contrast | Notes |
| --- | --- | --- | --- | --- | --- |
| 2026-10-01 | Passed | Passed | Passed | Passed | Keyboard-only and Narrator walkthroughs passed. Display scaling passed at 125%, 150%, and 200%. High contrast passed after correcting editor text colors and hover states. Debug build passed with 0 warnings and errors; 160/160 automated tests passed. |
