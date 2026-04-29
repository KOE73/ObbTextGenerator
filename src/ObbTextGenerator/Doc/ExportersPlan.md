## План по экспортерам

| Этап | Что сделать                                                                                                             | Зачем                                                                            |
| ---- | ----------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| 1    | Ввести общий интерфейс `IExporter`                                                                                      | Чтобы все выходные артефакты строились единообразно из одного `GeneratedSample`  |
| 2    | Разделить экспортеры на `ground-truth`, `debug/preview`, `derived`                                                      | Чтобы не смешивать обучающие данные, предпросмотр и производные артефакты        |
| 3    | Сделать обязательную поддержку `annotationLayer` или `annotationLayers`                                                 | Чтобы один и тот же sample можно было выгружать по разным вариантам боксов       |
| 4    | Сначала реализовать базовые экспортеры: `image`, `yolo-obb`, `aabb`, `overlay-image`, `oriented-crops`, `metadata-json` | Это сразу закроет основные сценарии генерации, проверки и OCR pipeline           |
| 5    | Все экспортеры должны работать только с уже готовыми данными sample, без собственной генерации геометрии текста         | Чтобы геометрия и логика экспорта не смешивались                                 |
| 6    | `text-line` stage должна сохранять абстрактные данные строки, а не один прибитый OBB                                    | Чтобы из одной сцены можно было получать разные OBB/AABB/quad/crop представления |
| 7    | Экспортеры preview/debug должны уметь отдельный масштаб, формат и качество                                              | Чтобы экономить место и быстро проверять глазами                                 |
| 8    | Экспортеры derived должны уметь строить датасеты для OCR recognition                                                    | Чтобы тем же движком получать не только detection dataset, но и crops для OCR    |

## Таблица предложенных экспортеров

| Exporter                   | Категория              | Что сохраняет                                    | Основные параметры                                                            |
| -------------------------- | ---------------------- | ------------------------------------------------ | ----------------------------------------------------------------------------- |
| `image`                    | ground-truth           | Основное изображение sample без оверлеев         | `path`, `format`                                                              |
| `aabb`                     | ground-truth           | Axis-aligned боксы по выбранному слою аннотаций  | `path`, `annotationLayer`                                                     |
| `yolo-obb`                 | ground-truth           | OBB-разметку в формате YOLO OBB                  | `path`, `annotationLayer`, `classId`                                          |
| `quad` / `polygon`         | ground-truth           | 4 точки или polygon для текста                   | `path`, `annotationLayer`                                                     |
| `paddle-det`               | ground-truth           | Формат разметки, удобный для PaddleOCR detection | `path`, `annotationLayer`                                                     |
| `metadata-json`            | ground-truth / derived | Полный metadata-файл по sample                   | `path`, `includeStages`, `includeTexts`, `includeAnnotations`                 |
| `overlay-image`            | debug / preview        | Картинку с нарисованными поверх аннотациями      | `path`, `annotationLayers`, `drawMode`, `imageScale`, `format`, `jpegQuality` |
| `preview-image`            | debug / preview        | Уменьшенную превью-картинку без или с оверлеями  | `path`, `imageScale`, `format`, `jpegQuality`                                 |
| `contact-sheet` / `mosaic` | debug / preview        | Сводную картинку из нескольких sample            | `path`, `columns`, `thumbScale`, `drawLabels`                                 |
| `oriented-crops`           | derived                | Вырезанные ориентированные crops по quad/obb     | `path`, `annotationLayer`, `imageScale`, `padding`                            |
| `ocr-recognition`          | derived                | Crops + GT для recognizer-а                      | `path`, `annotationLayer`, `textSource`, `fileListFormat`                     |
| `stats-json`               | derived                | Статистику генерации в JSON                      | `path`, `includePerLayer`, `includeRejects`                                   |
| `stats-csv`                | derived                | Табличную статистику генерации                   | `path`, `includePerLayer`, `includeRejects`                                   |
| `rejected-log`             | derived / debug        | Лог отбракованных sample и причин                | `path`, `includeReasons`                                                      |

## Внутренние данные, из которых строятся конкретные структуры

| Структура          | Что должна содержать                                                                                         | Зачем нужна                                                                                |
| ------------------ | ------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------ |
| `GeneratedSample`  | Финальное изображение, id sample, seed, список scene objects, annotation layers, metadata, reject/debug info | Главный объект, из которого работают все экспортеры                                        |
| `TextLineInstance` | Исходный текст, позиция, угол, transform, line bounds, visual bounds, glyph list, font info                  | Абстрактное описание текстовой строки, из которого можно получить разные OBB/AABB/quad     |
| `GlyphInstance`    | Символ, индекс, glyph bounds, visual bounds, baseline info, transform                                        | Чтобы строить tight/uppercase/lowercase/visual варианты и делать узкий OCR                 |
| `AnnotationLayer`  | Имя слоя и список аннотаций одного типа                                                                      | Чтобы параллельно хранить `line.tight`, `line.uppercase`, `line.lowercase`, `word`, `char` |
| `AnnotationEntry`  | `label`, `quad`, optional `aabb`, `text`, `metadata`, `sourceObjectId`                                       | Универсальная единица экспорта в разные форматы                                            |
| `SceneObject`      | Тип объекта, transform, z-order, optional refs на текст/фон/эффекты                                          | Чтобы хранить общую сценовую модель, а не только пиксели                                   |
| `RenderTrace`      | Какие стадии сработали, какие параметры выпали случайно, какие ограничения сработали                         | Для отладки, воспроизводимости и metadata-json                                             |
| `RejectInfo`       | Флаг reject, причина, стадия, диагностические данные                                                         | Чтобы логировать отбракованные sample                                                      |
| `CropSource`       | Ссылка на annotation layer, текст, quad, padding, crop transform                                             | Чтобы exporter `oriented-crops` не вычислял всё заново                                     |
| `SampleStatistics` | Кол-во строк, символов, слоёв, reject count, min/max sizes, углы                                             | Для `stats-json` и `stats-csv`                                                             |

## Что именно должно быть в `TextLineInstance`

| Поле                                    | Что хранит                       | Зачем                                                  |
| --------------------------------------- | -------------------------------- | ------------------------------------------------------ |
| `Text`                                  | Полный текст строки              | Для OCR GT и metadata                                  |
| `FontFamily` / `FontStyle` / `FontSize` | Параметры шрифта                 | Для анализа и воспроизводимости                        |
| `AnchorPoint`                           | Базовая точка размещения         | Для воспроизводимости layout                           |
| `RotationDeg`                           | Угол строки                      | Для геометрии и debug                                  |
| `Transform`                             | Полный affine transform строки   | Чтобы считать quad/obb/crops без повторного угадывания |
| `LogicalBounds`                         | Логические bounds строки         | Для одного из вариантов боксов                         |
| `VisualBounds`                          | Реальные видимые bounds по ink   | Для `line.visual`                                      |
| `UppercaseBounds`                       | Bounds по прописным/верхней зоне | Для экспериментов с OBB                                |
| `LowercaseBounds`                       | Bounds по строчным/средней зоне  | Для экспериментов с OBB                                |
| `Glyphs`                                | Список `GlyphInstance`           | База для любых производных представлений               |
| `Words`                                 | Опциональная разбивка на слова   | Если понадобится word-level экспорт                    |
| `Metadata`                              | Произвольные теги                | Для гибких профилей и exporter-ов                      |

## Вывод по архитектуре

| Решение                                                                          | Принять |
| -------------------------------------------------------------------------------- | ------- |
| `text-line` stage рисует строку и сохраняет абстрактную модель строки            | Да      |
| Конкретные OBB/AABB/quad — это производные представления                         | Да      |
| Экспортеры не должны заново измерять текст по картинке                           | Да      |
| Один sample может быть выгружен многими exporter-ами по разным annotation layers | Да      |
| `oriented-crops` считать одним из основных exporter-ов                           | Да      |
