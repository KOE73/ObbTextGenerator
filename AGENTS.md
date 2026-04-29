# Agent Guidance

This file describes how automated coding agents should work in this repository.
Keep changes narrow, explicit, and easy to review.

## Project Purpose

ObbTextGenerator is a modular C# toolkit for generating synthetic images with text-line annotations for OBB/quad detection pipelines.
The output is intended for text detection datasets and OCR-oriented workflows.

Typical generator flow:

1. Build a synthetic or image-based scene from configured backgrounds, patterns, colors, fonts, and text providers.
2. Render one or more text lines with controlled geometry and appearance.
3. Store abstract text geometry and annotation layers.
4. Write images, labels, debug previews, overlays, context dumps, and other artifacts from already prepared sample data.

## Core Principles

- Make incremental changes. Do not introduce broad infrastructure unless it is clearly needed for the requested task.
- Prefer explicit, typed settings, explicit factories, explicit registrations, and explicit data models.
- Avoid reflection, auto-discovery, DI containers, plugin magic, or abstract frameworks unless explicitly requested.
- Keep code readable for manual maintenance. Prefer clear intermediate variables over dense chained expressions.
- Keep function bodies step-by-step and easy to debug.
- Do not perform stylistic cleanup outside the requested scope.
- Do not reorder, remove, or rewrite unrelated YAML, comments, using directives, or config entries.
- If a fundamental interface or base architecture contract must change, call that out before making a large redesign.

## Architecture Rules

- The main namespace is currently `ObbTextGenerator`.
- `RenderContext` must contain only per-sample runtime state, not global generation settings.
- Global generation settings belong in configuration/runtime settings objects, not in per-sample state.
- Render stages must not parse YAML directly and must not depend on YAML format details.
- YAML is loaded into typed settings first; factories and runtime objects consume typed settings.
- `text-line` must not be tied to a single concrete OBB format. It should produce abstract text-line data that can later be exported in multiple annotation forms.
- Annotation layers must support multiple parallel layers so different box strategies can be compared.
- Writers/exporters must use the current sample state and existing annotation layers. They must not recompute text geometry from the final bitmap.
- Consult `src/ObbTextGenerator/Doc/ExportersPlan.md` before adding exporters or exporter-related structures.
- If planned architecture is not implemented yet, leave a simple extension point instead of inventing a temporary parallel architecture.

## Repository Layout

Current code is organized under `src/ObbTextGenerator/` by subsystem:

- `Configuration/` - YAML loading, root config, global typed settings.
- `Pipeline/` - pipeline contracts, registry, factory context, runner, stage module loading.
- `Rendering/` - sample runtime state, geometry, annotations, render session models.
- `Colors/` - color providers, color schemes, color factories.
- `Text/` - text providers, font providers, font groups, font preview, text/font factories.
- `Stages/` - concrete pipeline stages.
- `Resources/` - built-in YAML/assets used by runtime or tooling.
- `Doc/` - architecture notes and plans.
- `demo/` - public demo configuration, local demo resources, and demo run scripts.
- `tools/` - auxiliary tools such as the background downloader.

Stage grouping under `Stages/`:

- `Stages/Scene/` - backgrounds, patterns, textures, light, noise, camera/effects, color selection.
- `Stages/Text/` - text generation and rendering.
- `Stages/Visualization/` - visual inspection stages such as annotation overlays.
- `Stages/Writing/` - writing/export stages.

## Pipeline Rules

- The pipeline is linear.
- A pipeline may contain both rendering stages and writing stages.
- Rendering stages mutate the current sample state.
- Writing stages save artifacts from the current sample state at the point where they run.
- Stage order is meaningful. Do not move stages unless that is part of the task.
- Do not introduce a separate checkpoint/exporter binding model unless explicitly requested.
- When adding a new built-in stage, register it in `BuiltInPipelineStages`.
- Optional plugin stages must remain explicit through module registration.

## Configuration Rules

- YAML is the configuration format.
- Keep YAML easy to read and edit manually.
- Prefer compact single-line mappings with `{ ... }` when readability is preserved.
- Use anchors and aliases only when they clearly reduce duplication and improve clarity.
- Do not change `sampleCount`, output paths, stage ordering, or commented-out writer blocks unless the task requires it.
- Public runnable examples belong in `demo/`.
- Internal/reference configs under `src/ObbTextGenerator/` are not the public entry point.
- `config_full.yaml` is the internal reference file for supported options. When adding a new module or config property, update it with a clear example and comments.
- `general.resourceRoot` controls shared YAML resources such as `FontGroups`. Relative paths are resolved from the config file directory.
- `FontGroups` are loaded from `resourceRoot/FontGroups`, with built-in output resources used only as fallback when the configured resource directory is absent.

## Public Demo Rules

The public demo lives in `demo/`.

- Keep `demo/config_text.yaml` domain-neutral and publication-safe.
- Do not use private product codes, old domain terms, or local absolute paths in the demo.
- Demo resources may live next to the demo config under `demo/Resources`.
- The demo should be runnable through scripts in `demo/`, without requiring users to edit source-tree configs.
- Keep demo outputs under repository-root `LocalArtifacts/`.
- Do not re-enable or edit commented-out demo output stages unless the task asks for it.
- Keep demo comments in simple English.
- Keep `demo/README_DemoRun.md` aligned with demo scripts and output folders.

## Local Artifacts

All temporary outputs, diagnostics, previews, manual runs, and downloaded assets must go under repository-root `LocalArtifacts/`.

Recommended structure:

- `LocalArtifacts/GeneratedSamples/` - manual generator runs and temporary datasets.
- `LocalArtifacts/Backgrounds/` - locally downloaded/prepared backgrounds for `image-folder`.
- `LocalArtifacts/TextImage/` - locally downloaded/prepared full-frame text-image inputs for demo variants.
- `LocalArtifacts/FontGroupPreview/` - font group preview images.
- `LocalArtifacts/Diagnostics/` - temporary configs, traces, dumps, smoke runs.
- `LocalArtifacts/Benchmarks/` - benchmark outputs.
- `LocalArtifacts/Scratch/` - one-off experiments.

Rules:

- Do not place temporary files in the repository root or next to source files.
- `LocalArtifacts/` is not source code and must not be required for a fresh clone to build.
- If local artifacts are already tracked by git, call that out before publication cleanup.
- New launch/debug profiles should write temporary output to repository-root `LocalArtifacts/`.

## Publication Hygiene

Before preparing public-facing changes, scan for:

- private or domain-specific names, old product codes, and narrow legacy dataset names;
- local absolute paths, local workspace paths, wallpaper folders, CUDA/cuDNN/TensorRT paths;
- generated data, preview images, smoke outputs, downloaded assets, model files;
- real secrets, API keys, tokens, passwords, bearer tokens, or private URLs;
- internal-only notes in docs or configs.

Notes:

- Mentions of `--api-key` and environment variable names such as `WALLHAVEN_API_KEY` are acceptable when they are documentation or code paths and do not contain actual values.
- External model URLs are not secrets, but they are concrete dependencies and should be intentional.
- `LICENSE` and `NOTICE` author/brand names should be reviewed before publication.

## Code Style

- Use modern C#/.NET features when they improve clarity, safety, or performance.
- Use one class per file; file name should match the class name.
- An exception is acceptable for tightly related records.
- Do not split or merge classes without a clear reason or explicit request.
- Do not add, remove, or reorder `using` directives unless needed for changed code to compile.
- Prefer meaningful variable names. Do not use unclear abbreviations.
- Prefer auto-properties where they do not harm performance, ownership clarity, or model clarity.
- Use `Span<>`, `ReadOnlySpan<>`, `Memory<>`, and related low-allocation patterns only when justified by hot-path performance and readability.
- Do not remove `readonly` from structs or struct fields unless they genuinely need mutation.
- Preserve meaningful comments. Do not shorten comments that explain non-obvious behavior.
- For large files with roughly 10 or more methods, use `#region` only when it improves navigation and matches local style.

## Validation

- After code changes, build the whole solution:

```powershell
dotnet build ObbTextGenerator.slnx
```

- For refactors, validate the whole solution, including demo and tools when relevant.
- For YAML/demo changes, run a small smoke configuration from `LocalArtifacts/Diagnostics` rather than producing a large dataset.
- Keep smoke-run artifacts under `LocalArtifacts/Diagnostics` or another `LocalArtifacts/` subfolder.

## Git Safety

- The working tree may already contain user changes.
- Never revert changes you did not make unless explicitly asked.
- If unrelated files are dirty, ignore them.
- If dirty changes affect your task, work with them and preserve intent.
- Avoid destructive commands such as `git reset --hard` or `git checkout --` unless explicitly requested.
