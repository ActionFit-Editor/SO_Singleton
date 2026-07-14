---
name: so-singleton-help
description: Explain SO Singleton, its installed skill, Resources loading and caching contract, global API compatibility, asset placement, menu, and safety boundaries. Use when a user asks how singleton assets are placed, loaded, or consumed.
---

# SO Singleton Help

Answer in the user's language. This is help-only: explain the package without creating, loading through Unity, moving, renaming, or changing ScriptableObject assets.

1. Read `PACKAGE_SKILLS.md` first. Treat its generated package identity, related-skill table, `$skill-name` invocation, description, and read-only boundary as authoritative.
2. Read `Packages/com.actionfit.sosingleton/README.md` and `Packages/com.actionfit.sosingleton/AI_GUIDE.md` when present. If downloaded, resolve `Library/PackageCache/com.actionfit.sosingleton@*` without editing it.
3. Explain that global `SO_Singleton<T>.Instance` calls `Resources.Load<T>("SO/{typeof(T).Name}")`, caches a non-null result, returns `null` when no matching asset exists, and does not create or save an asset.
4. Explain that Unity virtually combines all `Resources` folders, so the asset must be reachable at `Resources/SO/{type-name}.asset`; the package does not own a project-specific physical folder, uniqueness policy, lifecycle reset, or migration.
5. Identify `Tools > Package > SO Singleton > README` as the only package menu entry and state that the package has no settings asset, audit skill, or asset-generation command.
6. Never scan by loading assets in Unity, assign singleton instances, create or delete assets, rename types or files, move Resources folders, change serialized values, add a settings menu, run Unity, publish, tag, or update the package catalog from this skill.
