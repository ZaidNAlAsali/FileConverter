# Fresh Hermes audit brief

Copy the prompt below into a new Hermes chat opened in the ZFileConverter Project. The root `AGENTS.md` is the authoritative project map and engineering contract; the new chat should read it before acting.

```text
Perform a bounded final product audit of the ZFileConverter repository and fix only confirmed material defects.

Workspace:
Open the ZFileConverter repository Project and work from its repository root.

Start by reading AGENTS.md, then inspect the current git status and diff. Do not assume the worktree is clean or that a previous chat completed publication. Preserve any pre-existing user changes.

Product goal:
ZFileConverter is an Explorer-first, local Windows converter that must stay smooth, dependable, lightweight, and understandable on ordinary or older laptops. It must not become an always-running service, tray utility, cloud app, Electron shell, or oversized preset catalog.

Audit scope:

1. Correctness and data safety
- Trace every changed symbol to its callers.
- Check settings Save, unsaved Close/revert, reopening Settings, preset migration, output validation, cancellation, error paths, and destructive post-conversion actions.
- Back up user settings before runtime or installer tests.
- Use only disposable conversion inputs.

2. Startup and window lifecycle
- Normal no-argument launch must show onboarding and then Settings.
- --settings must open Settings directly.
- Save must persist without closing Settings.
- Close after Save must exit normally.
- Unsaved Close must revert to the last successful Save.
- Reopening Settings within the same live process must bind fresh service state, with no stale singleton ViewModel.
- Ensure session shutdown cannot accidentally open Settings from onboarding.

3. Explorer extension
- Inspect only native-shell-critical code.
- Confirm menu construction stays fast and defensive inside Explorer.
- Confirm a saved preset rename/addition appears in a fresh context menu without restarting Explorer.
- Keep the previous good preset menu if the settings file is briefly unavailable during replacement.
- Verify mixed selections enable only presets compatible with every selected extension.
- Exercise the actual installed classic context menu, not only resource decoding.

4. Performance and footprint
- Measure at least three comparable Release runs before and after any optimization.
- Check UI-thread disk/network work, idle loops, timers, process spawning, conversion concurrency, diagnostic cleanup, and unnecessary dependencies.
- Judge the public MSI, not the raw build folder, because the raw ImageMagick package may contain unused architectures.
- Retain an optimization only if it is simple, behaviorally safe, and materially faster. If the legacy WPF Settings cold start remains around the documented baseline without a safe fix, accept it and stop.
- Do not replace WPF, SharpShell, .NET Framework, or the conversion engines in this audit.

5. Security and privacy
- Confirm file contents remain local and no telemetry/accounts/uploads were introduced.
- Confirm XML readers prohibit DTD resolution.
- Confirm shell argument quoting handles spaces, quotes, backslashes, and long selections.
- Confirm updater MSI downloads use HTTPS and pass the adjacent .sha256 check before execution.
- Test checksum success and mismatch/failure behavior without launching a real update.
- Do not claim code signing unless the artifact is actually Authenticode-signed.

6. Dependencies and package quality
- Run the current NuGet vulnerable-package audit.
- Remove only dependencies proven unused.
- Do not update packages merely because a newer patch exists.
- Preserve FFmpeg, ImageMagick, Ghostscript, NetOffice, LibreOffice integration, and CDA support while their features remain supported.

7. UX, accessibility, and documentation
- Inspect all touched windows at normal and minimum supported sizes in Dark and Light when relevant.
- Check keyboard default/cancel behavior, focus, clipping, contrast, and AutomationProperties on icon-only controls.
- Search public surfaces for obsolete repository URLs and old visible product branding.
- Validate docs/preset-reference.md against the implementation.
- Keep the visual system already defined in AGENTS.md. Do not redesign it.

8. Build and release evidence
- Run the full x64 Release solution rebuild using the command in AGENTS.md.
- Confirm the EXE, extension DLL, and MSI exist.
- Exercise representative FFmpeg and ImageMagick conversions.
- Verify the real MSI UI and controlled installed upgrade.
- Confirm user settings are byte-for-byte preserved unless a test intentionally changes and restores them.
- Run git diff --check, inspect the complete diff, scan changed text for secrets/private paths, and verify repository URLs.
- If publication is in scope, use a new patch version, push integration, wait for GitHub Actions, publish the CI-produced MSI plus .sha256, re-download it, and verify the public hash.

Hard boundaries:
- Do not add a broad smoke-test suite.
- Do not add more default presets.
- Do not delete or reset customized user presets.
- Do not redesign approved UI.
- Do not add background processes, telemetry, accounts, or cloud conversion.
- Do not perform broad modernization or style-only refactors.
- Do not commit or publish until local verification passes.
- Stop after confirmed material defects are fixed.

Required final answer:
- Release-blocking findings and exact fixes
- Runtime and native Windows verification evidence
- Performance measurements and whether they changed
- Dependency and vulnerability result
- Exact build/installer/release artifacts and SHA-256 if published
- A firm split between required remaining work and optional future ideas
- Explicitly state when the cycle is complete instead of proposing another polish pass
```
