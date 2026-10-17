# Contributing to OLED Sleeper

This guide covers reporting bugs, requesting features and sending pull requests.

## Reporting bugs and requesting features

Open an issue and choose **Bug report** or **Feature request**. Fill in every required field. Bug reports need the log file and hardware details; the form shows where to find them.

Search the existing issues, open and closed, before opening a new one. If yours already exists, add your details to it.

## Pull requests

### Which changes need an issue first

- **Bug fixes, typos and documentation:** open the pull request directly.
- **Everything else** (new features, new or changed settings, changes to existing behavior, refactors): open an issue first and wait for the maintainer to agree on the approach. Link the issue in your pull request. Pull requests in this category without an agreed issue will be closed.

Each pull request contains one change. Unrelated fixes go in separate pull requests.

### Branches

Create your branch from `develop` and open the pull request against `develop`. `master` holds released versions only.

```
git remote add upstream https://github.com/Quorthon13/OLED-Sleeper.git
git fetch upstream
git checkout -b my-change upstream/develop
```

### Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/): `type: subject`, lowercase, in the imperative, no trailing period, subject line only.

| Type       | Use for                                    |
|------------|--------------------------------------------|
| `feat`     | A new feature or setting                   |
| `fix`      | A bug fix                                  |
| `refactor` | A code change that doesn't change behavior |
| `test`     | Adding or changing tests                   |
| `docs`     | Documentation                              |
| `chore`    | Build, CI, dependencies and tooling        |

Describe what the change achieves:

```
fix: restore brightness when a dimmed monitor is disconnected
feat: add a tray option to pause idle detection
```

### Before opening the pull request

Run these from the repository root. All three must pass:

```
dotnet build OLED-Sleeper.sln
dotnet test
dotnet format OLED-Sleeper.sln --verify-no-changes
```

- The build must produce no warnings.
- If the format check fails, run `dotnet format OLED-Sleeper.sln` to fix it.
- If you change a command handler, add or update its tests in `OLED-Sleeper.Tests`, in the folder that mirrors the source file.
- In the pull request description, list the monitors and connections you tested on.

CI runs the build, tests and format check on every pull request.

## Development setup

- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022, Rider, or VS Code with C# Dev Kit

```
dotnet run --project OLED-Sleeper          # opens the settings window
dotnet run --project OLED-Sleeper -- -h    # starts hidden in the tray
```

Exit any installed copy of OLED Sleeper before running from source. Only one instance runs at a time, and both use the settings in `%APPDATA%\OLED-Sleeper`.

You don't need Inno Setup or `build.ps1` to contribute; they only build the release installer.

## Project layout

```
OLED-Sleeper/
  Features/<Name>/   Commands, Handlers, Services and Models for one feature
  Messaging/         The command bus (IMediator)
  Native/            All Win32 and DDC/CI P/Invoke
  Storage/           All file and folder access
  Infrastructure/    Startup, shutdown and WPF adapters
  UI/                Windows, view models and the tray icon
OLED-Sleeper.Tests/  xunit + Moq tests, folders mirror the app
```

- Features communicate by sending commands through `IMediator`, never by calling another feature's handler directly.
- P/Invoke declarations go in `Native/NativeMethods.cs`.
- File and folder access goes through `Storage/`.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
