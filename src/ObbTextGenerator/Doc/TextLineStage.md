# Text-Line Stage Documentation

`type: text-line` добавляет в sample одну или несколько текстовых строк, рисует их на холсте и сохраняет данные строки для последующих writing/export stages.

Эта стадия является основной точкой генерации текстового контента.

## Назначение

`text-line` stage:
- получает текст из `provider`;
- получает шрифт и размер из `font`;
- получает цвет из общей подсистемы цветов через `color`;
- пытается разместить строку без коллизий;
- рисует строку на canvas;
- сохраняет расширенное описание строки в `RenderContext.TextLines`.

Сама стадия не должна быть прибита к одному конкретному экспортному формату. Она формирует более абстрактное описание текстовой строки, которое потом используют exporters и overlay stages.

## Пример

```yaml
- type: text-line
  classId: 0
  minCount: 3
  maxCount: 9
rotation: 15
  color: { type: from-scheme, role: "text" }
  font: { type: system-random, minSize: 40, maxSize: 80 }
  provider:
    type: pattern
    templates: ["{N}-{NN} 50KG", "A{N}-{NN} 50KG", "{S:OM|CH}-{N} 50KG"]
```

## Основные поля

| Поле | Назначение |
| --- | --- |
| `provider` | Источник текста. |
| `font` | Настройки выбора шрифта и размера. |
| `color` | Цвет текста через общую подсистему цветов. |
| `minCount` / `maxCount` | Сколько строк попытаться сгенерировать за один sample. |
| `probability` | Вероятность запуска стадии для sample. |
| `rotation` | Поворот строки в градусах. Поддерживает sampled value выражения. |
| `layerName` | Имя слоя аннотаций, если логика будет опираться на него. |
| `classId` | Класс объекта для downstream-разметки. |

## Подсистема выбора шрифта

`text-line.font` использует отдельную typed-подсистему font providers.

Общий контракт сейчас такой:
- `GetTypeface(RenderContext context)` возвращает `SKTypeface`;
- `GetSize(RenderContext context)` возвращает размер шрифта.

Это значит, что stage не выбирает шрифт напрямую и не знает, откуда он пришёл. Она получает уже готовый provider через фабрику.

## Поддерживаемые типы `font`

| `type` | Назначение | Основные поля | Пример |
| --- | --- | --- | --- |
| `constant` | Фиксированный шрифт и фиксированный размер. | `family`, `weight`, `width`, `slant`, `size` | `font: { type: constant, family: "Arial", size: 36 }` |
| `system-random` | Случайный системный шрифт и случайный размер в диапазоне. | `includeGroups`, `excludeGroups`, `requiredGlyph`, `allowedWeights`, `allowedSlants`, `allowedWidths`, `minSize`, `maxSize` | `font: { type: system-random, includeGroups: ["practical-sans"], requiredGlyph: "Я", allowedWeights: [Normal, Bold], allowedSlants: [Upright], allowedWidths: [Normal], minSize: 40, maxSize: 80 }` |

## Примеры

Фиксированный шрифт:

```yaml
font: { type: constant, family: "Arial", size: 36 }
```

Случайный системный шрифт:

```yaml
font: { type: system-random, minSize: 40, maxSize: 80 }
```

Случайный системный шрифт с группами:

```yaml
font: { type: system-random, includeGroups: ["practical-sans", "practical-serif"], excludeGroups: ["decorative"], minSize: 40, maxSize: 80 }
```

Случайный системный шрифт с фильтром по глифу:

```yaml
font: { type: system-random, requiredGlyph: "Я", minSize: 40, maxSize: 80 }
```

Случайный системный шрифт с фильтрами по вариантам начертания:

```yaml
font: { type: system-random, includeGroups: ["industrial"], allowedWeights: [Normal, Bold], allowedSlants: [Upright], allowedWidths: [Normal, Condensed], minSize: 40, maxSize: 80 }
```

## FontGroups

Готовые группы шрифтов лежат в:
- [Resources/FontGroups](C:/GitKOE/ObbTextGenerator/src/Resources/FontGroups)

Сейчас из коробки добавлены группы:
- `practical-sans`
- `practical-serif`
- `industrial`
- `mono`
- `condensed`
- `bold-signage`
- `decorative`
- `handwritten-like`
- `cyrillic-friendly`
- `ui-sans`

Каждая группа хранится отдельным yaml-файлом:

```yaml
name: practical-sans
families:
  - Arial
  - Calibri
  - Segoe UI
  - Verdana
```

`includeGroups` оставляет только семейства из указанных групп.

`excludeGroups` вычитает семейства из указанных групп.

`requiredGlyph` остаётся отдельной проверкой: он не заменяет charset-проверку строки, а позволяет быстро отсечь шрифты, в которых нет хотя бы важного контрольного глифа, например `Я` для кириллицы.

`allowedWeights`, `allowedSlants`, `allowedWidths` фильтруют уже не семейства, а конкретные variant-начертания внутри семейства. Это позволяет явно задавать, нужны ли только `Upright`, только `Bold`, допустимы ли `Italic`, нужны ли `Condensed` и так далее.

## Как это работает сейчас

`constant`:
- строит `SKTypeface` по `family`, `weight`, `width`, `slant`;
- всегда возвращает один и тот же typeface;
- всегда возвращает один и тот же `size`.

`system-random`:
- при создании провайдера проходит по системным font families;
- при наличии `includeGroups` и `excludeGroups` сначала фильтрует список семейств по yaml-группам;
- для каждой семьи перечисляет доступные variant-начертания;
- применяет фильтры `allowedWeights`, `allowedSlants`, `allowedWidths`;
- если задан `requiredGlyph`, оставляет только variants, где этот глиф существует;
- при генерации строки случайно выбирает один typeface из подготовленного списка variants;
- размер каждый раз выбирается случайно в диапазоне `minSize..maxSize`.

## Текущие ограничения

- Фильтрация по `requiredGlyph` проверяет только один символ, а не полный набор символов строки.
- Нет встроенного способа ограничить выбор конкретным списком семей, категориями или regex-фильтром.
- Нет явной нормализации по coverage для многоязычных строк.
- Размер выбирается независимо от геометрических ограничений target frame, поэтому часть слишком крупных строк потом просто не размещается.

## Как работает стадия

1. Проверяет `probability`.
2. Выбирает случайное количество строк в диапазоне `minCount..maxCount`.
3. Для каждой строки получает текст, шрифт, размер и цвет.
4. Считает несколько видов bounds:
   - `tight`;
   - bounds по font metrics;
   - `cap-height`;
   - `x-height`.
5. Пытается найти позицию и угол без пересечения с уже занятыми областями.
6. Рисует текст на canvas.
7. Сохраняет `TextLineDescriptor` в runtime-состояние sample.

## Runtime-результат

После выполнения стадии в `RenderContext` появляются:
- нарисованный текст на изображении;
- collision-данные для размещения следующих строк;
- `TextLineDescriptor` с геометрией и метаданными строки.

Именно от этих готовых данных дальше должны работать overlay и exporters, а не пересчитывать геометрию по финальной картинке.

## Связанные подсистемы

- Цвет: [ColorSystem.md](C:/GitKOE/ObbTextGenerator/src/Doc/ColorSystem.md)
- Экспорт и writing stages: [ExportersPlan.md](C:/GitKOE/ObbTextGenerator/src/Doc/ExportersPlan.md)
