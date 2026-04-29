# Recursive Pipeline

Краткий справочник по рекурсивной pipeline-модели и `window`-scope.

## Идея

| Уровень | Что это значит |
| --- | --- |
| Верхний pipeline | По-прежнему линейный список `stages`. |
| Вложенный pipeline | Любой stage может подключать другой pipeline через `pipeline-block`, `pipeline-program`, `pipeline-repeat`, `pipeline-select`. |
| Единообразие | Программа, подпрограмма и ещё одна подпрограмма описываются одинаково: через список `stages`. |

## Meta-stage

| YAML `type` | Назначение |
| --- | --- |
| `pipeline-block` | Inline-подпайплайн. |
| `pipeline-program` | Подключение именованной программы из `pipelinePrograms`. |
| `pipeline-repeat` | Повтор вложенного подпайплайна несколько раз. |
| `pipeline-select` | Выбор одного или нескольких program-вариантов по `group`. |

Все углы в YAML задаются в градусах, но без суффикса `Deg`: используем `rotation`, `globalRotation`.

## `pipelinePrograms`

| Поле | Смысл |
| --- | --- |
| `name` | Уникальное имя программы. |
| `group` | Семейство, по которому работает `pipeline-select`. |
| `weight` | Вес варианта при случайном выборе. |
| `stages` | Вложенный список stage. |

## `window`

`window` поддерживается у всех render-stage и у composite pipeline-stage.

| `mode` | Смысл |
| --- | --- |
| `FullFrame` | Весь текущий scope. |
| `SingleRect` | Один прямоугольный регион внутри текущего scope. |
| `ScatteredRects` | Несколько случайных прямоугольников внутри текущего scope. |

## Наследование scope

| Ситуация | Что происходит |
| --- | --- |
| Render-stage без `window` | Рисует во весь текущий scope. |
| Render-stage с `window` | Создаёт дочернее окно внутри текущего scope и рисует только в нём. |
| Composite-stage с `window` | Выполняет все дочерние stage внутри выбранного дочернего окна. |
| Вложенный composite-stage | Работает по тем же правилам ещё глубже. |

## Типовой сценарий family-background

| Шаг | Что обычно используется |
| --- | --- |
| База | `pipeline-select` по группе `base` |
| Материал | `pipeline-select` по группе `material` |
| Загрязнение | `pipeline-select` или `pipeline-repeat` по группе `contamination` |
| Свет | `pipeline-select` по группе `light` |

## Лоскуты

| Что нужно | Как задать |
| --- | --- |
| Несколько patch-регионов | `pipeline-repeat` |
| Случайные patch-окна | `window: { mode: ScatteredRects, ... }` |
| Разный контент в patch | Внутри repeat использовать `tiled-pattern`, `image-folder`, `noise-background` и т.д. |
