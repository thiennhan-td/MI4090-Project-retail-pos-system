---
name: Retail Core POS
colors:
  surface: '#f8f9ff'
  surface-dim: '#cbdbf5'
  surface-bright: '#f8f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#eff4ff'
  surface-container: '#e5eeff'
  surface-container-high: '#dce9ff'
  surface-container-highest: '#d3e4fe'
  on-surface: '#0b1c30'
  on-surface-variant: '#444651'
  inverse-surface: '#213145'
  inverse-on-surface: '#eaf1ff'
  outline: '#757682'
  outline-variant: '#c5c5d3'
  surface-tint: '#4059aa'
  primary: '#00236f'
  on-primary: '#ffffff'
  primary-container: '#1e3a8a'
  on-primary-container: '#90a8ff'
  inverse-primary: '#b6c4ff'
  secondary: '#0051d5'
  on-secondary: '#ffffff'
  secondary-container: '#316bf3'
  on-secondary-container: '#fefcff'
  tertiary: '#00312c'
  on-tertiary: '#ffffff'
  tertiary-container: '#004942'
  on-tertiary-container: '#4ebdb0'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#dce1ff'
  primary-fixed-dim: '#b6c4ff'
  on-primary-fixed: '#00164e'
  on-primary-fixed-variant: '#264191'
  secondary-fixed: '#dbe1ff'
  secondary-fixed-dim: '#b4c5ff'
  on-secondary-fixed: '#00174b'
  on-secondary-fixed-variant: '#003ea8'
  tertiary-fixed: '#89f5e7'
  tertiary-fixed-dim: '#6bd8cb'
  on-tertiary-fixed: '#00201d'
  on-tertiary-fixed-variant: '#005049'
  background: '#f8f9ff'
  on-background: '#0b1c30'
  surface-variant: '#d3e4fe'
typography:
  display-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 36px
    fontWeight: '700'
    lineHeight: 44px
  display-lg-mobile:
    fontFamily: Plus Jakarta Sans
    fontSize: 28px
    fontWeight: '700'
    lineHeight: 36px
  headline-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 30px
    fontWeight: '700'
    lineHeight: 38px
  headline-lg-mobile:
    fontFamily: Plus Jakarta Sans
    fontSize: 24px
    fontWeight: '700'
    lineHeight: 32px
  headline-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 22px
    fontWeight: '600'
    lineHeight: 28px
  headline-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 18px
    fontWeight: '600'
    lineHeight: 24px
  title-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 22px
  title-sm:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
  body-lg:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-lg:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
  label-md:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
  label-sm:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '600'
    lineHeight: 14px
  numeric-display:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '700'
    lineHeight: 32px
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  gutter: 1rem
  gutter-desktop: 1.5rem
  margin: 1rem
  margin-desktop: 2rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 0.75rem
  space-lg: 1rem
  space-xl: 1.5rem
---

## Brand & Style
The design system embodies the efficiency, unwavering reliability, and clarity essential to high-throughput retail operations and enterprise ERP management. It serves store clerks, inventory managers, and enterprise retail operators who demand immediate transactional feedback and low cognitive fatigue under fast-paced checkout conditions.

The visual direction combines Modern Utilitarian SaaS with crisp, tactile structural clarity. Interfaces feature high information density without clutter, precise dividing lines, clean monochromatic workspaces, and deliberate chromatic cues for critical transaction states, barcode scans, and quick-key actions.

## Colors
The palette is built around deep Navy (`#1E3A8A`) and operational Royal Blue (`#2563EB`), projecting authority, system stability, and enterprise discipline. Teal (`#0D9488`) handles operational confirmations, inventory reconciliation, and successful billing flows. Complementary dynamic accents (e.g., energetic amber/orange `#F97316`) are reserved for active checkout carts, pending shifts, alerts, and terminal payment states.

Surface foundations use clean slate tones (`#F8FAFC`, `#F1F5F9`) paired with pure white canvas containers (`#FFFFFF`) to ensure screen glare reduction under store lighting and maximum barcode/SKU legibility. Neutral slate grays provide high-contrast text hierarchies, preventing visual degradation across varying hardware displays.

## Typography
Typography is paired to balance front-of-house customer friendliness with back-of-house analytical accuracy. Plus Jakarta Sans handles primary store headers, section summaries, and operational dashboards with modern energy. Inter serves as the workhorse for body, labels, inventory grids, and tabular cash totals due to its neutral structure and high legibility at micro scales.

Tabular figures (`font-variant-numeric: tabular-nums`) must be strictly applied across receipt item lines, pricing arrays, barcode strings, and POS keypad summaries to guarantee flawless vertical alignment of currency values.

## Layout & Spacing
The layout follows a responsive, fluid grid engineered for mixed device usage: high-density multi-pane POS terminal screens (1080p, touch screens) alongside standard tablet and mobile handheld scanning units.

- **Desktop & POS Terminals (≥1024px):** Fixed-split or 12-column operational layout. The primary POS workstation splits into a flexible catalog/SKU matrix (60–65% width) and a fixed-width sticky transaction receipt panel (35–40% width, min-width 380px). Gutters set at `1.5rem` (`gutter-desktop`) with outer screen margins of `2rem` (`margin-desktop`).
- **Tablet / Mobile Devices (<1024px):** Single-column layout transitioning to a tabbed workflow (e.g., Switch between "Catalog" and "Cart / Payment"). Gutters scale down to `1rem` (`gutter`) and canvas margins tighten to `1rem` (`margin`) to maximize touch target density.

Spacing scale strictly defines component interior padding and modular gaps, maintaining an ultra-efficient 4px baseline rhythm.

## Elevation & Depth
The system prioritizes functional planar separation over heavy decorative shadows to maintain performance on low-spec POS hardware and kiosk terminals.

- **Structural Outlines:** Primary visual hierarchy is established using crisp, low-contrast 1px solid borders (`#E2E8F0`) defining panel containers, checkout lines, and inventory tables.
- **Tonal Layers:** The screen background rests on `#F8FAFC`. Base working cards, product grids, and receipt drawers sit on pure `#FFFFFF`. Header bars, sticky summary panels, and auxiliary toolbars use subtle cool-gray fills (`#F1F5F9`) or deep brand navy backgrounds (`#1E3A8A`).
- **Tactile Depth & Modals:** Elevated surfaces (such as cash register drawers, dropdown discount selectors, and payment processing modals) use tight, low-diffusion ambient drop shadows (`0 4px 12px -2px rgba(15, 23, 42, 0.08)`). Touch feedback triggers instant inset borders and tonal shifts rather than dramatic 3D transformations.

## Shapes
A soft, structured radius profile (`roundedness: 1` -> 0.25rem / 4px base) delivers an enterprise-grade, clean-edged appearance optimal for dense financial and inventory interfaces. Small components (buttons, input fields, badges) maintain crisp 4px corners, while large containers, modal sheets, and card panels use 8px (`rounded-lg`), maximizing screen real estate and mirroring modern POS hardware ergonomics without organic bloat.

## Components

### Buttons
- **Primary Transaction (Checkout / Pay):** Filled `#2563EB` with white text, 4px border radius. Minimum height 48px on POS screens for reliable finger taps; 38px on desktop ERP. Font weight 600.
- **Secondary (Hold Order, Draft, Print):** White surface with 1px border (`#CBD5E1`) and navy text (`#1E3A8A`). Hover state shifts to `#F1F5F9`.
- **Destructive / Cancel (Void, Remove SKU):** Subtle light-rose background with crimson text; active state prompts explicit confirmation modal.

### Chips & Status Badges
- Compact padding (`0.25rem` top/bottom, `0.5rem` left/right), uppercase or capitalized label typography (`label-sm`).
- **Paid / Completed:** Pale emerald background with `#0D9488` text.
- **Pending / In-Cart:** Soft amber/orange background with `#C2410C` text.
- **Out of Stock / Inactive:** Slate gray `#F1F5F9` background with `#64748B` text.

### Input Fields & Barcode Scanners
- Bordered by default with 1px `#CBD5E1`, background `#FFFFFF`. Height: 44px (touch-friendly).
- Quick Barcode/Search Field: Prominent placement with prefix icon, persistent autofocus indicator (`2px solid #2563EB` ring), and immediate inline clear-action button.
- Numeric Keypad Inputs: Center-aligned, monospaced tabular numerals with rapid stepper toggles (+ / -) sized at minimum 40x40px touch boundaries.

### Checkboxes & Radio Buttons
- Precision 18x18px squares/circles with 1px `#94A3B8` borders.
- Selected state fills with `#2563EB` containing a crisp white tick or central dot. Touch hitboxes extend to an invisible 44x44px boundary.

### Lists & Inventory Tables
- High-density rows with alternating hover highlights (`#F8FAFC`).
- Row borders: 1px bottom divider `#E2E8F0`. Numeric columns (Price, Stock, Quantity, Total) strictly right-aligned with tabular figures; SKU and descriptions left-aligned.

### Cards & Tile Matrix (Catalog Quick-Keys)
- Product Tiles: Compact cards with 1px solid border `#E2E8F0`, 4px radius, displaying high-contrast product title, inventory count indicator, and bold price tag. Press/touch state invokes immediate background tint to `#EFF6FF` for instant visual acknowledgement.

### Specialized POS Components
- **Numpad Grid:** 3x4 block grid of prominent mechanical-touch keys for tender input, discount calculation, and manual SKU entry.
- **Cart Summary Drawer:** Anchored bottom or right rail displaying running Subtotal, Tax/VAT, Discounts, and a large-type Grand Total indicator.