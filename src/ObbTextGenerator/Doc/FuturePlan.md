# Future Plan

These are plans for the distant future. Implement current tasks with them in mind—but without making them the primary focus.

## Forks
Pipeline is linear for now.
Future sample forking may be added at checkpoints if research scenarios require it.
Current design should not prevent future checkpoint-based sample cloning.

На данный момент конвейер является линейным.
В будущем, если того потребуют исследовательские сценарии, на контрольных точках может быть добавлено ветвление выборок.
Текущая архитектура не должна препятствовать реализации клонирования выборок на основе контрольных точек в будущем.

## Structured Surface Objects

### Problem

Current pipeline and `surface` mode are good for backgrounds, patches, labels, and other loosely structured fragments.
They become less convenient for flat composite objects whose internal layout is important.

Examples:
- license plates
- tags and labels with borders
- signs and table-like layouts
- composite flat objects with multiple text slots

The main limitation is that such objects require not only random rendering, but also exact local structure:
- border in a precise place
- separators in precise places
- text in named slots
- knowledge of which symbol was placed in which slot

### Why This Matters

For these cases it is better to separate two concerns:

| Concern | Meaning |
| --- | --- |
| Local object construction | Draw the object on its own flat surface in a convenient 2D coordinate system. |
| Scene placement | Insert the finished flat object into the main image with rotation, scaling, and later perspective distortion. |

This is cleaner than trying to draw everything directly in the final scene coordinates.

### Recommended Direction

Do not turn the generator into a generic drawing editor.
Instead, add a narrow higher-level system for structured flat objects.

Recommended properties of that system:
- local surface coordinates
- simple drawing primitives
- named slots/regions
- text placement into exact regions
- optional metadata about generated slot content
- final placement into the main scene through the existing `surface` mechanism

### Best Integration Strategy

The preferred model is:

1. Build the object on a local flat surface.
2. Keep all internal geometry in that local coordinate system.
3. Place the whole object into the main image as a single `surface`.
4. Apply global transforms at insertion time.
5. Add perspective distortion later at insertion time, not during internal object drawing.

This keeps object construction simple and makes later affine/perspective transforms apply consistently to both image content and annotations.

### Suggested Scope

The future system should stay intentionally narrow.
It should solve:
- flat structured objects
- exact local placement
- reusable object-like fragments

It should not try to solve:
- arbitrary vector graphics
- full interactive layout tooling
- a CorelDRAW-like general editor

### Russian Summary

Текущий `pipeline` и `surface` хорошо подходят для фонов, лоскутов и простых локальных фрагментов.
Но для объектов вроде автомобильных номеров, бирок, табличек и этикеток постепенно нужен следующий уровень: не просто набор stage, а система сборки плоского составного объекта.

Правильное направление:
- сначала собирать такой объект на своей локальной плоскости;
- там задавать рамки, линии, слоты и текст в точных местах;
- потом вставлять его в основной кадр как единый `surface`;
- affine и перспективные искажения применять именно на этапе вставки.

Такой подход даёт:
- более чистую архитектуру;
- удобные локальные координаты;
- предсказуемую геометрию;
- возможность позже добавить перспективу без усложнения внутренней логики объекта.

