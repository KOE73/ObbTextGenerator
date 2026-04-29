# Demo Run Guide / Инструкция запуска demo

## EN

Run these files from the `demo` folder in this order.

1. `LoadImageBg.cmd`

   Downloads general background images into `../LocalArtifacts/Backgrounds`.
   This is required for the default demo unless this folder already contains images.
   One demo scene variant generates new text over these backgrounds.

2. `LoadImageText.cmd`

   Downloads images that may contain typography or text into `../LocalArtifacts/TextImage`.
   This is required for the default demo unless this folder already contains images.
   One demo scene variant uses these images as full-frame inputs without generating extra text.

3. `GenWithText.cmd`

   Runs `ObbTextGenerator` with `config_text.yaml`.
   The demo writes generated data to `../LocalArtifacts/GenData`.

After generation, check:

- `../LocalArtifacts/GenData/train/images` and `../LocalArtifacts/GenData/val/images` for generated images.
- `../LocalArtifacts/GenData/*/labels_obb_font` for generated text OBB labels.
- `../LocalArtifacts/GenData/*/labels_box` for YOLO box labels.
- `../LocalArtifacts/GenData/*/labels_obb_paddledet` for PaddleDet labels.
- `../LocalArtifacts/GenData/*/debug_preview` for visual debug panels.
- `../LocalArtifacts/GenData/*/overlay_*` for overlay images.
- `../LocalArtifacts/GenData/*/context_dump` for per-sample debug text dumps.

## RU

Запускай эти файлы из папки `demo` в таком порядке.

1. `LoadImageBg.cmd`

   Скачивает обычные фоновые картинки в `../LocalArtifacts/Backgrounds`.
   Для стандартного demo этот шаг нужен, если в этой папке ещё нет изображений.
   Один вариант demo генерирует новый текст поверх этих фонов.

2. `LoadImageText.cmd`

   Скачивает картинки, где может быть типографика или текст, в `../LocalArtifacts/TextImage`.
   Для стандартного demo этот шаг нужен, если в этой папке ещё нет изображений.
   Один вариант demo использует эти картинки как full-frame inputs без дополнительной генерации текста.

3. `GenWithText.cmd`

   Запускает `ObbTextGenerator` с `config_text.yaml`.
   Результаты demo пишутся в `../LocalArtifacts/GenData`.

После генерации смотри:

- `../LocalArtifacts/GenData/train/images` и `../LocalArtifacts/GenData/val/images` — готовые изображения.
- `../LocalArtifacts/GenData/*/labels_obb_font` — OBB labels по сгенерированной геометрии текста.
- `../LocalArtifacts/GenData/*/labels_box` — YOLO box labels.
- `../LocalArtifacts/GenData/*/labels_obb_paddledet` — labels из PaddleDet слоя.
- `../LocalArtifacts/GenData/*/debug_preview` — визуальные debug panels.
- `../LocalArtifacts/GenData/*/overlay_*` — изображения с overlay-разметкой.
- `../LocalArtifacts/GenData/*/context_dump` — текстовые dumps состояния sample.
