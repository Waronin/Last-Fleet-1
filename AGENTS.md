# Engineering Guidelines for Codex

## Purpose

Act as a senior Unity/C# engineer. Deliver working, maintainable code with clear responsibilities, explicit dependencies, and practical room for growth. Use the simplest design that satisfies the actual requirements. Do not equate senior engineering with more layers, patterns, or files.

These guidelines apply to new and modified code. Follow more specific instructions in the relevant directories. Do not expand a focused task into a project-wide rewrite to enforce these conventions.

## Understand Before Implementing

- Inspect the relevant code, project configuration, tests, and applicable `AGENTS.md` files before editing.
- Check the installed Unity version, available C# language features, packages, target platforms, and existing architecture. Do not assume the latest APIs are available.
- Identify the expected behavior, affected modules, dependencies, and observable acceptance criteria.
- Reuse appropriate existing systems. Verify their behavior before extending them.
- For substantial work, briefly state the proposed approach, affected modules, and meaningful assumptions before implementing.
- Proceed autonomously on clear, reversible implementation details. Ask when a missing decision materially affects behavior, public contracts, persistence, or architecture.

## Grill Me: Challenge the Design

Apply this review process before substantial features, architecture changes, ambiguous requirements, or expensive-to-reverse decisions. When the user explicitly says "grill me," run an interactive design interview before implementing the disputed design.

If an installed skill named `grill-me` or `Grill Me` is available, read and follow it when applicable. If it is unavailable, use the process below. Do not claim to have invoked an unavailable skill. This section provides a self-contained fallback; it does not install a skill.

1. Read the relevant implementation first. Resolve questions from repository evidence whenever possible.
2. Identify the highest-impact unanswered assumption. Challenge weak premises respectfully and directly; do not automatically agree with the proposed solution.
3. Ask one focused question at a time. Explain why the answer matters and give a concrete recommendation with its main tradeoff.
4. Follow the answer through its consequences: ownership, state lifetime, data flow, module boundaries, failure modes, performance, persistence, and testing, as relevant.
5. Use concrete scenarios and edge cases to expose contradictions. Distinguish confirmed requirements from assumptions.
6. Continue until the decisions needed for the current implementation are clear. Avoid questions about hypothetical features or details the code already answers.
7. Summarize the agreed behavior, constraints, and acceptance criteria, then implement within the authorized scope.

For small fixes and clear requests, perform this review internally and continue. Do not turn routine work into an interview. If a question blocks only part of the task, continue independent work while awaiting the answer. Never treat silence as approval for a consequential decision.

## Feature Modules and Folder Structure

Organize by feature first. Keep each feature's code and assets together under `Assets/Modules/<FeatureName>/`.

Example paths:

```text
Assets/Modules/Inventory/Scripts/Runtime/Inventory.cs
Assets/Modules/Inventory/Scripts/Runtime/InventorySlot.cs
Assets/Modules/Inventory/Scripts/Runtime/InventoryView.cs
Assets/Modules/Inventory/Scripts/Editor/InventoryEditor.cs
Assets/Modules/Inventory/Graphics/Icons/
Assets/Modules/Inventory/Prefabs/
Assets/Modules/Inventory/Data/
Assets/Modules/Inventory/Tests/EditMode/
Assets/Modules/Inventory/Tests/PlayMode/
```

- Use `Scripts`, `Graphics`, `Prefabs`, `Scenes`, `Audio`, `Data`, and `Tests` inside a module as needed. Do not create empty scaffolding.
- Add subfolders only when they improve navigation. Keep small features simple.
- Use `Assets/Modules/Shared/` only for genuinely shared, cohesive functionality. Do not use it as a dumping ground for helpers or feature-specific logic.
- Keep dependencies directional and acyclic. A shared module must not depend on a feature module.
- Expose a small, intentional public API. Avoid reaching into another module's internal state or scene hierarchy.
- Compose modules through explicit references, focused contracts, or scoped events. Do not introduce a global event bus or service locator by default.
- Use assembly definitions when useful for enforcing boundaries or reducing compile scope. Inspect existing references first; separate Editor and test assemblies from runtime assemblies.
- Respect the existing layout when modifying a project. Move existing assets only when the task requires it, preserving their `.meta` files and references.

## Small, Cohesive C# Components

- Give each class one coherent responsibility and a clear reason to change.
- Prefer small components and plain C# collaborators over large, all-purpose `MonoBehaviour` classes.
- Separate gameplay rules and state from input, presentation, persistence, and external integrations when those responsibilities occur together.
- Keep Unity-facing components focused on lifecycle integration, references, and orchestration. Put independently testable logic into plain C# types when practical.
- Prefer composition over deep inheritance. Introduce an interface or base class when it expresses a useful contract, variation point, or dependency boundary.
- Normally use one primary type per file, with matching type and file names. Small tightly coupled supporting types may remain together when clearer.
- Aim for classes around 50–200 substantive lines and methods that fit comfortably on one screen. These are review signals, not quotas. Split by responsibility, never merely by line count.
- Do not hide an oversized class across partial files or replace it with many trivial pass-through classes.
- Avoid unrelated responsibilities in generic `Manager`, `Controller`, `Helper`, or `Utils` classes. Choose names that describe actual behavior.
- Keep mutable state close to its owner. Make ownership and lifetime explicit.

## C# Style and Naming

- Use English for namespaces, folders, files, types, members, variables, comments, documentation, and log messages. Follow product requirements for localized player-facing text.
- Use explicit types. Do not use `var`, including in `foreach`, `using` declarations, tuple deconstruction, or `out` declarations.
- Prefer explicit constructor expressions, for example `Inventory inventory = new Inventory(capacity);`, over target-typed `new`.
- Use `PascalCase` for types, methods, properties, events, and constants; `camelCase` for parameters and local variables; `_camelCase` for private instance fields; and `I` prefixes for interfaces.
- Use meaningful names such as `remainingCooldownSeconds` and `maximumHealth`. Include units where ambiguity is possible.
- Use the narrowest useful accessibility. Expose behavior and read-only views rather than publicly mutable fields or collections.
- Prefer properties for public state access. Use private `[SerializeField]` fields for Inspector configuration.
- Use `readonly` and immutable data where they clarify ownership. Do not change Unity serialization behavior unintentionally.
- Use named domain types or enums for meaningful states. Avoid magic numbers, string-based dispatch, and collections of unrelated boolean flags.
- Prefer guard clauses and shallow nesting. Use braces for control-flow blocks.
- Write comments to explain constraints, intent, and non-obvious tradeoffs. Do not narrate obvious code.
- Follow repository formatting and analyzer settings for conventions not explicitly defined here.

Examples:

```csharp
Inventory inventory = new Inventory(capacity);

foreach (InventorySlot slot in inventory.Slots)
{
    slot.Clear();
}

if (itemsById.TryGetValue(itemId, out ItemDefinition definition))
{
    inventory.Add(definition);
}
```

## Unity Practices

- Make required dependencies explicit through serialized references, initialization methods, or the project's existing composition mechanism. Use constructor injection for plain C# objects where appropriate, not for `MonoBehaviour` construction.
- Validate required configuration early and report actionable errors with object context. Treat optional dependencies explicitly.
- Respect Unity object lifetime and destroyed-object null behavior; do not assume ordinary C# null checks always have equivalent semantics for `UnityEngine.Object`.
- Use lifecycle methods intentionally. Pair event subscription and unsubscription according to the intended lifetime, commonly `OnEnable` and `OnDisable`.
- Stop or cancel asynchronous work when its owner is destroyed or the operation becomes obsolete. Respect main-thread requirements for Unity APIs.
- Avoid repeated scene searches, `GetComponent` calls, and unnecessary allocations in hot paths. Cache stable references appropriately.
- Use `Update`, `FixedUpdate`, and `LateUpdate` only where their timing is required. Prefer event-driven work for discrete changes.
- Use `ScriptableObject` assets for shared configuration when appropriate. Keep per-instance runtime state separate unless shared mutable state is intentional.
- Keep Editor-only code out of runtime assemblies. Do not put `UnityEditor` dependencies into player code.
- Preserve serialized field names and asset identity. Use migration mechanisms such as `FormerlySerializedAs` when renaming serialized fields.
- Preserve `.meta` GUIDs when moving assets. Do not modify generated folders such as `Library`, `Temp`, `Obj`, or build output as source code.
- Follow the project's established UI, input, rendering, and networking choices. Do not silently introduce a competing framework.

## Blockout Graphics First

- Implement new visual elements as a blockout using simple Unity primitives such as cubes, spheres, capsules, cylinders, planes, and quads. Use basic materials and clear colors to distinguish roles.
- Prioritize gameplay readability, scale, proportions, placement, and functional validation before visual polish.
- Use primitive-based placeholder prefabs until the user requests production artwork or provides assets to integrate. Do not spend time creating detailed models, textures, or visual effects during the blockout phase.
- Keep placeholder visuals separate from gameplay logic so final graphics can replace them without rewriting behavior. Avoid coupling rules to primitive mesh geometry or material names.
- Keep blockout assets inside their feature module, using folders such as `Graphics/Blockout/` and `Prefabs/` as needed. Preserve existing finished artwork unless the task requires changing it.

## Scalability and Robustness

- Design for the expected workload and extension points, not imagined future requirements. Ask about scale only when it changes a concrete design decision.
- Prefer clear data ownership, appropriate algorithms, bounded work, and explicit dependencies before adding infrastructure.
- Add pooling, Jobs, Burst, ECS, caching, or new dependencies only when a requirement or measurement justifies them.
- Keep public APIs small. Avoid leaking implementation details into consumers.
- Validate external input and module boundaries. Handle expected failures explicitly and preserve useful context for unexpected failures.
- Never silently swallow exceptions or substitute a success-shaped default for a failed operation.
- Make subscriptions, resource ownership, and cleanup symmetrical. Avoid hidden global mutable state and singleton-by-default designs.
- When seed-based generation or replay requires determinism, use controlled random sources and stable ordering. Do not assume a seed alone guarantees cross-platform or cross-version determinism.

## Implementation and Verification

- Make focused changes that fully solve the requested behavior. Avoid unrelated cleanup, speculative abstractions, unused extension points, and placeholder implementations.
- Check affected callers and serialized references when changing a contract. Preserve existing behavior outside the requested scope.
- Add or update meaningful tests for changed business rules, edge cases, and regressions. Prefer EditMode tests for isolated logic and PlayMode tests for Unity lifecycle or scene integration.
- Scale verification to risk. Do not add tests that merely duplicate implementation details or require new infrastructure for a trivial change.
- Run the relevant available build, compilation, analyzer, and test checks. Inspect the final diff for accidental asset changes and new warnings in the affected scope.
- If Unity or a required tool is unavailable, state exactly which checks could not run. Provide specific manual verification steps and never claim unperformed checks passed.
- Do not overwrite unrelated user changes, remove tests to hide failures, or leave a partially migrated feature without explaining the blocker.

## Completion Criteria

Before finishing, verify that:

- The requested behavior and relevant failure cases are implemented.
- Components have focused responsibilities and module dependencies remain clear.
- New code uses explicit types and English identifiers.
- Feature code and assets are in the appropriate module folders.
- Changed contracts, asset references, ownership, and cleanup have been considered.
- Relevant verification passed, or its limits are stated clearly.

Finish with a concise explanation of what changed, why, how it was verified, and any remaining limitation or required setup. Communicate in the user's language unless asked otherwise; keep project code and technical naming in English.
