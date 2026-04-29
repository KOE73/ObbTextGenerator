# Color System Documentation

Подсистема цветов в ObbTextGenerator используется везде, где нужен цвет как конфигурируемое typed-описание:
- в `color` у паттернов и слоёв;
- в `text-line.color`;
- в `schemes.roles`.

Один и тот же формат конфигурации используется во всех этих местах.

## Формат

Цвет задаётся объектом с полем `type`.

```yaml
color: { type: constant, color: "#RRGGBB" }
```

## Поддерживаемые типы

| `type` | Назначение | Основные поля | Пример |
| --- | --- | --- | --- |
| `constant` | Константный цвет. | `color` | `color: { type: constant, color: "#RRGGBB" }` |
| `gray` | Оттенки серого (`R=G=B`). | `intensity`: значение `0-255` или диапазон `[min, max]`; `alpha`: значение или диапазон | `color: { type: gray, intensity: [50, 150], alpha: 255 }` |
| `random` | Случайный цвет. | `preset`: `any`, `dark`, `light`; либо `r/g/b/a` как значения или диапазоны | `color: { type: random, preset: "dark" }` |
| `from-scheme` | Берёт цвет из активной схемы по роли. | `role` | `color: { type: from-scheme, role: "text" }` |

Пример `random` с диапазонами каналов:

```yaml
color: { type: random, r: [180, 200], g: [160, 180], b: [130, 150], a: [100, 255] }
```

## Использование в схемах

Секция `schemes.roles` использует ту же подсистему цветов, а не отдельный формат.

```yaml
schemes:
  - name: "paper-dark-text"
    roles:
      bg: { type: random, r: [240, 255], g: [240, 255], b: [230, 245], a: [255, 255] }
      text: { type: random, r: [0, 50], g: [0, 50], b: [0, 50], a: [255, 255] }
      overlay_obb: { type: constant, color: "#FF2020" }
      overlay_box: { type: constant, color: "#20FF40" }
      overlay_xheight: { type: constant, color: "#20A0FF" }
```

`from-scheme` полезен в стадиях рендера и паттернах, когда цвет должен зависеть от выбранной схемы:

```yaml
color: { type: from-scheme, role: "bg" }
```

## Замечания

- `constant` удобен для debug-оверлеев и строго фиксированных цветов.
- `random` удобен для естественной вариативности без ручного перечисления палитр.
- `gray` удобен для нейтральных текстур и масок.
- `from-scheme` связывает локальную настройку с активной цветовой схемой sample.
