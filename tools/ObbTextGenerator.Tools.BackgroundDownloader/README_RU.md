# ObbTextGenerator.Tools.BackgroundDownloader

CLI-инструмент для скачивания локальных фоновых изображений под `image-folder` stages.

Сейчас поддерживается Wallhaven через официальный endpoint `api/v1/search`. Tool не парсит HTML-страницы и не добавляет скачанные изображения в репозиторий.

Файлы по умолчанию пишутся в локальные артефакты:

```text
LocalArtifacts/Backgrounds/Wallhaven
```

## Параметры

- `--source` — image source API. Сейчас поддерживается: `wallhaven`.
- `--output` — папка вывода. По умолчанию `LocalArtifacts/Backgrounds/Wallhaven`.
- `--count` — максимальное количество изображений для загрузки.
- `--query` — Wallhaven search query.
- `--categories` — Wallhaven categories bitmask: general/anime/people. Default: `100`.
- `--purity` — Wallhaven purity bitmask: sfw/sketchy/nsfw. Default: `100`.
- `--sorting` — сортировка результатов: `date_added`, `relevance`, `random`, `views`, `favorites`, `toplist`.
- `--order` — порядок результатов: `desc` или `asc`.
- `--top-range` — диапазон toplist: `1d`, `3d`, `1w`, `1M`, `3M`, `6M`, `1y`.
- `--atleast` — минимальное разрешение, например `1024x1024` или `1920x1080`.
- `--resolutions` — фильтр по точному разрешению, например `1920x1080,2560x1440`.
- `--ratios` — фильтр по aspect ratio, например `16x9,16x10`.
- `--colors` — Wallhaven color filter hex без `#`.
- `--seed` — seed для повторяемой random sorting pagination.
- `--api-key` — optional Wallhaven API key. Если не указан, используется `WALLHAVEN_API_KEY`.
- `--start-page` — первая страница результатов Wallhaven search.
- `--max-pages` — максимальное количество страниц для сканирования. `0` значит без явного лимита.
- `--api-delay-ms` — задержка между запросами страниц Wallhaven API.
- `--download-delay-ms` — задержка между скачиванием изображений.
- `--timeout-seconds` — HTTP timeout в секундах.
- `--overwrite` — перезаписывать существующие файлы с таким же generated name.
- `--dry-run` — сканировать API pages и писать manifest entries без скачивания image-файлов.
- `--file-name-pattern` — шаблон имени выходного файла. Поддерживаемые tokens: `{index}`, `{id}`, `{width}`, `{height}`.
- `--manifest` — имя manifest-файла внутри output directory.
- `--user-agent` — HTTP user agent.

## Query Syntax

`--query` передаётся в Wallhaven как параметр `q`. Он поддерживает обычные слова и операторы поиска Wallhaven.

Частые примеры:

| Query | Что значит |
| --- | --- |
| `paper` | Искать изображения по слову `paper`. |
| `"fabric texture"` | Искать текстовый запрос из двух слов. В PowerShell нужны кавычки, потому что есть пробел. |
| `+paper -anime` | Оставить/усилить `paper`, исключить `anime`. |
| `paper type:jpg` | Искать paper-like изображения и ограничить JPEG там, где Wallhaven поддерживает такой оператор. |
| `id:12345` | Поиск по id wallpaper, если это поддерживается API/search syntax. |

Точный набор операторов контролирует Wallhaven, а не этот tool. Если запрос работает на сайте Wallhaven, обычно его можно пробовать и в `--query`.

## Categories Bitmask

`--categories` — это трёхсимвольная bitmask Wallhaven:

```text
general anime people
   1      0      0
```

Каждая позиция — `1` включено или `0` выключено.

| Значение | Включённые категории |
| --- | --- |
| `100` | Только General. Это default и самый безопасный вариант для background textures. |
| `010` | Только Anime. |
| `001` | Только People. |
| `110` | General + anime. |
| `101` | General + people. |
| `011` | Anime + people. |
| `111` | Все категории. |

Для OCR-фонов обычно лучше начинать с `100`, потому что так меньше стилизованных и person-focused изображений.

## Purity Bitmask

`--purity` — ещё одна трёхсимвольная bitmask Wallhaven:

```text
sfw sketchy nsfw
 1     0      0
```

| Значение | Включённые purity levels |
| --- | --- |
| `100` | Только SFW. Это default. |
| `110` | SFW + sketchy. |
| `010` | Только sketchy. |
| `001` | Только NSFW. Нужен Wallhaven API key и аккаунт с доступом к NSFW. |
| `111` | SFW + sketchy + NSFW. Для NSFW нужен API-доступ. |

Для публичных датасетов и GitHub-friendly примеров лучше оставлять `--purity 100`.

## Sorting И Page Scan

`--sorting` управляет порядком результатов Wallhaven API:

| Значение | Когда полезно |
| --- | --- |
| `toplist` | Хороший default для визуально сильных изображений. Используется вместе с `--top-range`. |
| `relevance` | Полезно, когда `--query` важнее популярности. |
| `date_added` | Новые или старые изображения в зависимости от `--order`. |
| `random` | Случайные результаты. Для повторяемости используйте `--seed`. |
| `views` | Популярные по views. |
| `favorites` | Популярные по favorites. |

`--max-pages` ограничивает количество страниц API. Для небольших проверок удобно так:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query paper --count 10 --max-pages 1 --dry-run
```

Если `--max-pages 0`, tool будет запрашивать страницы, пока не скачает `--count` изображений или пока API не вернёт конец результатов.

## Output И Manifest

Каждый запуск пишет manifest рядом со скачанными изображениями:

```text
LocalArtifacts/Backgrounds/Wallhaven/manifest.json
```

В manifest сохраняются source URL, direct image URL, local path, image id, размеры, file type, file size, colors, status и ошибки, если они были.

Имена файлов задаются через `--file-name-pattern`.

Поддерживаемые tokens:

| Token | Что значит |
| --- | --- |
| `{index}` | Порядковый номер загрузки с leading zeros. |
| `{id}` | Wallhaven image id. |
| `{width}` | Ширина изображения. |
| `{height}` | Высота изображения. |

Пример:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query paper --file-name-pattern "paper_{index}_{width}x{height}_{id}"
```

## Rate Limit

У Wallhaven public API есть rate limit. Default `--api-delay-ms 1400` держит tool ниже документированного публичного лимита 45 API requests per minute.

Для скачивания файлов есть отдельная задержка:

```powershell
--download-delay-ms 200
```

Если API начинает возвращать HTTP 429, увеличьте задержки.

## Использование В Генераторе

Укажите скачанную папку в `image-folder` stage:

```yaml
- type: image-folder
  path: "../../LocalArtifacts/Backgrounds/Wallhaven/Fabric"
  recursive: true
  augment:
    fitMode: Cover
```

Пути резолвятся относительно директории активного config file. Если config лежит в `src/ObbTextGenerator/config.yaml`, то `../../LocalArtifacts/...` указывает на корневой `LocalArtifacts` репозитория.

## Лицензии

Скачанные изображения — это локальный user-provided dataset material. Они не являются частью репозитория и не покрываются лицензией исходного кода проекта.

Перед использованием изображений в датасете нужно отдельно проверить правила источника, license и copyright status.

## Базовое использование

Скачать 100 SFW general backgrounds в папку по умолчанию:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --count 100
```

Скачать fabric-like изображения в отдельную папку:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query fabric --count 200 --output LocalArtifacts\Backgrounds\Wallhaven\Fabric
```

Проверить одну страницу без скачивания image-файлов:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query paper --count 10 --max-pages 1 --dry-run
```

`--dry-run` всё равно вызывает API и пишет `manifest.json`, но не скачивает бинарники картинок. Это удобно, чтобы проверить параметры поиска перед большой загрузкой.

## Практические примеры

Фоны похожие на бумагу для печатного текста:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query paper --count 300 --atleast 1024x1024 --output LocalArtifacts\Backgrounds\Wallhaven\Paper
```

Фоны похожие на ткань или мешковину:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query "fabric texture" --count 300 --sorting relevance --output LocalArtifacts\Backgrounds\Wallhaven\Fabric
```

Бетон или стены:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query "concrete wall" --count 200 --sorting relevance --output LocalArtifacts\Backgrounds\Wallhaven\Concrete
```

Более свежие изображения:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query paper --sorting date_added --order desc --count 100
```

Top images за последние шесть месяцев:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query texture --sorting toplist --top-range 6M --count 100
```

Random search с фиксированным seed:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query paper --sorting random --seed obb-paper-v1 --count 100
```

JPEG-only search:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query "paper type:jpg" --count 100
```

Крупные landscape backgrounds:

```powershell
dotnet run -c Release --project .\tools\ObbTextGenerator.Tools.BackgroundDownloader\ObbTextGenerator.Tools.BackgroundDownloader.csproj -- download --query texture --atleast 1920x1080 --ratios 16x9 --count 100
```
