# ZFileConverter visual system

ZFileConverter uses a midnight-and-mineral palette designed for a native Windows utility. Dark mode is the default; the light palette keeps the same hierarchy and semantic colors rather than inverting the interface.

## Core identity

| Token | Dark | Light | Purpose |
|---|---:|---:|---|
| Canvas | `#08111F` | `#F4F7FB` | Window background |
| Surface | `#111E30` | `#FFFFFF` | Cards and primary panels |
| Elevated surface | `#192A42` | `#EDF3FA` | Selected and raised controls |
| Primary accent | `#5F9CFF` | `#256FD6` | Primary actions, focus, progress |
| Accent deep | `#234F91` | `#D9E8FC` | Selection and supportive surfaces |
| Warm accent | `#FF8F69` | `#D9653D` | Preset identity and restrained emphasis |
| Primary text | `#F4F7FD` | `#10213A` | Titles and high-emphasis copy |
| Secondary text | `#A8B8CE` | `#52647C` | Supporting copy |
| Border | `#2B3D56` | `#D4DEEA` | Cards, inputs, and separators |

## Semantic intent

- Blue communicates action, selection, focus, and conversion progress.
- Coral is supportive rather than competitive. It highlights preset identity and select conversion moments.
- Error, warning, and success colors remain semantic and are checked against both surface systems.
- Pure black is avoided so layered dark surfaces remain visible.
- Pure white is reserved for the strongest text and icon moments.

## Brand mark

The icon combines a document, a custom Z, and a forward arrow. Source and generated assets live under:

```text
Tools/generate-brand-assets.py
Resources/Icons/
docs/brand/
```

The Windows ICO includes 16, 20, 24, 32, 40, 48, 64, 128, and 256 pixel frames.
