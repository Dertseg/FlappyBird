# Version 2.2 validation

- MVVM, SQLite and Entity Framework Core retained; no screen layout or physics changes.
- Optional audio/haptics exceptions isolated from result persistence.
- Atomic per-session recovery files are replayed into SQLite through EF Core on initialization. Result and achievements share a transaction; GUID prevents duplicates. Explicit discard removes the recovery file. A full/unwritable disk cannot guarantee recovery.
- Dirty settings survive unrelated result/skin changes; explicit reload replaces the draft.
- Achievement aggregation skipped once all catalog achievements are unlocked.
- GitHub Actions runs checks and builds Release APK. scripts/android-smoke.ps1 exercises a connected Android device (creates one zero-score attempt).

Validation on 2026-11-12:
- 18,971 assertions passed, including a real SQLite trigger-induced failure, recovery, feedback exceptions and settings draft preservation.
- Release arm64 APK built; versionName 2.2.0, versionCode 4.
- Installed over 2.1.0 on connected Android phone without uninstalling. Existing best score 104 and 32 attempts preserved before smoke testing.
- APK uses the existing development signing key, compatible with the installed build.
- NuGet vulnerability audit unavailable due to network access; successful build used cached packages.

APK SHA256: DA8929047BEC0D236A60BCB59149C7731BD79724F1607307CE14858570A0BAB5
