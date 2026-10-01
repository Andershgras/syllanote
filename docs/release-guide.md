# Syllanote release guide

This guide applies to the portable x64 release of Syllanote `v0.1.0`.
The portable ZIP is the public fallback because a trusted certificate is not
currently available for direct MSIX distribution.

## System requirements

- A 64-bit edition of Windows 10 version 1809 or later
- Windows 11 is recommended
- Enough free space to extract the complete ZIP and store local notes

The release includes .NET and the Windows App SDK runtime. Visual Studio and a
separate .NET installation are not required.

## Download and verify

Download both of these files from the GitHub release:

- `Syllanote-v0.1.0-win-x64.zip`
- `Syllanote-v0.1.0-win-x64.zip.sha256`

Verify the ZIP before extracting it:

```powershell
Get-FileHash .\Syllanote-v0.1.0-win-x64.zip -Algorithm SHA256
Get-Content .\Syllanote-v0.1.0-win-x64.zip.sha256
```

The two hashes must match. Only run a release downloaded from the official
Syllanote repository. The first release is not code-signed, so Windows or an
organization policy may show a warning or block it. Do not disable security
controls to work around an organization policy.

## Install and start

1. Create or choose a folder where Syllanote should remain, such as
   `%LOCALAPPDATA%\Programs\Syllanote`.
2. Extract the complete ZIP into that folder.
3. Start `Syllanote.Desktop.exe` from the extracted folder.

Do not run the executable from inside the ZIP, and do not move the executable
without the files and folders beside it.

## Upgrade

1. Open the current version and create a manual backup.
2. Close every running Syllanote window.
3. Extract the new version into a new program folder.
4. Start the new `Syllanote.Desktop.exe`.
5. Confirm that the expected notebooks and pages open before deleting the old
   program folder.

Portable releases reuse the same local data directory, and pending database
migrations are applied at startup. Keep the package identity and the portable
publisher/product names stable between releases. If either must change, create
a backup first and verify restore before removing the previous data container.

## Uninstall or reinstall

To remove the application files, close Syllanote and delete the extracted
program folder. Local notes are intentionally left in place, so extracting and
starting the same release again behaves like a reinstall.

To remove Syllanote completely, first create and verify a manual backup, then
remove the local data folder described below. Removing that folder permanently
deletes the portable installation's live notes, concepts, restore state, safety
backups, and saved interface settings.

## Local data

The portable release stores its local data under:

```text
%LOCALAPPDATA%\Andershgras\Syllanote
```

The live library is stored in `syllanote.db`. Restore preparation files and the
`SafetyBackups` directory are stored beside it. Window placement and panel
widths are stored in the associated local settings store.

MSIX and portable installations use separate Windows data containers. Notes do
not automatically move between them; use Syllanote backup and restore instead.

## Create a backup

1. Select **Create backup** in the top bar.
2. Choose a destination and keep the `.syllanote-backup` extension.
3. Wait for the **Backup created** message before closing Syllanote.

A manual backup contains the complete library: notebooks, sections, pages,
plain and formatted note content, hierarchy order, and concepts. Store backups
outside the Syllanote program and local data folders.

## Restore a backup

1. Select **Restore backup** in the top bar.
2. Choose a `.syllanote-backup` file.
3. Review the confirmation and select **Restore and close**.
4. Wait for Syllanote to close.
5. Reopen the same `Syllanote.Desktop.exe` to finish the restore.

Before replacing the live library, Syllanote creates a safety backup and keeps
the three newest safety backups. If validation fails, the live database is not
replaced.

## Troubleshooting

### Syllanote does not start

- Confirm that the complete ZIP was extracted before starting the executable.
- Confirm that Windows is 64-bit and meets the minimum version above.
- Close other Syllanote processes and try again.
- Confirm that the program and local data drives have free space.
- If the startup error window appears, note the displayed database path and do
  not delete the database. Preserve that folder before attempting recovery.

### Notes appear to be missing

- Confirm that the same Windows user account is running Syllanote.
- Check whether you switched between the portable and MSIX versions, which use
  separate data containers.
- Restore a recent manual backup into the version you intend to keep using.

### Backup or restore fails

- Confirm that the selected file and destination folder are available.
- Confirm that both the destination and local data drives have free space.
- Do not edit or replace the backup while restore preparation is running.
- A failed restore preparation does not replace the live library.

## Known limitations

- Only 64-bit Windows is supported by the first release artifact.
- The portable release is not code-signed and has no installer or automatic
  updater.
- Notes remain local to one Windows user profile and device.
- Cloud sync and collaboration are not implemented.
- Scheduled backups, backup encryption, selective restore, import, and export
  are not implemented.
- Desktop interaction is manually regression-tested; the automated suite does
  not drive the WinUI interface.

## Build from source

The repository's `scripts/build-portable-release.ps1` script creates the
versioned ZIP and SHA-256 file. See the main README for the exact build and test
commands.
