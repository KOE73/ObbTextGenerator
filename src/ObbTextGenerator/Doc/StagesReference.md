# Stages Reference

Краткий справочник по встроенным этапам pipeline.

## Общие правила

| Правило | Смысл |
| --- | --- |
| Pipeline линейный | Этапы выполняются строго сверху вниз. |
| Rendering stages меняют `RenderContext` | Они рисуют, выбирают цвета, добавляют runtime-данные и annotation layers. |
| Render stages теперь поддерживают `window` | Любой рисующий этап может работать на всём кадре или только на части изображения. |
| Composite pipeline stages могут вкладываться | Можно собирать программу, подпрограмму и ещё одну подпрограмму единообразно. |
| Writing stages сохраняют текущее состояние sample | Они не должны пересчитывать геометрию по финальной картинке. |
| Порядок важен | Writer или overlay видит только то, что уже успели записать предыдущие этапы. |

## Встроенные этапы

### Scene

| YAML `type` | Класс | Что делает | Основной результат |
| --- | --- | --- | --- |
| `scheme-selector` | `SchemeSelectorStage` | Выбирает активную цветовую схему. | Заполняет `RenderContext.ActiveSchemeColors`. |
| `background-fill-color` | `BackgroundFillColorStage` | Заливает фон одним цветом. | Базовый фон изображения. |
| `image-folder` | `ImageFolderStage` | Загружает фон из папки с изображениями и применяет аугментацию. | Image-based background для всего кадра или окна. |
| `noise-background` | `NoiseBackgroundStage` | Накладывает шумовой слой через shader. | Текстура/грязь/вариативность фона. |
| `tiled-texture` | `TiledTextureStage` | Рисует повторяющуюся простую текстуру. | Дополнительный фон поверх базовой заливки. |
| `tiled-pattern` | `TiledPatternStage` | Берёт pattern из библиотеки и тайлит его по кадру. | Активный паттерн в контексте и паттерн на фоне. |
| `spotlight` | `SpotlightStage` | Накладывает радиальный световой акцент. | Локальное освещение поверх сцены. |
| `camera-effects` | `CameraEffectsStage` | Применяет blur/grain-подобный постпроцесс. | Постобработка всего sample. |

### Text

| YAML `type` | Класс | Что делает | Основной результат |
| --- | --- | --- | --- |
| `text-line` | `TextLineStage` | Генерирует и рисует текстовые строки. | `TextLines`, collision layer и отрисованный текст. |

### Visualization

| YAML `type` | Класс | Что делает | Основной результат |
| --- | --- | --- | --- |
| `annotation-overlay` | `AnnotationOverlayStage` | Рисует существующий annotation layer поверх изображения. | Preview/inspection overlay. |

### Pipeline

| YAML `type` | Класс | Что делает | Основной результат |
| --- | --- | --- | --- |
| `pipeline-block` | `PipelineBlockStage` | Выполняет вложенный список stage по порядку. | Локальный подпайплайн. |
| `pipeline-program` | `PipelineProgramStage` | Подключает именованную программу из `pipelinePrograms`. | Повторное использование готового подпайплайна. |
| `pipeline-repeat` | `PipelineRepeatStage` | Повторяет вложенный блок несколько раз. | Множественные проходы или лоскуты. |
| `pipeline-select` | `PipelineSelectStage` | Выбирает один или несколько program-вариантов по `group`. | Семейства вариантов сцены. |

### Writing

| YAML `type` | Класс | Что делает | Основной результат |
| --- | --- | --- | --- |
| `write-image` | `WriteImageStage` | Сохраняет bitmap sample в файл. | PNG/JPEG и т.п. |
| `write-debug-preview` | `WriteDebugPreviewStage` | Собирает debug-preview: картинка, overlays и текстовый trace в боковой панели. | Быстрый визуальный feedback без sidecar-файлов. |
| `write-yolo-obb` | `WriteYoloObbStage` | Пишет ориентированные боксы из `TextLines`. | `.txt` с YOLO OBB и optional feedback layer. |
| `write-yolo-box` | `WriteYoloBoxStage` | Пишет axis-aligned boxes из `TextLines`. | `.txt` с YOLO box и optional feedback layer. |

## Когда обычно ставить

| Место в pipeline | Обычно какие этапы |
| --- | --- |
| 1 | `scheme-selector` |
| 2..N | `background-fill-color`, `noise-background`, `tiled-texture`, `tiled-pattern` |
| После фона | `text-line` |
| После текста | `spotlight`, `camera-effects` |
| Перед записью preview | `annotation-overlay` |
| В конце | `write-image`, `write-yolo-obb`, `write-yolo-box` |

## Ключевые поля

| YAML `type` | На что смотреть в первую очередь |
| --- | --- |
| `scheme-selector` | `schemeName` |
| `background-fill-color` | `color` |
| `image-folder` | `path`, `recursive`, `minAlpha`, `maxAlpha`, `augment`, `window` |
| `noise-background` | `noiseType`, `minFreq`, `maxFreq`, `minOctaves`, `maxOctaves`, `minAlpha`, `maxAlpha`, `blendMode` |
| `tiled-texture` | `tileSize`, `alpha`, `rotation` |
| `tiled-pattern` | `patternName`, `group`, `blendMode`, `minAlpha`, `maxAlpha` |
| `text-line` | `provider`, `font`, `color`, `count`, `probability`, `rotation`, `x`, `y`, `classId` |
| `spotlight` | `minRadiusPercent`, `maxRadiusPercent`, `minAlpha`, `maxAlpha`, `blendMode` |
| `camera-effects` | `minBlur`, `maxBlur`, `grainAlpha` |
| `annotation-overlay` | `layerName`, `colorRole`, `strokeWidth`, `fill`, `showText` |
| `pipeline-block` | `stages`, `window` |
| `pipeline-program` | `programName`, `window` |
| `pipeline-repeat` | `minCount`, `maxCount`, `stages`, `window` |
| `pipeline-select` | `group`, `mode`, `minCount`, `maxCount`, `allowDuplicates`, `noneWeight`, `window` |
| `write-image` | `path`, `format`, `quality` |
| `write-debug-preview` | `path`, `format`, `quality`, `panelWidth`, `padding`, `fontSize`, `traceVerbosity`, `overlayLayers` |
| `write-yolo-obb` | `path`, `annotationLayer`, `boxType`, `feedbackLayer` |
| `write-yolo-box` | `path`, `boxType`, `feedbackLayer` |

## Зависимости между этапами

| Этап | От чего зависит |
| --- | --- |
| `background-fill-color` | Обычно полезнее после `scheme-selector`, если цвет берётся из схемы. |
| `image-folder` | Требует существующую папку с изображениями; путь резолвится относительно config directory. |
| `text-line` | Обычно опирается на уже подготовленный фон и часто на active color scheme. |
| `annotation-overlay` | Требует, чтобы нужный `layerName` уже был заполнен ранее. |
| `pipeline-program` | Требует существующую запись в `pipelinePrograms`. |
| `pipeline-select` | Требует хотя бы одну программу подходящей `group`, иначе quietly ничего не выберет. |
| `write-yolo-obb` | Требует заполненный `RenderContext.TextLines`. |
| `write-yolo-box` | Требует заполненный `RenderContext.TextLines`. |
| `write-debug-preview` | Лучше ставить после writers, которые уже заполнили `feedbackLayer`, если preview должен показывать готовые боксы. |
| `write-image` | Сохраняет всё, что уже нарисовано к моменту запуска. |

## Быстрые примеры

| Задача | Минимальный набор этапов |
| --- | --- |
| Простая генерация текста | `scheme-selector` -> `background-fill-color` -> `text-line` -> `write-image` -> `write-yolo-obb` |
| Family-based фон | `scheme-selector` -> `pipeline-select(base)` -> `pipeline-select(material)` -> `pipeline-select(contamination)` |
| Лоскуты по картинке | `pipeline-repeat` + `window.mode: ScatteredRects` + любой render stage внутри |
| Нужен быстрый preview с текстом справа | `...` -> `write-yolo-*` -> `write-debug-preview` |
| Нужен inspection overlay поверх OBB | `...` -> `write-yolo-obb` -> `annotation-overlay` -> `write-image` |
| Нужен обычный YOLO box | `...` -> `write-yolo-box` |

## Связанные документы

| Тема | Документ |
| --- | --- |
| `text-line` подробнее | [TextLineStage.md](C:/GitKOE/ObbTextGenerator/src/Doc/TextLineStage.md) |
| Экспорт и writing stages | [ExportersPlan.md](C:/GitKOE/ObbTextGenerator/src/Doc/ExportersPlan.md) |
| Рекурсивный pipeline и window-scopes | [RecursivePipeline.md](C:/GitKOE/ObbTextGenerator/src/Doc/RecursivePipeline.md) |
| Цветовая подсистема | [ColorSystem.md](C:/GitKOE/ObbTextGenerator/src/Doc/ColorSystem.md) |
| Подсистема паттернов | [PatternsSystem.md](C:/GitKOE/ObbTextGenerator/src/Doc/PatternsSystem.md) |
