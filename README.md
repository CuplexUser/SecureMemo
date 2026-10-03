# Secure Memo

A Windows desktop notepad that keeps everything you write in an encrypted, password-protected
database. Notes are organized in tabs, and an encrypted File Manager can hold files alongside
them.

## Features

- **Tabbed notes:** add, rename, reorder and delete tabs (**Tabs > Tab Window**, or right-click a tab).
- **Encryption:** the database is compressed and encrypted with a password you choose. The password
  is never stored; only a salted hash used to check it when you open the database.
- **Find:** search the current tab or all tabs, up or down, with optional case sensitivity.
- **Backups:** make a backup of the database and restore any earlier one.
- **Sync folder:** save the database and its settings to a folder of your choice (for example a
  Dropbox or OneDrive folder) and restore them on another computer.
- **File Manager:** store files inside an encrypted container, organized in folders. See
  [File Manager](#file-manager).
- **Settings:** font, number of tabs in a new database, always-on-top, and the sync folder.

## Requirements

- Windows 10 or later
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build (the app targets
  `net10.0-windows7.0` and uses Windows Forms)

## Building and running

```powershell
dotnet build SecureMemo.sln -c Release
dotnet run --project SecureMemo -c Release
```

Only one instance runs at a time; starting a second one brings the first to the front.

The `Installer` folder holds an older WiX setup project. It is not part of the solution build.

## Getting started

1. **File > Create New Database** and choose a password. Passwords need at least 8 characters, with
   an uppercase letter, a lowercase letter and a digit, and no spaces.
2. Write in the tabs and save (**File > SaveDatabase**, Ctrl+S).
3. Next time, use **File > Open Database** (Ctrl+O) and enter the same password.

Backups, the sync folder, settings and Change Password are under the **Options** menu.

There is no way to recover a forgotten password.

## Where data is stored

Release builds keep their data in `%APPDATA%\SecureMemo\`. Debug builds use the folder the
executable runs from, so a development build never touches your real data.

| File | Contents |
| --- | --- |
| `MemoDatabase.dat` | The encrypted notes database |
| `FileStorage.dat` | The encrypted File Manager container (created when you first add something) |
| `ApplicationSettings.ini` | Settings, the application salt and the password check value |
| `backup\*MemoDatabase.dat` | Database backups |
| `SecureMemo<date>.log` | Log files (warnings and errors only in Release builds) |

Saving to the sync folder writes `MemoDatabase.dat` and an encrypted `ApplicationSettings.dat`
there.

## File Manager

The **File Manager** menu is available while a database is open, because stored files are
encrypted with the database password. **File Manager > Open File Manager** opens the window.

- **Folders:** right-click the folder tree to create, rename or delete folders. Folder names may
  contain letters, digits, `.`, `_` and `-`.
- **Adding files:** drag files from Explorer onto the file list, or use **File > Add Files...**. A
  file whose name is already taken in that folder is stored as `name (2).ext`. Files are limited to
  64 MB each.
- **Reading files:** **Export...** saves a decrypted copy wherever you choose (select several files
  to export them to a folder). **Open** (or double-click) decrypts a read-only copy into a private
  temporary folder and opens it in its default application. Those copies are deleted when the File
  Manager closes, or the next time Secure Memo starts if a file was still in use. Changes made to an
  opened copy are not saved back.
- Every change is saved immediately.
- **File Manager > Export File Database** writes decrypted copies of all stored files, with their
  folders, into a new folder. **Clear File Database** permanently deletes everything stored.

Notes:

- **Change Password** re-encrypts the stored files along with the database.
- **Create New Database** deletes the stored files, since they belong to the previous database's
  password.
- Backups and the sync folder cover the notes database only, not the File Manager container.

## Security notes

- Data is compressed with LZMA, then encrypted with AES-256 in CBC mode. The key and IV are derived
  from the password with PBKDF2 (HMAC-SHA1, 1000 iterations, fixed salt).
- When you open a database, the password is checked against a salted, iterated SHA-512 value
  stored in `ApplicationSettings.ini`.
- While the app runs, the database password is held in memory.

These choices are kept so existing databases keep opening; the unit tests include a database file
written by an earlier version to guard that compatibility. By current standards the key derivation
is weak (few iterations, a salt shared by all users, and an IV that only depends on the password),
so treat Secure Memo as protection against casual access rather than a determined attacker.

## Tests

```powershell
dotnet test SecureMemo.sln -c Release
```

The MSTest project in `UnitTests` covers the logic behind the UI: the tab and database logic,
storage and encryption, settings, search, backup/sync, and the File Manager's storage. The forms
themselves are not unit tested. The tests build the application's real Autofac container,
pointed at a temporary folder, so dependency-injection wiring is tested too, and nothing touches
your real data.

### Coverage

```powershell
dotnet test UnitTests -c Release --settings coverage.runsettings --results-directory TestResults
```

This writes a Cobertura report under `TestResults\`. `coverage.runsettings` limits the measurement
to the code the UI depends on: forms, designer files, user controls, `Program.cs` and the vendored
7-Zip LZMA SDK are excluded. Open the report with any Cobertura viewer, for example
[ReportGenerator](https://github.com/danielpalme/ReportGenerator).

## Project layout

| Folder | Contents |
| --- | --- |
| `SecureMemo\` (root) | The main window and dialogs (`FormMain`, `FormFileManager`, ...) |
| `SecureMemo\Managers` | `MainFormLogicManager`: the tab and database logic behind the main window |
| `SecureMemo\Services` | Database, settings and File Manager storage |
| `SecureMemo\DataModels`, `FileStorageModels` | Persisted data |
| `SecureMemo\TextSearchModels` | The Find engine |
| `SecureMemo\Toolkit` | Encryption, compression, serialization and INI file helpers |
| `SecureMemo\Configuration`, `Library\AutofacModules` | Autofac and logging setup |
| `UnitTests\` | MSTest tests, grouped like the main project |
