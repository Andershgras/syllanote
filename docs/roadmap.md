# Syllanote Roadmap

Last reviewed: 2026-10-10

Current phase: **Milestone 5 — Rich-text editor v2**

This roadmap is the working plan for evolving the published local-first MVP into `v0.2.0 — Writing & Organization`. The next release is intentionally focused on a richer writing experience, clearer visual organization, and easier access to the notebook-scoped Concept Dictionary while preserving the stability, data safety, and accessibility established in `v0.1.0`.

## How to use this roadmap

- Keep only one milestone actively in progress unless a small documentation task is independent.
- Move a milestone to **Completed** only when its acceptance criteria and verification steps have been recorded.
- Do not treat a successful build or automated test run as proof that WinUI navigation, focus, rich-text editing, or autosave works correctly.
- Add new product features to **Ideas / Not committed** first. Moving one into the committed roadmap requires an explicit product decision.
- Prefer small changes that fit the current architecture. Do not add frameworks or abstractions without a demonstrated need.

## Status legend

| Status | Meaning |
| --- | --- |
| **Completed** | Implemented and verified to the level described by the milestone. |
| **Now** | The current priority and the only planned implementation focus. |
| **Next** | Committed work that follows the current milestone. |
| **Later** | Valuable work that should wait until the next planned release is complete. |
| **Ideas / Not committed** | Possible directions, not promises or scheduled work. |

## Current project status

Syllanote `v0.1.0` is published as the first versioned x64 Windows release. The core note-taking workflow, failure handling, data-protection workflow, accessibility baseline, portable packaging, and release process are implemented and verified. The solution has clear project boundaries, and the automated suite and Windows CI provide a useful safety net for further development.

### Implemented product areas

- Local notebooks, sections, and pages with create, rename, delete, and manual reordering.
- SQLite persistence through Entity Framework Core migrations.
- Rich-text page editing with headings, bold, italic, underline, and lists.
- Debounced autosave with navigation safeguards.
- Cross-notebook search of page titles and searchable plain text.
- Notebook-scoped Concept Dictionary with recognition, highlighting, definitions, references, and reference navigation.
- Versioned whole-library backups with integrity validation and explicit restart-based restore.
- Automatic pre-restore safety backups with retention limited to the three newest archives.
- Resizable navigation panels and persistent panel widths.
- Persistent window size, placement, and maximized state.
- Windows 11-inspired WinUI presentation with Mica, Fluent icons, and consistent empty states.
- Verified keyboard-only, Narrator, display-scaling, and high-contrast workflows.

### Current verification baseline

- 160 of 160 automated tests passed on 2026-10-09.
- The latest Debug and Release builds complete with no warnings or errors.
- The final self-contained `v0.1.0` x64 ZIP was rebuilt from `main` on 2026-10-09. SHA-256 `4470b4d699d250e730247772cf87146e0d4711c994523da615b78ecd65aeeefb` matches the published checksum file.
- Portable startup, page persistence after restart, executable branding, and the corrected Page move-up, move-down, and rename context-menu actions passed manual release checks.
- Windows CI passed for release commit `a84e586`, and the tagged release is published through [GitHub Releases](https://github.com/Andershgras/syllanote/releases/tag/v0.1.0).
- The portable release guide documents installation, startup, upgrade, uninstall, local data, backup, restore, troubleshooting, and known limitations.
- The 15 `MVVMTK0045` warnings were removed by converting field-based `[ObservableProperty]` members to AOT-compatible partial properties.
- The normal WinUI workflow was manually regression-tested successfully on 2026-10-01.
- Unavailable-database startup handling and locked-database autosave recovery were manually verified on 2026-10-01.
- Manual backup creation and the confirmed close-and-reopen restore flow were verified on 2026-10-01.
- Keyboard-only, Narrator, 125%, 150%, and 200% display scaling, and high-contrast workflows were manually verified on 2026-10-01.
- Automated tests cover Domain, Application, and Infrastructure behavior, but do not drive the WinUI interface.

### Known release gaps

- Scheduled backup, backup encryption, selective restore, import, and export are not implemented.
- A trusted public MSIX signing identity is not available, so `v0.1.0` uses the documented self-contained ZIP fallback.

### Next release direction

- Target `v0.2.0` as the next minor release under the theme **Writing & Organization**.
- Deliver Rich-text editor v2, persistent Section colors, and a visible Concept Dictionary entry point before beginning release stabilization.
- Keep Markdown export committed, but schedule it after `v0.2.0` so editor, navigation, persistence, and export risks are not combined in one release.
- Continue shipping x64 first through GitHub Releases with the documented portable ZIP fallback unless a trusted MSIX signing path becomes available.

---

## Completed

### Milestone 0 — Functional local-first MVP

**Work types:** Product features, architecture, stabilization

**Status:** Completed

#### Purpose

Establish a complete local note-taking workflow that is useful without accounts, cloud services, or an external database.

#### Scope

- Layered Domain, Application, Infrastructure, and Desktop projects.
- Notebook, section, page, and concept domain models.
- SQLite persistence and Entity Framework Core migrations.
- Hierarchical CRUD and persistent manual ordering.
- Rich-text editing and storage of both RTF and searchable plain text.
- Autosave, page-switch protection, and stale-navigation safeguards.
- Search, Concept Dictionary, recognition, highlighting, references, and reference navigation.
- UI component extraction, shared styling, empty states, and persistent workspace state.

#### Deliberately excluded

- Backup, import, and export.
- Cloud sync and collaboration.
- A complete accessibility audit.
- Public packaging and a versioned release.

#### Dependencies

None. This milestone is the baseline for all further work.

#### Acceptance criteria

- Users can create, organize, edit, search, and reopen local notes.
- Rich-text content, hierarchy order, and concepts persist between sessions.
- Search and concept references navigate to the expected page without overwriting another page's content.
- Workspace sizing and window placement persist between sessions.

#### Verification

- Current automated baseline: 137 of 137 tests passing.
- Current Debug and Release builds complete with no errors.
- Manual UI regression was completed after the navigation and UI-polish work.

---

### Milestone 1 — Stabilization and failure safety

**Work types:** Bug fixes, technical debt, stabilization

**Status:** Completed

**Estimated size:** Medium

#### Purpose

Make the existing MVP trustworthy under persistence errors, invalid states, rapid navigation, and application restart before adding another product feature.

#### Scope

- Replace the 15 warning-producing `[ObservableProperty]` fields with AOT-compatible partial properties without changing behavior.
- Add a clear user-facing outcome for startup and database migration failures.
- Handle failures from page loading, search, concept operations, and hierarchy CRUD at appropriate UI boundaries.
- Ensure an autosave failure keeps the page dirty and informs the user instead of failing silently.
- Provide consistent feedback for blank or invalid input and stale search or concept-reference results.
- Add focused automated tests for failure paths that belong in the Domain, Application, or Infrastructure layers.
- Create and maintain a concise manual WinUI regression checklist.

#### Deliberately excluded

- A new logging, telemetry, or crash-reporting platform.
- A broad MVVM rewrite or new UI architecture.
- A UI automation framework.
- Backup, export, tags, templates, or other product features.

#### Dependencies

- Milestone 0 completed.
- Existing navigation locks, selection-version checks, and autosave ownership must be preserved.

#### Acceptance criteria

- Debug, Release, and x64 publish complete with no errors and no warnings.
- All automated tests pass.
- Failed persistence never clears the dirty state as if the page were saved.
- Startup database failures produce an actionable message and a safe exit or recovery path.
- Invalid user input receives clear feedback instead of an unexplained no-op.
- Rapid page, search-result, and concept-reference navigation cannot save content to the wrong page.

#### Verification

- Run the full automated test suite.
- Build Debug and Release configurations and run the x64 publish check.
- Exercise a locked or unavailable database and an invalid migration/startup state.
- Manually test typing followed by rapid page changes, search navigation, concept-reference navigation, application close, and restart.
- Manually verify hierarchy CRUD, ordering, context menus, rich-text formatting, autosave, and persisted UI state.

#### Recorded verification

- 144 of 144 automated tests passed on 2026-10-01.
- Debug and Release builds and the x64 publish check completed with 0 warnings and 0 errors.
- The user completed a normal manual regression pass on 2026-10-01 and reported that the application continued to work.
- Automated failure-path coverage includes migration failures, failed page persistence, and restoration of entities after failed updates.
- An unavailable database produced the actionable startup error window without opening the editor, and the original database was restored without data loss.
- A SQLite write lock produced the autosave error; unsaved text remained visible, navigation was blocked, and saving succeeded after the lock was released.
- The reusable checklist is maintained in [`manual-regression-checklist.md`](manual-regression-checklist.md).

---

### Milestone 2 — Data protection v1

**Work types:** Release work, new product feature

**Status:** Completed

**Estimated size:** Medium

#### Purpose

Protect locally stored notes with a supported backup and recovery path before the first public release.

#### Scope

- Create a manual whole-library backup containing notebooks, sections, pages, rich-text content, ordering, and concepts.
- Use a versioned Syllanote backup format with schema or application-version metadata.
- Validate a backup before changing the live database.
- Create a safety backup of current data before restore.
- Restore a complete library through an explicit confirmation and restart flow.
- Document where live data and backup files are stored.

#### Deliberately excluded

- Scheduled or background backups.
- Backup encryption.
- Selective restore or merging two libraries.
- Markdown, PDF, or Word export.
- Cloud backup or sync.

#### Dependencies

- Milestone 1 error handling must be complete.
- Database lifetime and application restart behavior must be understood and stable.

#### Acceptance criteria

- A representative library can be backed up, removed, and restored without data loss.
- RTF content, searchable text, hierarchy order, concepts, and references survive the round trip.
- A corrupt, invalid, or unsupported newer backup is rejected without modifying live data.
- Restore cannot overwrite live data without explicit confirmation and a successful safety backup.

#### Verification

- Add integration tests using temporary SQLite databases and backup files.
- Perform a manual round trip with formatted notes, multiple notebooks, ordering, concepts, and references.
- Test corrupt files, unsupported versions, cancelled operations, and interrupted restore preparation.
- Repeat the critical navigation and autosave regression checks after restore.

#### Recorded verification

- 158 of 158 automated tests passed on 2026-10-01.
- Debug and Release builds and the x64 publish check completed with 0 warnings and 0 errors.
- A representative SQLite snapshot test verified notebooks, sections, pages, hierarchy order, searchable text, RTF content, concepts, backup metadata, migration history, and checksum.
- Validation tests rejected a changed database and an unsupported newer backup format without modifying live data.
- Restore integration tests verified safety-backup creation, three-file retention, pending restore application at startup, rejection of tampered data or a missing safety backup, and recovery after an interrupted finalization step.
- The user manually verified backup creation and the explicit confirmation, close, reopen, and restore workflow on 2026-10-01.
- WinUI file-picker and confirmation behavior remains a manual verification surface because the automated suite does not drive the desktop interface.

---

### Milestone 3 — Accessibility and keyboard readiness

**Work types:** Stabilization, release work

**Status:** Completed

**Estimated size:** Small to medium

#### Purpose

Make the critical note-taking workflows usable without a mouse and improve the quality of information exposed to Windows accessibility tools.

#### Scope

- Audit accessible names, help text, roles, selection states, and status messages.
- Establish a predictable tab order and visible keyboard focus.
- Support keyboard access to navigation, formatting, hierarchy actions, and concept workflows.
- Preserve and verify the existing `Ctrl+F` and Enter search behavior.
- Restore focus sensibly after dialogs, creation, deletion, and navigation.
- Verify the workspace under Windows high contrast and common display-scaling levels.

#### Deliberately excluded

- Formal accessibility certification.
- Full localization.
- A visual redesign or replacement of the existing navigation model.
- A new UI automation framework.

#### Dependencies

- Milestone 1 completed.
- The UI structure and critical workflows must be stable before the audit begins.

#### Acceptance criteria

- A user can complete the critical create, edit, navigate, search, save, and concept workflows with the keyboard.
- Narrator announces important controls, selection changes, and operation status meaningfully.
- Focus does not become lost or trapped after dialogs and navigation.
- The application remains usable at 125%, 150%, and 200% display scaling and in high contrast.

#### Verification

- Complete a documented keyboard-only walkthrough.
- Complete a manual Narrator walkthrough of the critical workflows.
- Check focus after every create, rename, delete, search, and navigation transition.
- Inspect the main workspace at the selected scaling levels and in high contrast.
- Rerun the automated tests and the relevant WinUI regression checks after changes.

#### Recorded verification

- 160 of 160 automated tests passed on 2026-10-01.
- The Debug build completed with 0 warnings and 0 errors after the accessibility changes.
- Keyboard-only and Narrator walkthroughs passed on 2026-10-01.
- The workspace passed manual checks at 125%, 150%, and 200% display scaling.
- High contrast passed after correcting editor text colors and hover states.

---

### Milestone 4 — First versioned release

**Work types:** Release work, documentation

**Status:** Completed

**Target:** `v0.1.0`, x64 first

**Estimated size:** Medium

#### Purpose

Produce a reproducible, installable, and portfolio-ready first release that can be evaluated without Visual Studio.

#### Scope

- Use `v0.1.0` as the first public Git tag and product version, with .NET assembly and file version `0.1.0.0`.
- Map the first product release to MSIX package version `1.0.0.0` and document that mapping in the release notes.
- Support x64 as the required first-release architecture.
- Update package identity, product name, publisher metadata, icons, and version values.
- Keep the package identity name and publisher stable after installation. Before any unavoidable identity change, create a manual backup and verify restore into the new package family because Windows treats it as a separate application data container.
- Produce MSIX as the primary release target.
- Allow a self-contained unpackaged ZIP as a fallback if public MSIX signing is not practical for the first portfolio release.
- Add a minimal Windows continuous-integration workflow for restore, build, and automated tests.
- Document installation, startup, upgrade, uninstall, local data, backup, restore, troubleshooting, and known limitations.
- Add release notes and accurate portfolio screenshots after the release UI has passed regression testing.
- Publish the versioned artifact through GitHub Releases.

#### Deliberately excluded

- Microsoft Store publication.
- Automatic updates.
- Telemetry or an external crash-reporting service.
- Required x86 or ARM64 release artifacts.
- New product features.

#### Dependencies

- Milestones 1, 2, and 3 completed.
- The manual regression checklist and backup/restore instructions must be current.

#### Acceptance criteria

- A versioned x64 artifact can be installed or run on a clean Windows user profile without Visual Studio.
- First launch, note creation, autosave, restart, search, concepts, backup, and restore work in the release build.
- An upgrade from a previous release candidate preserves user data.
- CI runs the documented restore, build, and test workflow successfully.
- README screenshots and release notes match the shipped application.
- Known limitations are stated clearly and do not promise unimplemented features.

#### Verification

- Build the release from a clean checkout using documented commands.
- Install and smoke-test it on another Windows user profile or machine.
- Run the full automated suite and manual WinUI regression checklist against the release build.
- Verify upgrade, uninstall, reinstall, and local-data behavior.
- Verify backup and restore using the packaged application.
- Confirm that the Git tag, artifact metadata, manifest version, release notes, and screenshots follow the documented version mapping.

#### Recorded verification

- 160 of 160 automated tests passed on 2026-10-09; Debug, Release, and x64 publish builds completed successfully.
- The final x64 artifact was built from `main` commit `a84e586`. Its SHA-256 is `4470b4d699d250e730247772cf87146e0d4711c994523da615b78ecd65aeeefb`, matching the published checksum file.
- Portable startup, page persistence across restart, and executable branding passed manual checks. After correcting the Page context-menu target, move up, move down, and rename were manually retested successfully.
- Windows CI completed successfully for release commit `a84e586`.
- Annotated tag `v0.1.0` and the release notes, portable ZIP, and checksum were published through [GitHub Releases](https://github.com/Andershgras/syllanote/releases/tag/v0.1.0) on 2026-10-09.
- README screenshots, release notes, installation guidance, and known limitations match the published portable release.

---

## Now

### Milestone 5 — Rich-text editor v2

**Work types:** New product feature, editor stabilization, accessibility

**Status:** Now

**Estimated size:** Large

#### Purpose

Give users more control over note presentation while preserving autosave, searchable plain text, Concept recognition, theme handling, and reliable RTF persistence.

#### Scope

- Add a deliberately small set of font-size choices.
- Add strikethrough formatting.
- Add left, center, and right paragraph alignment.
- Add increase-indent and decrease-indent actions.
- Add a clear-formatting action with a predictable result for selections and the insertion point.
- Show page word and character counts without storing derived counts in the database.
- Make the formatting toolbar usable at narrow window widths through a deliberate overflow or compact layout.
- Add controlled palettes for text color and text highlighting.
- Refactor color handling so intentional user colors persist while theme normalization and transient Concept highlighting remain separate concerns.
- Keep toolbar state synchronized with the current selection, including mixed-format selections where practical.

#### Product decisions

- Use a curated, accessible color palette rather than an unrestricted color picker in this milestone.
- User-applied text colors and highlights are persistent page formatting; Concept highlighting remains transient and must never be saved as user formatting.
- Preserve searchable plain text in `Page.Content` and RTF in `Page.FormattedContent`.
- Word and character counts are informational UI state, not new persisted domain properties.

#### Deliberately excluded

- Arbitrary installed font families.
- Tables, images, attachments, code blocks, and embedded media.
- A general Word-compatible editing surface.
- Markdown editing or round-trip Markdown conversion.

#### Dependencies

- Milestone 4 completed.
- Existing editor update guards, autosave ownership, selection preservation, and Concept recognition must be retained.
- The current RTF theme-color normalization and Concept background-highlighting paths must be separated before persistent foreground or highlight colors are enabled.

#### Acceptance criteria

- Every new format can be applied to selected text and to text typed after a collapsed selection.
- Supported formatting survives autosave, page navigation, application restart, backup, and restore.
- Search and Concept recognition continue to use correct plain text regardless of formatting.
- Concept highlighting never removes, replaces, or persists as a user's text highlight.
- Theme changes and high contrast do not make formatted text unreadable.
- The formatting toolbar remains reachable with mouse, keyboard, and Narrator at supported display scales and narrow window widths.
- Word and character counts update without marking an otherwise unchanged page as dirty.

#### Verification

- Add focused tests for any non-UI RTF color-preservation or normalization behavior.
- Run the full automated test suite, then Debug and Release builds sequentially.
- Manually test each format on selected text, a collapsed selection, mixed-format text, and empty pages.
- Verify save, navigation, restart, search, Concept recognition, Concept flyouts, theme changes, and high contrast with colored and highlighted text.
- Exercise undo and redo around every new formatting command even though dedicated undo and redo buttons are outside this milestone.
- Inspect the toolbar at 125%, 150%, and 200% display scaling and at the minimum supported window width.

---

## Next

### Milestone 6 — Persistent Section colors

**Work types:** New product feature, persistence, navigation UI

**Status:** Next

**Estimated size:** Medium

#### Purpose

Help users visually distinguish subjects and work areas without changing the existing Notebook → Section → Page hierarchy.

#### Scope

- Add an optional persisted color key to each Section.
- Provide a curated palette of approximately six to eight colors plus **No color**.
- Add a **Change color** action to the existing Section context menu.
- Represent the chosen color as a small marker or leading strip instead of coloring the entire navigation row.
- Preserve existing selection, hover, keyboard-focus, and high-contrast states.
- Include the new value in Entity Framework Core migrations, repository behavior, backup, restore, and upgrade verification.

#### Product decisions

- Persist a stable palette key rather than an arbitrary hex value so colors can be adapted safely across themes.
- Color supplements the Section name and selection state; it must never be the only way information is communicated.
- Existing Sections upgrade to **No color**.

#### Deliberately excluded

- Notebook and Page colors.
- Custom color creation or a full color picker.
- Automatic color assignment.
- Filtering, grouping, or search by color.

#### Dependencies

- Milestone 5 completed.
- The Section domain, persistence, application-service, and navigation binding paths must use one consistent color-key contract.

#### Acceptance criteria

- A user can assign, change, and remove a Section color.
- The selected color survives application restart, backup, and restore.
- A `v0.1.0` database migrates without data loss and gives existing Sections **No color**.
- Section rename, delete, ordering, selection, and Page navigation remain unchanged.
- Section rows remain understandable in light theme, dark theme, high contrast, and for users who cannot distinguish the palette colors.

#### Verification

- Add Domain, application-service, migration, and SQLite persistence tests for valid, missing, and unsupported color keys.
- Verify backup and restore with both colored and uncolored Sections.
- Manually test color selection, removal, navigation, ordering, rename, delete, restart, themes, keyboard use, and Narrator.
- Run the full automated suite and sequential Debug and Release builds.

---

### Milestone 7 — Concept Dictionary discoverability

**Work types:** Usability, navigation UI, accessibility

**Status:** Next

**Estimated size:** Small

#### Purpose

Make the existing Concept Dictionary discoverable without requiring users to know that the Notebook row has a right-click menu.

#### Scope

- Add a visible **Concepts** entry point with both icon and text in the Notebook navigation area.
- Make the action operate on the selected Notebook and disable it when no Notebook is selected.
- Keep the existing Notebook context-menu action as a secondary route.
- Add a documented keyboard accelerator, provisionally `Ctrl+Shift+D`.
- Add tooltip, accessible name, and help text that identify the selected-Notebook scope.
- Improve the empty state so first-time users understand that Concepts define terms and connect them to referenced Pages.
- Preserve the current **Back to page** behavior, selected Page, and predictable keyboard focus.

#### Product decisions

- Place the primary entry point in the Notebook navigation area because the Dictionary is notebook-scoped.
- Do not create a permanent fifth workspace column or a global cross-notebook Dictionary in this milestone.

#### Deliberately excluded

- Changing the notebook-scoped Concept data model.
- A global Concept Dictionary.
- Concept aliases, relationships, graphs, or automatic definition generation.
- A redesign of Concept CRUD or reference navigation.

#### Dependencies

- Milestone 6 completed.
- Existing navigation locks, autosave-before-navigation behavior, and focus restoration must be preserved.

#### Acceptance criteria

- A user can find and open the Dictionary without using a context menu.
- The UI clearly identifies which Notebook owns the displayed Concepts.
- Mouse, keyboard, and Narrator users can open the Dictionary and return to the current Page.
- Concept create, update, delete, recognition, reference loading, and reference navigation behave as before.
- Rapid navigation cannot open or modify Concepts for the wrong Notebook.

#### Verification

- Manually open the Dictionary through the visible entry point, context menu, and keyboard accelerator.
- Verify no-Notebook, empty-Dictionary, populated-Dictionary, and rapid Notebook-switch states.
- Repeat Concept CRUD, recognition, reference navigation, autosave, focus, keyboard, and Narrator checks.
- Run the full automated suite and sequential Debug and Release builds.

---

### Milestone 8 — `v0.2.0` stabilization and release

**Work types:** Stabilization, release work, documentation

**Status:** Next

**Target:** `v0.2.0`, x64 first

**Estimated size:** Medium

#### Purpose

Ship Rich-text editor v2, Section colors, and improved Concept Dictionary access as one coherent and reproducible minor release.

#### Scope

- Set product, assembly, file, and release metadata for `v0.2.0` using the established version mapping rules.
- Verify upgrade from the published `v0.1.0` portable release without losing notes, formatting, hierarchy, Concepts, ordering, or workspace settings.
- Verify migration and restore behavior with representative `v0.1.0` data and backups.
- Run the full automated, build, publish, and manual WinUI regression workflows.
- Update README feature documentation, screenshots, release notes, release guide, known limitations, and roadmap evidence.
- Build and publish the x64 portable ZIP and SHA-256 checksum through GitHub Releases.
- Use MSIX only if a trusted signing path is available without changing the stable package identity or weakening the documented fallback.

#### Deliberately excluded

- Portable Markdown export.
- Microsoft Store publication and automatic updates.
- Required x86 or ARM64 artifacts.
- Tags, templates, import, sync, and collaboration.
- Additional product features after release stabilization begins.

#### Dependencies

- Milestones 5, 6, and 7 completed with recorded verification.
- The manual regression checklist and upgrade fixtures must be current before producing the release candidate.

#### Acceptance criteria

- A representative `v0.1.0` library opens and migrates successfully in `v0.2.0` without data loss.
- New editor formatting, Section colors, and Concept access work in the published Release build.
- Existing create, edit, navigate, search, Concept, autosave, backup, and restore workflows remain functional.
- CI and the documented local release workflow pass from the release commit.
- Release notes, screenshots, version metadata, artifact checksum, tag, and known limitations match the shipped application.

#### Verification

- Run automated upgrade and persistence tests using representative pre-`v0.2.0` data.
- Run the full automated suite and clean Debug, Release, and x64 publish builds sequentially.
- Complete the full manual WinUI regression checklist against the release candidate.
- Smoke-test the portable artifact from a clean extraction path and verify persistence across restart.
- Verify backup creation and restoration across the supported version boundary.
- Confirm Git tag, GitHub Release, artifact, checksum, version metadata, screenshots, and documentation before marking the milestone complete.

---

## Later

### Milestone 9 — Portable content export

**Work types:** New product feature

**Status:** Later

**Estimated size:** Medium

#### Purpose

Give users a readable copy of their notes that does not require Syllanote.

#### Scope

- Export one notebook to a deterministic folder hierarchy.
- Use Markdown as the primary page-content format.
- Preserve notebook, section, and page names where the target file system permits it.
- Handle invalid filename characters and collisions consistently.
- Export Concept Dictionary content in a simple documented format.
- Explain any rich-text formatting that cannot be represented faithfully in Markdown.

#### Deliberately excluded

- Guaranteed round-trip reproduction of all RTF formatting.
- General Markdown import.
- PDF or Word generation.
- Merging exported content into an existing library.
- Sync or collaboration.

#### Dependencies

- Milestone 8 completed and `v0.2.0` published.
- The expanded editor format set and first post-release data contracts must be stable enough to define explicit Markdown conversion rules.

#### Acceptance criteria

- Every page and concept in the selected notebook is represented in the export.
- Export does not modify live application data.
- Output can be read and navigated without Syllanote.
- Repeated exports produce predictable names and structure.
- Unsupported formatting and filename changes are reported clearly.

#### Verification

- Add automated export tests for hierarchy, filename handling, collisions, Unicode, empty content, and every supported editor format.
- Manually inspect an export containing headings, lists, emphasis, colors, highlights, Concepts, and non-ASCII text.
- Open the exported Markdown in at least one independent Markdown viewer.
- Confirm that cancelling or failing an export leaves no misleading partial-success state.

---

## Ideas / Not committed

The following ideas are intentionally outside the committed roadmap. Each requires a separate product decision before design or implementation begins.

### Search scope and result previews

Allow users to choose between the selected Notebook and all Notebooks, and show a clearer matching-text preview. This should be designed as one coherent search improvement so filtering, keyboard navigation, stale-result handling, and accessibility remain consistent.

### Favorite Pages

Pinned or favorite Pages could provide fast access to frequently used notes. This requires a persistence decision, ordering behavior, an obvious navigation location, and rules for rename, delete, backup, and restore.

### Duplicate Page

Duplicating a Page could make repeated note structures faster without committing to a full template system. A first version would need clear naming, ordering, RTF-copy, plain-text-copy, Concept-recognition, and rollback behavior.

### Tags

Potentially useful for cross-notebook organization, but would require a data migration, tag-management UX, filtering rules, and integration with search. It is a possible candidate for the first post-release product milestone.

### Templates

Could improve repeated note creation, but should not be added until the creation workflow and portable data formats are stable. A first version would need a deliberately small template model rather than a general plugin system.

### Import

General Markdown or text import should be designed only after the export format has been used and reviewed. Import introduces mapping, duplicate-name, merge, validation, and rollback decisions that backup/restore does not solve.

### Sync

Sync is not planned within the current roadmap horizon. A credible local-first implementation would require identity, remote storage, encryption decisions, conflict resolution, offline semantics, migration compatibility, and recovery from partial synchronization.

### Collaboration

Real-time or shared editing is a separate product direction rather than a small extension of sync. It is not committed.

## Cross-cutting definition of done

A future milestone is complete only when all applicable items below are satisfied:

- The agreed scope and acceptance criteria are implemented.
- Automated tests pass, and new non-UI behavior has appropriate tests.
- Debug and Release builds have no unexpected warnings or errors.
- The milestone-specific manual WinUI checks pass.
- Navigation and autosave regressions are checked after any UI or persistence change.
- User-facing documentation and known limitations are updated.
- The working tree contains no unrelated changes.
- Verification evidence and any remaining limitations are recorded before the milestone is marked **Completed**.
