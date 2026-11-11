---
name: Athletic Dark Inverted
colors:
  surface: '#051424'
  surface-dim: '#051424'
  surface-bright: '#2c3a4c'
  surface-container-lowest: '#010f1f'
  surface-container-low: '#0d1c2d'
  surface-container: '#122131'
  surface-container-high: '#1c2b3c'
  surface-container-highest: '#273647'
  on-surface: '#d4e4fa'
  on-surface-variant: '#e4bebc'
  inverse-surface: '#d4e4fa'
  inverse-on-surface: '#233143'
  outline: '#ab8987'
  outline-variant: '#5b403f'
  surface-tint: '#ffb3b1'
  primary: '#ffb3b1'
  on-primary: '#680011'
  primary-container: '#ff535b'
  on-primary-container: '#5b000e'
  inverse-primary: '#bb152c'
  secondary: '#7bd0ff'
  on-secondary: '#00354a'
  secondary-container: '#00a6e0'
  on-secondary-container: '#00374d'
  tertiary: '#94ccff'
  on-tertiary: '#003352'
  tertiary-container: '#3f97d8'
  on-tertiary-container: '#002c48'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#ffdad8'
  primary-fixed-dim: '#ffb3b1'
  on-primary-fixed: '#410007'
  on-primary-fixed-variant: '#92001c'
  secondary-fixed: '#c4e7ff'
  secondary-fixed-dim: '#7bd0ff'
  on-secondary-fixed: '#001e2c'
  on-secondary-fixed-variant: '#004c69'
  tertiary-fixed: '#cde5ff'
  tertiary-fixed-dim: '#94ccff'
  on-tertiary-fixed: '#001d32'
  on-tertiary-fixed-variant: '#004b74'
  background: '#051424'
  on-background: '#d4e4fa'
  surface-variant: '#273647'
  surface-base: '#0a1118'
  surface-elevated-1: '#111c26'
  surface-elevated-2: '#162433'
  surface-border: '#243647'
  athletic-crimson-dark: '#d62828'
  text-primary: '#f8fafc'
  text-secondary: '#94a3b8'
typography:
  display-hero:
    fontFamily: Plus Jakarta Sans
    fontSize: 56px
    fontWeight: '800'
    lineHeight: 64px
    letterSpacing: -0.03em
  display-hero-mobile:
    fontFamily: Plus Jakarta Sans
    fontSize: 36px
    fontWeight: '800'
    lineHeight: 44px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 40px
    letterSpacing: -0.02em
  headline-lg-mobile:
    fontFamily: Plus Jakarta Sans
    fontSize: 24px
    fontWeight: '700'
    lineHeight: 32px
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 20px
    fontWeight: '700'
    lineHeight: 28px
  headline-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 24px
  body-lg:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 26px
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 22px
  body-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 18px
  label-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 14px
    fontWeight: '700'
    lineHeight: 20px
    letterSpacing: 0.02em
  label-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.05em
  score-display:
    fontFamily: Plus Jakarta Sans
    fontSize: 48px
    fontWeight: '800'
    lineHeight: 48px
    letterSpacing: -0.01em
  score-display-mobile:
    fontFamily: Plus Jakarta Sans
    fontSize: 36px
    fontWeight: '800'
    lineHeight: 36px
    letterSpacing: -0.01em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  gutter: 1.5rem
  gutter-mobile: 0.75rem
  margin: 2rem
  margin-mobile: 1rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 1rem
  space-lg: 1.5rem
  space-xl: 2.5rem
---

## Brand & Style
The design system delivers an intense, high-impact athletic dark aesthetic engineered for professional handball clubs, sports management platforms, and passionate fanbases. Built upon an inverted dark architecture, it channels the kinetic power, agility, and grit of the indoor arena under halogen stadium lights.

The design movement combines **High-Contrast Athletic Modernism** with crisp data-dense functionalism. The experience evokes urgency, competitive pride, and technological precision. Deep navy-slate surfaces establish depth and eliminate glare on stadium sidelines, while crimson athletic red drives dominant actions, active states, and scoreline highlights. Electric cyan-blue injects secondary agility through tactical badges, telemetry markers, and supportive data points.

## Colors
The color hierarchy centers on an inverted dark ecosystem engineered for decisive athletic clarity:

- **Primary Athletic Red (`#e63946`)**: Dominant chromatic beacon. Drives prime action buttons, active navigation markers, score highlights, live game timers, and MVP/Winner tags. Under active presses or hover states, deepens to Crimson (`#d62828`).
- **Secondary Cyan-Blue (`#38bdf8`)**: High-contrast electric accent. Deployed across tactical statistics, secondary actions, active filter tags, supportive links, and verified club status indicators.
- **Tertiary Deep Cobalt (`#0077b6`)**: Structural sports blue. Powers focus rings, progress track fills, interactive hover rings, and secondary container fills.
- **Neutral Framework (`#94a3b8`)**: Slate steel for supporting metrics, sub-labels, icons, and deactivated UI states.
- **Dark Inverted Surfaces**:
  - `surface-base` (`#0a1118`): Canvas background, root stadium canvas.
  - `surface-elevated-1` (`#111c26`): Roster cards, fixture modules, and table backgrounds.
  - `surface-elevated-2` (`#162433`): Floating overlays, elevated dropdowns, and modal dialogs.
  - `text-primary` (`#f8fafc`): Crisp athletic white for numerical tallies, player names, and prominent titles.

## Typography
Plus Jakarta Sans commands headers, callouts, and scoreboard counters with geometric vigor, while Inter provides neutral legibility across dense lineups, match event timelines, and league standings tables.

All match time counters, scoreboard numbers, penalty shot tallies, and analytics data must implement `font-variant-numeric: tabular-nums` to eliminate layout displacement during rapid real-time updates. Text paired with primary red backgrounds must strictly use high-purity white (`#f8fafc`) to preserve contrast compliance in dark ambient environments.

## Layout & Spacing
A fluid 12-column grid system structures wide desktop layouts, compressing to 8 columns on tablet devices and 4 columns on mobile handsets. 

Vertical spacing maintains a strict 8px incremental scale (`space-xs` through `space-xl`). Outer margins pull tight to `margin-mobile` (1rem) on mobile viewports to maximize data density during match coverage, scorecards, and roster tables. Component paddings rely strictly on `space-sm` for compact interactive badges and `space-md` through `space-lg` for scorecard cards and data clusters.

## Elevation & Depth
Elevation is constructed through dark tonal layering complemented by subtle athletic accent glows and low-contrast perimeter strokes, avoiding soft muddy drop shadows:

- **Base Layer (Canvas)**: Inverted stadium background (`#0a1118`).
- **Level 1 (Cards, Tables, Roster Tiles)**: Surface 1 (`#111c26`) paired with a 1px border of `rgba(36, 54, 71, 0.6)`.
- **Level 2 (Modals, Overlays, Active Match Panels)**: Surface 2 (`#162433`) framed with 1px border of `rgba(230, 57, 70, 0.35)` or `rgba(56, 189, 248, 0.3)` and an ambient shadow: `0 12px 28px -4px rgba(0, 0, 0, 0.65)`.
- **Athletic Energy Radiance**: High-priority interactive triggers and live indicators utilize directional ambient glows (`0 0 20px rgba(230, 57, 70, 0.25)` or `0 0 16px rgba(56, 189, 248, 0.2)`).

## Shapes
A disciplined corner radius system centers around `0.5rem` (8px), establishing athletic stability and technical balance. Data tables, match event containers, and standard buttons adhere to the standard radius. Status badges, live match indicators, and player role tags scale up to pill structures (`rounded-full`) to immediately distinguish dynamic game markers from structural system modules.

## Components

### Buttons
- **Primary Athletic Button**: Solid vibrant red (`#e63946`) fill with pure white (`#f8fafc`) text (`label-lg`), height 44px, horizontal padding `space-lg`. Hover transitions to crimson (`#d62828`) with an ambient red aura (`0 0 16px rgba(230, 57, 70, 0.3)`).
- **Secondary Action Button**: Dark surface `#162433` with a 1px border in cyan-blue (`#38bdf8`), cyan text (`#38bdf8`), and 44px height. Hover switches to full cyan-blue fill (`#38bdf8`) with dark text (`#0a1118`).
- **Tertiary Ghost Button**: Transparent background with text in slate (`#94a3b8`), transitioning to `#f8fafc` with subtle `rgba(255, 255, 255, 0.05)` fill on hover.

### Chips & Badges
- **Live Match & Critical Indicator**: Solid red (`#e63946`) or translucent red (`rgba(230, 57, 70, 0.15)`) with `#e63946` text and border, pill-shaped (`rounded-full`), `label-md`.
- **Tactical & Role Chips**: Translucent cyan-blue (`rgba(56, 189, 248, 0.15)`) with `#38bdf8` text and 1px border in `rgba(56, 189, 248, 0.3)`. Used for court positions ("Pivote", "Extremo", "Portero").
- **Neutral Tags**: Surface `#162433` with slate text (`#94a3b8`).

### Input Fields & Controls
- **Text Inputs**: 44px height, background `#111c26`, border 1px solid `#243647`, text `#f8fafc`, placeholder `#94a3b8`. Focus state introduces a 1px border of `#38bdf8` and an ambient cyan ring (`0 0 0 2px rgba(56, 189, 248, 0.2)`).
- **Checkboxes & Radios**: 18px dimensions, 4px corner radius for checkboxes, full circle for radios. Unchecked state: `#111c26` with `#243647` border. Checked state: solid `#e63946` fill with white checkmark glyph.

### Cards & Match Widgets
- **Match Scorecard**: Base container on `#111c26` with 1px border of `#243647`. Includes top match-status strip (e.g., "EN DIRECTO" in red pill, "MINUTO 42"). Scores render in `score-display` font in crisp `#f8fafc`.
- **Roster & Player Cards**: `#111c26` surface, top photo aspect ratio 4:5, player squad number in ghost athletic typography, lower metadata shelf with cyan position badge and key match metrics.

### Tables & Leaderboards
- Dark header `#162433` with uppercase `label-md` in slate (`#94a3b8`).
- Row height: 48px, alternating row fills between `#0a1118` and `#111c26`.
- Active team row highlighted with a 3px vertical border indicator in `#e63946`.