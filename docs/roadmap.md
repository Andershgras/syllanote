# Syllanote Roadmap

Last reviewed: 2026-10-01

Current phase: **Now — Milestone 4: First versioned release**

This roadmap is the working plan for taking Syllanote from a functional local-first MVP to a reliable first portfolio release. It is intentionally focused: stability, data safety, accessibility, and release readiness take priority over additional product features.

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
| **Later** | Valuable work that should wait until the first release is stable. |
| **Ideas / Not committed** | Possible directions, not promises or scheduled work. |

## Current project status

Syllanote is a strong functional MVP in an alpha-level release state. The core note-taking workflow, failure handling, data-protection workflow, and accessibility baseline are implemented and verified, the solution has clear project boundaries, and the automated suite provides a useful safety net. The application is not yet release-ready because packaging and the versioned release workflow still need focused work.

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

- 160 of 160 automated tests pass.
- The latest Debug build after Milestone 3 completes with no warnings or errors.
- The most recent Release build and x64 file-system publish checks completed with no warnings or errors before the Milestone 3 accessibility changes and must be rerun for Milestone 4.
- The 15 `MVVMTK0045` warnings were removed by converting field-based `[ObservableProperty]` members to AOT-compatible partial properties.
- The normal WinUI workflow was manually regression-tested successfully on 2026-10-01.
- Unavailable-database startup handling and locked-database autosave recovery were manually verified on 2026-10-01.
- Manual backup creation and the confirmed close-and-reopen restore flow were verified on 2026-10-01.
- Keyboard-only, Narrator, 125%, 150%, and 200% display scaling, and high-contrast workflows were manually verified on 2026-10-01.
- Automated tests cover Domain, Application, and Infrastructure behavior, but do not drive the WinUI interface.

### Known release gaps

- Scheduled backup, backup encryption, selective restore, import, and export are not implemented.
- File-system publish profiles exist locally for x86, x64, and ARM64, but they are ignored by Git and are not reproducible from a clean checkout.
- App icons still use placeholder assets, and the public MSIX signing identity has not yet been verified against a trusted certificate.
- There is no version tag, release artifact, or continuous-integration workflow yet.

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

## Next

### Milestone 4 — First versioned release

**Work types:** Release work, documentation

**Status:** Now

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

---

## Later

### Milestone 5 — Portable content export

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

- Milestone 4 completed.
- The backup format and first-release data contracts must be stable.

#### Acceptance criteria

- Every page and concept in the selected notebook is represented in the export.
- Export does not modify live application data.
- Output can be read and navigated without Syllanote.
- Repeated exports produce predictable names and structure.
- Unsupported formatting and filename changes are reported clearly.

#### Verification

- Add automated export tests for hierarchy, filename handling, collisions, Unicode, and empty content.
- Manually inspect an export containing headings, lists, emphasis, concepts, and non-ASCII text.
- Open the exported Markdown in at least one independent Markdown viewer.
- Confirm that cancelling or failing an export leaves no misleading partial-success state.

---

## Ideas / Not committed

The following ideas are intentionally outside the committed roadmap. Each requires a separate product decision before design or implementation begins.

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
