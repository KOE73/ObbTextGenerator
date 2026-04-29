# Pattern System Documentation

Паттерны в ObbTextGenerator позволяют создавать сложные многослойные фоны с использованием процедурной генерации, геометрических примитивов и шумов.

## Структура конфигурации

Паттерны описываются в секции `patterns` файла `config.yaml`. Каждый паттерн имеет уникальное имя и настройки генерации плитки (tile).

```yaml
patterns:
  - name: "grid-paper"
    pattern:
      minTileSize: "50"
      maxTileSize: "10%"
      minGlobalRotation: -10
      maxGlobalRotation: 10
      layers:
        - type: fill
          ...
```

### Глобальные параметры паттерна
- `minTileSize` / `maxTileSize` (string): Размер квадратной плитки. 
    - Можно указывать в пикселях: `"50"`, `"100"`.
    - Можно указывать в процентах от размера холста: `"10%"`, `"100%"`. Это позволяет создавать не повторяющиеся, а растянутые на весь фон эффекты (например, дым или грязь).
- `minGlobalRotation` / `maxGlobalRotation` (float): Диапазон поворота всего заполненного паттерном холста в градусах.
- `layers` (list): Список слоев, из которых состоит паттерн. Слои отрисовываются последовательно.

---

## Типы слоев (`type`)

Каждый слой в списке `layers` имеет тип, определяющий его геометрию и поведение (фиксированное или случайное).

### 1. `fill`
Полная заливка всей плитки цветом.
- Использование: обычно идет первым слоем для задания базового цвета.

### 2. `rect-fixed`
Прямоугольник с четко заданными координатами.
- `rect` (list: [x, y, w, h]): Относительные координаты и размеры (0.0 - 1.0). По умолчанию `[0, 0, 1, 1]`.

### 3. `rect-random`
Случайные прямоугольники в случайных местах.
- `minCount` / `maxCount`: Количество объектов.
- `minWidth` / `maxWidth`, `minHeight` / `maxHeight`: Диапазоны размеров.
- `minRotation` / `maxRotation`: Диапазон поворота.

### 4. `circle-random`
Случайные круги.
- `minCount` / `maxCount`: Количество объектов.
- `minWidth` / `maxWidth`: Диапазон диаметров.

### 5. `line-random`
Случайные линии.
- `minCount` / `maxCount`: Количество объектов.
- `minThickness` / `maxThickness`: Толщина линии.

### 6. `grid-procedural`
Процедурная сетка.
- `spacingX` / `spacingY` (float): Шаг сетки (0.1 = 10% от стороны плитки).
- `minThickness` / `maxThickness`: Толщина линий.
- `minRotation` / `maxRotation`: Поворот сетки.
- `positionJitter` (float): Искажение линий. При значениях > 0 сетка становится неровной (эффект ткани).

### 7. `scatter-random`
Разбросанные мелкие черточки/ворсинки.
- `minCount` / `maxCount` (int): Количество.
- `minWidth` / `maxWidth`: Длина черточек.
- `minRotation` / `maxRotation`: Поворот объектов.

### 8. `perlin-procedural`
Шум Перлина (облака, пятна, дым).
- `baseFrequencyX` / `baseFrequencyY` (float): Масштаб шума (0.01 - 0.05).
- `octaves` (int): Детализация (1-5).

---

## Общие параметры любого слоя

- `color`: Настройки цвета. Подробно см. [ColorSystem.md](C:/GitKOE/ObbTextGenerator/src/Doc/ColorSystem.md).
- `blendMode`: Режим наложения (SrcOver, Multiply, Overlay, Screen и др.).
- `minAlpha` / `maxAlpha` (float, 0.0-1.0): Диапазон прозрачности конкретного слоя.

---

## Настройка цвета (`color`)

Описание подсистемы цвета вынесено в отдельный файл: [ColorSystem.md](C:/GitKOE/ObbTextGenerator/src/Doc/ColorSystem.md).

---

## Примеры

### Тканый мешок (rect-fixed)
```yaml
  - name: "woven-sack"
    pattern:
      minTileSize: "20"
      layers:
        - { type: fill, color: { type: from-scheme, role: bg } }
        - { type: rect-fixed, rect: [0, 0, 1.0, 0.5], color: { type: gray, intensity: 0 }, blendMode: Multiply, minAlpha: 0.1 }
        - { type: rect-fixed, rect: [0, 0, 0.5, 1.0], color: { type: gray, intensity: 255 }, blendMode: Overlay, minAlpha: 0.1 }
```

### Эффект дыма (perlin + 100%)
```yaml
  - name: "smoke-overlay"
    pattern:
      minTileSize: "100%"
      layers:
        - { type: perlin-procedural, baseFrequencyX: 0.02, octaves: 3, color: { type: constant, color: "#ffffff" }, minAlpha: 0.1, maxAlpha: 0.3 }
```
