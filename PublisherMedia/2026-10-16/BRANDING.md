# Framing revision

The approved capsule-and-steps vector design, palette, and text-free direction are unchanged. Each variant is framed from its actual foreground bounds, including its stroke widths, instead of centering an oversized logical canvas. The whole mark sits 2.5% left of geometric center to counterbalance the brighter capsule on the right.

Card, cover, and social use 78% foreground height with equal top and bottom margins. Icon uses 76% and heavier capsule/stair strokes, yielding approximately 2 pixel edges at the actual 80-pixel portal icon size.

Sources and PNGs: cover 1950×1300, card 420×280, icon 160×160, social 1200×630. `review-*` PNGs are vector-render previews at observed portal slot sizes: 80×80, 210×140, and 475×325. They are QA artifacts, not upload files.

The portal requires a 1200×630 social image, while its saved field thumbnail was observed at 300×325 with a left-aligned cover crop. Keep the correct wide image centered; verify the crop/save preview and full-size image separately. Do not shift or distort the published artwork to mask this display defect.

`build-branding.cjs` uses the pinned Sharp dependency, `__dirname`, and editable SVG sources. Its `social-crop-source-2400x1260.png` is for the portal's crop tool, not a direct upload slot. No text, publisher logo, raster image, or external reference appears in the SVG artwork. Review previews, prior branding and intermediate captures stay local and are ignored by Git.
