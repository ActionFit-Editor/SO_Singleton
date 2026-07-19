# AI Guide - SO Singleton

This file is shipped inside the UPM package so an AI assistant in a consuming Unity project can understand the package without access to the source project's `Docs/AI` folder.

## Package Identity

- Package ID: `com.actionfit.sosingleton`
- Display name: SO Singleton
- Repository: `https://github.com/ActionFit-Editor/SO_Singleton.git`
- Current package version at generation time: `1.0.6`
- Unity version: `6000.2`

## Purpose

SO Singleton provides ScriptableObject singleton loading conventions. Use `README.md`, `package.json`, package source files, and `Editor/PackageInfo/ActionFitPackageInfo_SO.asset` together to understand the user-facing workflow and catalog metadata.

## Agent Skills

- `Skills~/manifest.json` registers schema v2 `so-singleton-help` for Codex and Claude with read-only access.
- Help reads generated `PACKAGE_SKILLS.md` before explaining the `Resources/SO/{type-name}` lookup, present-or-missing result cache, global API, asset placement, menu, and safety boundaries.
- This initial registration is help-only. It does not load or scan assets through Unity, create or change ScriptableObjects, move Resources folders, run validation, publish, tag, or update the catalog.

## Project Router Registration

This package should be listed in `Packages/com.actionfit.custompackagemanager/PACKAGE_AI_GUIDE_ROUTER.md`.

Requested router entry:

- `Packages/com.actionfit.sosingleton/AI_GUIDE.md` - SO Singleton provides ScriptableObject singleton loading conventions. Read when changing singleton load paths, Resources/SO behavior, or singleton base APIs.

If the router file is not already included in the AI assistant's default reading sequence, the router file is responsible for asking the user to link it from `Docs/AI/PROJECT.md` when available, or otherwise from `AGENTS.md`, `CLAUDE.md`, or another primary AI markdown entry point.

Read this file when:

- changing files under `Packages/com.actionfit.sosingleton/`
- diagnosing `SO Singleton` behavior in a consuming project
- preparing a release for `com.actionfit.sosingleton`
- editing package metadata, README, AI guide, package version, or release notes

## Required Reading For AI

- Read this `AI_GUIDE.md` before changing, diagnosing, or explaining this package.
- Read `Packages/com.actionfit.custompackagemanager/PACKAGE_AI_GUIDE_ROUTER.md` when deciding which installed ActionFit package `AI_GUIDE.md` applies to a task.
- Read `README.md` for human-facing setup and usage.
- Read `package.json` for package ID, version, Unity version, and dependencies.
- Read `Editor/PackageInfo/ActionFitPackageInfo_SO.asset` for catalog metadata, repository name, owner, status, description, release note, and dependency override.

## Editing Rules

- Keep changes scoped to this package unless the user explicitly asks for cross-package edits.
- Do not change package IDs, repository names, public menu paths, serialized field names, or package assembly names casually; these can affect installed projects.
- Preserve Unity `.meta` files when adding, moving, or renaming files inside the package.
- When behavior changes, update this `AI_GUIDE.md` in the same package before publishing so consuming projects receive the latest AI context.
- Keep `README.md` focused on human usage. Keep this file focused on AI-facing architecture, constraints, migration notes, and package-specific editing rules.

## Settings Asset Lifecycle

- Register an opt-in settings type with `ActionFitSettingsAssetAttribute`; do not infer settings ownership from every `ScriptableObject`.
- `EditorOnly` assets default to `Assets/_Data/_<Owner>/<Type>.asset`.
- `RuntimeSingleton` assets must inherit `SO_Singleton<Self>` and default to `Assets/_Data/_<Owner>/Resources/SO/<Type>.asset`.
- Resolution order is canonical path, declared legacy paths, one unique project type match, then guarded creation. Duplicate, invalid, or occupied results block creation and are reported by `Tools/Package/SO Singleton/Audit Settings SO`.
- Registered type discovery, project main-asset type paths, and per-type resolution are cached. Startup bootstrap and audit inspect paths/types without loading existing assets; a consumer resolution loads only its selected asset. Clear the editor cache with `Tools/Package/SO Singleton/Clear Settings Cache`; project changes and assembly/play transitions also invalidate it.
- `SO_Singleton<T>.Instance` caches present and missing Resources results until `SubsystemRegistration`, so disabled Domain Reload does not retain a stale session result.
- Implement `IActionFitSettingsAssetInitializer` only for defaults that are safe before first asset persistence. Never overwrite an existing asset's serialized values.

## Package Tools Menu

- Unity menu root: `Tools/Package/SO Singleton/`.
- Keep package commands under this package root.
- Lower separated entries:
- `README`: opens this package README.
- `Audit Settings SO`: checks all registered settings types without creating assets.
- `Clear Settings Cache`: invalidates the editor resolution cache.
- Do not add README or Setting SO access back to Custom Package Manager package rows or Project Files.

## Release Note Rules

- `ActionFitPackageInfo_SO.ReleaseNote` must contain only the single version being prepared.
- Do not copy older changelog entries into the newest release note.
- Version history and update-range summaries are composed by Custom Package Manager from separate catalog version rows.
- Do not add headings such as `## 1.0.0` inside ReleaseNote unless a specific package UI requires it; the catalog row already carries the version.
## Publish Notes

- Publishing is manual through Custom Package Manager.
- Before reusing a version, check the remote Git tags. Published tags are immutable.
- If this package is modified after a version was tagged, bump to the next unused patch version before publishing.
- The package repository should include this `AI_GUIDE.md` so other projects can load the AI package context after installing the package.
