# Manual WinUI Regression Checklist

Use this checklist after changes to navigation, persistence, startup, or the main workspace. Automated tests and successful builds do not replace these checks.

## Core workflow

- Create, rename, reorder, and delete a notebook, section, and page.
- Use the hierarchy context menus and confirm that selection remains on the expected item.
- Type on one page, switch pages quickly, and confirm that content is saved to the correct page.
- Apply headings, font sizes, bold, italic, underline, strikethrough, bullets,
  numbering, alignment, indentation, text colors, and highlighting. Clear
  formatting from both selected text and the insertion point; restart the
  application and confirm that the supported formatting remains.
- Confirm that word and character counts update while editing without causing
  an otherwise unchanged Page to save.
- Assign, change, and remove a Section color. Confirm that the marker survives
  restart without replacing selection, hover, or keyboard-focus feedback.
- Search by title and content, open a result, and confirm that the expected page is selected.
- Open the **Concepts** workspace dropdown before and after selecting a Notebook,
  choose another Notebook directly, and verify `Ctrl+Shift+D`. Create, edit, and
  delete a Concept; open a Concept reference and confirm that it navigates to
  the expected Page. Confirm that **Back to page** restores the prior Page.
- Resize the navigation panels and move, resize, and maximize the window; restart and confirm that the workspace state is restored.
- Close the application with pending edits and confirm that the latest content is present after restart.

## v0.2.0 release-candidate upgrade

- Open a representative library last used by the published `v0.1.0` portable
  build. Confirm that notebooks, Sections, Pages, plain text, RTF formatting,
  hierarchy order, and Concepts remain intact after the first `v0.2.0` start.
- Confirm that pre-existing Sections initially show **No color**, then assign a
  color and verify it after restart.
- Create a backup in `v0.1.0`, restore it through `v0.2.0`, reopen the
  application, and repeat the library-content checks.
- Confirm that panel widths, window placement, and maximized state remain usable
  after upgrading.
- Extract the final portable ZIP into a clean folder, start it from that folder,
  edit a Page, restart, and confirm that the edit remains.

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
- Reach every formatting control and the narrow-width overflow menu from the
  keyboard, apply each format, and continue typing in the editor without losing
  the selection or insertion point. Verify `Ctrl+B`, `Ctrl+I`, and `Ctrl+U` as
  well.
- Open the **Concepts** dropdown with both keyboard navigation and
  `Ctrl+Shift+D`, choose a Notebook, create and edit a Concept, open a reference,
  and return to the Page editor using only the keyboard.
- After creating, renaming, deleting, cancelling a dialog, or following a search or concept reference, confirm that focus is visible and returns to a sensible related control.

### Narrator workflow

- Confirm that backup, restore, search, create, formatting, Section color,
  **Concepts**, save, delete, and back controls have meaningful names, help text,
  and control types.
- Confirm that notebook and section controls announce their name plus current selection and expanded or collapsed status.
- Confirm that page, search-result, concept, and concept-reference lists announce their purpose, current item, and selection changes.
- Trigger a search message, concept validation message, successful operation, and save error. Confirm that each important status is announced once and is understandable without visual context.
- Open each create, rename, delete, unsaved-changes, and restore dialog. Confirm that its title, input label, buttons, and default action are announced meaningfully.

### Display and contrast

- With Windows app mode set to dark, create a Page, apply foreground colors and
  highlighting, navigate away, reopen the Page, and restart the application.
  Confirm that new and restored editor text remains readable.
- Switch Windows app mode between dark and light while Syllanote is open. Confirm
  the documented WinUI limitation: highlighting remains visible, foreground
  colors can temporarily render as the automatic theme foreground, stored RTF
  remains unchanged, and the selected colors return after restarting Syllanote.
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

### Milestone 4 portable release verification record

| Date | Artifact | Startup and persistence | Branding | Automated verification |
| --- | --- | --- | --- | --- |
| 2026-10-01 | Self-contained `Syllanote-v0.1.0-win-x64.zip` built with checksum | Passed — the extracted application opened, saved a page, and reloaded it correctly after restart | Passed — the Syllanote icon appeared on the executable | 160/160 tests passed; x64 Release publish completed successfully |
| 2026-10-09 | Final `Syllanote-v0.1.0-win-x64.zip` rebuilt from `main` commit `a84e586`, published through GitHub Releases, and verified against SHA-256 `4470b4d699d250e730247772cf87146e0d4711c994523da615b78ecd65aeeefb` | Passed — portable startup and page persistence across restart passed; Page move up, move down, and rename were retested successfully after correcting the context-menu target | Passed — the Syllanote icon appeared on the published executable | 160/160 tests passed; Debug, Release, and x64 publish builds passed; Windows CI passed for `a84e586` |

### v0.2.0 release-candidate display verification record

| Date | Scaling | High contrast | Live theme change | Notes |
| --- | --- | --- | --- | --- |
| 2026-10-12 | Passed at 125%, 150%, and 200% | Passed | Passed | User-confirmed display checks on the release candidate. Page actions also opened with `Shift+F10`, and `Escape` closed the menu, on the corrected Debug build. |

### v0.2.0 portable release-candidate verification record

| Date | Artifact | Startup and restart | Build verification | Manual coverage exception |
| --- | --- | --- | --- | --- |
| 2026-10-12 | Local `Syllanote-v0.2.0-win-x64.zip` built from commit `8c54467`; SHA-256 `005fe2921a871995abd80f73bdeedfaef5c5ff85d55c1dde944dd304a74b6b98` matched the checksum file | Passed from a new extraction directory; the existing library loaded before and after restart | Clean x64 Debug and Release builds passed with 0 warnings and 0 errors; self-contained x64 publish passed | Narrator and unavailable-database failure-safety checks were explicitly not repeated for this candidate; earlier `v0.1.0` results remain historical evidence only |
