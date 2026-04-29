# WriteContextDumpStage Requirements

## Purpose

This file keeps the stable intent, formatting rules, and future extension notes for `WriteContextDumpStage`.
It should stay next to the stage implementation so the formatting contract is easy to find and evolve.

## Output Contract

- Output is a plain text file written by pipeline stage `write-context-dump`.
- The dump must stay human-readable first.
- The dump should always keep the same high-level section order.
- The dump should combine:
  - summary tables for fast scanning;
  - tree sections for full inspection of nested runtime state.
- Tables must be rendered through `Spectre.Console.Table`.
- Tree sections must be rendered through `Spectre.Console.Tree`.
- File output must be produced through a Spectre console configured without ANSI and without colors.
- Table and tree line styles must be configurable from stage settings.

## Required Top-Level Sections

1. Header
2. Context Summary
3. TextLines table
4. Annotation Layers table
5. Sample Data table
6. Trace Entries table
7. Full tree sections

## Tree Formatting Rules

- Use a stable ASCII tree style through Spectre tree guides.
- Support both plain ASCII and pseudographic line styles.
- Keep member names explicit.
- Prefer deterministic ordering where possible.
- When a value cannot be expanded safely, show a short summary instead of failing.
- Cycles must be marked explicitly.
- Truncation must be marked explicitly.

## Full-Content Scope

- Primary target is full readable state of `RenderContext`.
- Important semantic runtime data must be shown in detail:
  - settings;
  - text line descriptors;
  - annotation layers;
  - sample data bag;
  - trace entries.
- Opaque graphics/runtime objects may be summarized instead of recursively exploding internal engine state.

## Change Notes

- Keep this section for future formatting decisions.
- Add new requirements here before changing dump layout.
- If context structure changes, update this file together with formatter behavior.

## Pending Ideas

- Optional separate section for current render window stack.
- Optional machine-readable sibling file if debugging flow grows.
- Optional include/exclude filters per section.
