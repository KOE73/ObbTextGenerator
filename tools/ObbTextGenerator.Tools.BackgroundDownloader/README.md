# ObbTextGenerator.Tools.BackgroundDownloader

CLI tool for downloading local background images for `image-folder` stages.

The tool currently supports Wallhaven through the official `api/v1/search` endpoint. It does not parse HTML pages and does not bundle downloaded images into the repository.

Downloaded files are local artifacts. By default they are written to:

```text
LocalArtifacts/Backgrounds/Wallhaven
```

## Parameters

- `--source` — image source API. Currently supported: `wallhaven`.
- `--output` — output directory. Defaults to `LocalArtifacts/Backgrounds/Wallhaven`.
- `--count` — maximum number of images to download.
- `--query` — Wallhaven search query.
- `--categories` — Wallhaven categories bitmask: general/anime/people. Default: `100`.
- `--purity` — Wallhaven purity bitmask: sfw/sketchy/nsfw. Default: `100`.
- `--sorting` — result sorting: `date_added`, `relevance`, `random`, `views`, `favorites`, `toplist`.
- `--order` — result order: `desc` or `asc`.
- `--top-range` — toplist range: `1d`, `3d`, `1w`, `1M`, `3M`, `6M`, `1y`.
- `--atleast` — minimum resolution, for example `1024x1024` or `1920x1080`.
- `--resolutions` — exact resolution filter, for example `1920x1080,2560x1440`.
- `--ratios` — aspect ratio filter, for example `16x9,16x10`.
- `--colors` — Wallhaven color filter hex without `#`.
- `--seed` — seed for repeatable random sorting pagination.
- `--api-key` — optional Wallhaven API key. If omitted, `WALLHAVEN_API_KEY` is used.
- `--start-page` — first Wallhaven search result page.
- `--max-pages` — maximum pages to scan. Use `0` for no explicit limit.
- `--api-delay-ms` — delay between Wallhaven API page requests.
- `--download-delay-ms` — delay between image downloads.
- `--timeout-seconds` — HTTP timeout in seconds.
- `--overwrite` — overwrite existing files with the same generated name.
- `--dry-run` — scan API pages and write manifest entries without downloading image files.
- `--file-name-pattern` — output file name pattern. Supported tokens: `{index}`, `{id}`, `{width}`, `{height}`.
- `--manifest` — manifest file name written inside the output directory.
- `--user-agent` — HTTP user agent.

## Query Syntax

`--query` is passed to Wallhaven as the `q` parameter. It supports normal words and Wallhaven search operators.

Common examples:

| Query | Meaning |
| --- | --- |
| `paper` | Search for images matching `paper`. |
| `"fabric texture"` | Search for both words as a text query. Quote in PowerShell because it contains a space. |
| `+paper -anime` | Prefer/require `paper`, exclude `anime`. |
| `paper type:jpg` | Search paper-like images and restrict to JPEG where Wallhaven supports the operator. |
| `id:12345` | Search by Wallhaven wallpaper id where supported by the API/search syntax. |

The exact supported operators are controlled by Wallhaven, not by this tool. If a query works on the Wallhaven website, it is usually a good candidate for `--query`.

## Categories Bitmask

`--categories` is a three-character Wallhaven bitmask:

```text
general anime people
   1      0      0
```

Each position is either `1` enabled or `0` disabled.

| Value | Enabled categories |
| --- | --- |
| `100` | General only. This is the default and the safest option for background textures. |
| `010` | Anime only. |
| `001` | People only. |
| `110` | General + anime. |
| `101` | General + people. |
| `011` | Anime + people. |
| `111` | All categories. |

For OCR backgrounds, `100` is usually the most useful default because it avoids many stylized or person-focused images.

## Purity Bitmask

`--purity` is another three-character Wallhaven bitmask:

```text
sfw sketchy nsfw
 1     0      0
```

| Value | Enabled purity levels |
| --- | --- |
| `100` | SFW only. This is the default. |
| `110` | SFW + sketchy. |
| `010` | Sketchy only. |
| `001` | NSFW only. Requires a Wallhaven API key and an account that can access NSFW content. |
| `111` | SFW + sketchy + NSFW. Requires API access for NSFW. |

For public datasets and GitHub-friendly examples, keep `--purity 100`.

## Sorting And Page Scan

`--sorting` controls how Wallhaven orders API results:

| Value | Typical use |
| --- | --- |
| `toplist` | Good default for visually strong images. Works with `--top-range`. |
| `relevance` | Useful when `--query` matters more than popularity. |
| `date_added` | Newest or oldest images depending on `--order`. |
| `random` | Randomized results. Use `--seed` for repeatability. |
| `views` | Popular by views. |
| `favorites` | Popular by favorites. |

`--max-pages` limits how many API pages are scanned. Use it for small checks:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query paper --count 10 --max-pages 1 --dry-run
```

If `--max-pages 0`, the tool keeps requesting pages until it downloads `--count` images or the API runs out of results.

## Output And Manifest

Each run writes a manifest next to downloaded images:

```text
LocalArtifacts/Backgrounds/Wallhaven/manifest.json
```

The manifest stores source URL, direct image URL, local path, image id, dimensions, file type, file size, colors, status, and errors if any.

File names are controlled by `--file-name-pattern`.

Supported tokens:

| Token | Meaning |
| --- | --- |
| `{index}` | Download sequence number, zero-padded. |
| `{id}` | Wallhaven image id. |
| `{width}` | Image width. |
| `{height}` | Image height. |

Example:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query paper --file-name-pattern "paper_{index}_{width}x{height}_{id}"
```

## Rate Limit

Wallhaven public API is rate-limited. The default `--api-delay-ms 1400` keeps the tool below the documented public limit of 45 API requests per minute.

Image downloads have a separate delay:

```powershell
--download-delay-ms 200
```

Increase delays if the API starts returning HTTP 429.

## Using Downloaded Backgrounds

Point an `image-folder` stage to the downloaded folder:

```yaml
- type: image-folder
  path: "../../LocalArtifacts/Backgrounds/Wallhaven/Fabric"
  recursive: true
  augment:
    fitMode: Cover
```

Paths are resolved relative to the active config file directory. If the config is `src/ObbTextGenerator/config.yaml`, then `../../LocalArtifacts/...` points to the repository-level `LocalArtifacts`.

## Licensing Note

Downloaded images are local user-provided dataset material. They are not part of this repository and are not covered by the repository source-code license.

Check the source website rules and the license/copyright status of images before using them in a dataset.

## Basic Usage

Download 100 SFW general backgrounds into the default local artifact directory:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --count 100
```

Download fabric-like images into a dedicated folder:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query fabric --count 200 --output LocalArtifacts\Backgrounds\Wallhaven\Fabric
```

Dry-run one page without saving image files:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query paper --count 10 --max-pages 1 --dry-run
```

`--dry-run` still calls the API and writes `manifest.json`, but it does not download image binaries. It is useful for checking search parameters before downloading a large batch.

## Practical Examples

Paper-like backgrounds for printed text:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query paper --count 300 --atleast 1024x1024 --output LocalArtifacts\Backgrounds\Wallhaven\Paper
```

Fabric or sack-like backgrounds:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query "fabric texture" --count 300 --sorting relevance --output LocalArtifacts\Backgrounds\Wallhaven\Fabric
```

Concrete or wall backgrounds:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query "concrete wall" --count 200 --sorting relevance --output LocalArtifacts\Backgrounds\Wallhaven\Concrete
```

Mostly recent images:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query paper --sorting date_added --order desc --count 100
```

Top images from the last six months:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query texture --sorting toplist --top-range 6M --count 100
```

Random search with a stable seed:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query paper --sorting random --seed obb-paper-v1 --count 100
```

JPEG-only search:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query "paper type:jpg" --count 100
```

Large landscape backgrounds:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query texture --atleast 1920x1080 --ratios 16x9 --count 100
```
