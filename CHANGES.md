# Change log (working notes)

## Build (quick reference)

From the repository root:

```bash
dotnet build "\\tower.local\Repos\OLED-Sleeper\OLED-Sleeper.sln" -c Release
```

Optional: run tests with `dotnet test OLED-Sleeper.sln`.

---

**Important:** This file was **generated with AI assistance**. All content and all code changes described here **must be reviewed thoroughly** by a human before you rely on them for releases, compliance, or production use. Treat this document as informal notes, not an audited changelog.

---

## Session: monitor topology debounce & idle sync refactor

These changes target flicker / black flashes when a secondary display wakes after idle “sleep” behavior, by reducing spurious topology syncs and avoiding tearing down the idle-detection loop on every sync.

### 1. Topology debouncing & hysteresis (`MonitorStateWatcher`)

- **File:** `OLED-Sleeper/Features/MonitorState/Services/MonitorStateWatcher.cs`
- **Behavior:** A layout change is committed only after the same monitor topology (same device-name set and count) is observed on **`RequiredStableTopologyPolls` (3) consecutive polls** at the existing **2 second** poll interval (~6 seconds of stability before sync).
- **Effect:** Brief enumeration glitches during display wake (e.g. monitor briefly missing from `EnumDisplayMonitors`) should no longer trigger immediate `SynchronizeMonitorStateCommand` storms.
- **Initial startup:** The first sync from `MonitorListReady` is still **immediate** (not debounced).
- **Snapshots:** Committed and pending topologies use **cloned** `MonitorInfo` lists so enrichment does not mutate stored state.

### 2. Idle detection without stop/start on topology sync

- **Files:**
  - `OLED-Sleeper/Features/MonitorIdleDetection/Services/Interfaces/IMonitorIdleDetectionService.cs`
  - `OLED-Sleeper/Features/MonitorIdleDetection/Services/MonitorIdleDetectionService.cs`
  - `OLED-Sleeper/Features/MonitorState/Handlers/SynchronizeMonitorStateCommandHandler.cs`
- **New API:** `ApplyTopologyAndSettings(List<MonitorSettings>, IReadOnlyList<MonitorInfo>)` rebuilds managed monitors and resets per-monitor timer state **under the existing service lock** while the background idle loop keeps running.
- **Refactor:** Shared `ApplyManagedMonitorsAndResetTimersUnlocked` used by both `UpdateSettings` (async cache path) and `ApplyTopologyAndSettings` (sync path with live enriched list from the watcher).
- **`Start()`:** Now **idempotent** (guarded with `_lock` so only one loop is started). `Stop()` also uses `_lock` for consistency.
- **Handler:** `SynchronizeMonitorStateCommandHandler` no longer calls `idleDetectionService.Stop()` or `UpdateSettings()` for topology sync; it updates cache, calls `ApplyTopologyAndSettings`, then `Start()`.

### 3. Monitor info cache aligned with sync

- **Files:**
  - `OLED-Sleeper/Features/MonitorInformation/Services/Interfaces/IMonitorInfoManager.cs`
  - `OLED-Sleeper/Features/MonitorInformation/Services/MonitorInfoManager.cs`
- **New API:** `UpdateCachedMonitorsFromSnapshot(IReadOnlyList<MonitorInfo>)` copies enriched monitors into `_cachedMonitors` **without** raising `MonitorListReady`.
- **Handler:** Invoked after topology sync so `GetCurrentMonitorsAsync()` consumers see a cache consistent with the committed topology.

### 4. Dependency injection

- **`SynchronizeMonitorStateCommandHandler`** now takes **`IMonitorInfoManager`** in addition to existing dependencies (primary constructor). `ServiceConfigurator` already registers `IMonitorInfoManager`; no registration change was required for a successful build in this session.

---

## Outstanding / uncommitted work (Git)

**Note:** In the environment where this file was written, `git status` output was not captured reliably. **Run these locally** for the authoritative picture:

```bash
git status
git diff
```

### Snapshot of other modified paths (from an earlier workspace `git status`)

The following paths were already modified **before or outside** the topology/idle session above. They may still be dirty, may have been committed, or may overlap with the files listed in this document—**verify with `git status`**:

| Path |
|------|
| `OLED-Sleeper.Tests/Features/MonitorBlackout/Handlers/HideBlackoutOverlayCommandHandlerTests.cs` |
| `OLED-Sleeper/Features/MonitorBehavior/Commands/RestoreMonitorStateCommand.cs` |
| `OLED-Sleeper/Features/MonitorBlackout/Handlers/ApplyBlackoutOverlayCommandHandler.cs` |
| `OLED-Sleeper/Features/MonitorBlackout/Handlers/HideBlackoutOverlayCommandHandler.cs` |
| `OLED-Sleeper/Features/MonitorDimming/Handlers/ApplyDimCommandHandler.cs` |
| `OLED-Sleeper/Features/MonitorDimming/Handlers/ApplyUndimCommandHandler.cs` |
| `OLED-Sleeper/Features/MonitorDimming/Services/Interfaces/IMonitorDimmingService.cs` |
| `OLED-Sleeper/Features/MonitorDimming/Services/MonitorDimmingService.cs` |
| `OLED-Sleeper/Features/MonitorIdleDetection/Models/ActivityReason.cs` |
| `OLED-Sleeper/Features/MonitorIdleDetection/Services/MonitorIdleDetectionService.cs` *(also edited in the session above)* |
| `OLED-Sleeper/Features/MonitorInformation/Services/Interfaces/IMonitorInfoManager.cs` *(also edited in the session above)* |
| `OLED-Sleeper/Features/MonitorInformation/Services/MonitorInfoManager.cs` *(also edited in the session above)* |
| `OLED-Sleeper/Features/MonitorInformation/Services/MonitorInfoProvider.cs` |
| `OLED-Sleeper/Features/MonitorState/Handlers/SynchronizeMonitorStateCommandHandler.cs` *(also edited in the session above)* |
| `OLED-Sleeper/Features/MonitorState/Services/MonitorStateWatcher.cs` *(also edited in the session above)* |
| `OLED-Sleeper/Native/NativeMethods.cs` |
| `OLED-Sleeper/UI/Services/Interfaces/IWorkspaceService.cs` |
| `OLED-Sleeper/UI/Services/WorkspaceService.cs` |
| `OLED-Sleeper/UI/ViewModels/MonitorConfigurationViewModel.cs` |
| `OLED-Sleeper/UI/ViewModels/MonitorLayoutViewModel.cs` |
| `OLED-Sleeper/UI/Views/MonitorConfigurationView.xaml` |

### Files touched specifically for topology debounce + idle sync (this session)

- `OLED-Sleeper/Features/MonitorIdleDetection/Services/Interfaces/IMonitorIdleDetectionService.cs`
- `OLED-Sleeper/Features/MonitorIdleDetection/Services/MonitorIdleDetectionService.cs`
- `OLED-Sleeper/Features/MonitorInformation/Services/Interfaces/IMonitorInfoManager.cs`
- `OLED-Sleeper/Features/MonitorInformation/Services/MonitorInfoManager.cs`
- `OLED-Sleeper/Features/MonitorState/Services/MonitorStateWatcher.cs`
- `OLED-Sleeper/Features/MonitorState/Handlers/SynchronizeMonitorStateCommandHandler.cs`
- `CHANGES.md` (this file)

---

## Review checklist (human)

- [ ] Confirm debounce count (`RequiredStableTopologyPolls = 3`) is acceptable for your hardware (too high = slow reaction to real unplug; too low = still noisy on wake).
- [ ] Exercise: secondary monitor wake after OLED-Sleeper idle sleep, multi-monitor hotplug, and settings changes (still using `UpdateSettings` path).
- [ ] Confirm no duplicate idle loops or regressions around app shutdown (`Stop()` still cancels the loop once).
- [ ] Run full test suite and manual UI pass; re-run `git diff` and code review on every file in the working tree.
