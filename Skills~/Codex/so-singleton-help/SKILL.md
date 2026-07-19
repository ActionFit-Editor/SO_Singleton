---
name: so-singleton-help
description: Explain SO Singleton, its settings asset lifecycle, Resources loading and caching contract, global API compatibility, asset placement, menus, and safety boundaries. Use when a user asks how singleton or package settings assets are placed, found, created, or consumed.
---

# SO Singleton Help

Answer in the user's language. This is help-only: explain the package without creating, loading through Unity, moving, renaming, or changing ScriptableObject assets.

1. Read `PACKAGE_SKILLS.md` first. Treat its generated package identity, related-skill table, `$skill-name` invocation, description, and read-only boundary as authoritative.
2. Read `Packages/com.actionfit.sosingleton/README.md` and `Packages/com.actionfit.sosingleton/AI_GUIDE.md` when present. If downloaded, resolve `Library/PackageCache/com.actionfit.sosingleton@*` without editing it.
3. Explain that global `SO_Singleton<T>.Instance` calls `Resources.Load<T>("SO/{typeof(T).Name}")` once per subsystem lifetime, caches both a present and missing result, resets at `SubsystemRegistration`, returns `null` when no matching asset exists, and does not create or save an asset.
4. Explain that opt-in `ActionFitSettingsAssetAttribute` registrations use cached project type/path discovery, reuse canonical, declared legacy, or one unique typed asset, and create only a missing `AutoCreate` asset under `Assets/_Data/_<Owner>/`. Existing assets are not moved, renamed, or reserialized, while duplicates and occupied or invalid paths block creation.
5. Explain that `RuntimeSingleton` registrations must inherit `SO_Singleton<Self>` and use `Assets/_Data/_<Owner>/Resources/SO/<Type>.asset`; `EditorOnly` registrations use `Assets/_Data/_<Owner>/<Type>.asset` by default.
6. Identify `Tools > Package > SO Singleton > README`, `Audit Settings SO`, and `Clear Settings Cache`. The audit checks registered paths and types without creating or loading existing assets; project, assembly, and play-state changes invalidate the editor cache.
7. Never run the audit or bootstrap, scan by loading assets in Unity, assign singleton instances, create or delete assets, rename types or files, move Resources folders, change serialized values, run Unity, publish, tag, or update the package catalog from this help-only skill.
