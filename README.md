# OBB Text Dataset Generator

**ObbTextGenerator** is a .NET tool for generating synthetic images with text-line annotations for OCR detection experiments.

The current public entry point is the `demo/` folder. It contains the runnable demo config, local demo resources, and helper scripts.

## What It Produces

- generated images;
- YOLO OBB labels;
- YOLO axis-aligned box labels;
- optional PaddleDet-derived labels;
- debug previews;
- context dumps for inspection.

## Requirements

- .NET SDK with the target framework used by the solution;
- Windows for the current bundled OpenCV runtime package;
- optional ONNX Runtime GPU dependencies if you use GPU PaddleDet backends.

## Build

```powershell
dotnet build .\ObbTextGenerator.slnx -c Release
```

## Run The Demo

Open `demo/` and follow:

- `demo/README_DemoRun.md`

The short version:

```cmd
cd demo
LoadImageBg.cmd
LoadImageText.cmd
GenWithText.cmd
```

`LoadImageBg.cmd` and `LoadImageText.cmd` prepare optional local image inputs under `LocalArtifacts/`.
`GenWithText.cmd` runs the generator using `demo/config_text.yaml`.

Generated data is written to:

```text
LocalArtifacts/GenData
```

Typical output folders:

- `train/images`, `val/images`;
- `labels_obb_font`;
- `labels_box`;
- `labels_obb_paddledet`;
- `debug_preview`;
- `context_dump`.

## Configuration

Public demo configuration is in:

```text
demo/config_text.yaml
```

Demo-local YAML resources, such as font groups, are in:

```text
demo/Resources
```

Use the files in `demo/` when evaluating the project.

## Repository Structure

- `demo/` - public demo config, demo resources, and run scripts.
- `src/ObbTextGenerator/` - main generator executable and core pipeline.
- `src/ObbTextGenerator.PaddleDet/` - optional PaddleDet stage module.
- `tools/ObbTextGenerator.Tools.BackgroundDownloader/` - helper tool for downloading local image inputs.
- `LocalArtifacts/` - generated samples, downloaded images, diagnostics, and other local outputs.
- `models/` - local model files.

`LocalArtifacts/` and `models/` are local working folders and should not be treated as source assets.

## Notes

The project is published as an applied research/dataset-generation tool. The public API is not frozen, and some internal architecture and exporter plans are still evolving.

## License

MIT. See `LICENSE`.
