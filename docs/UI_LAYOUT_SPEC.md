# UI Layout Specification — Local Netflix-Style Media Player

This document specifies the visual structure and layout behavior of the WPF UI, without prescribing implementation code. It covers four primary screens: **Home**, **Show Detail**, **Player**, and the shared **Navigation Shell**.

---

## 1. Navigation Shell (Application Frame)

- The application window uses a single root **Shell** containing:
  - A slim top **Header Bar** (fixed height, dark/transparent-over-content background): app logo/name on the left, a search box in the center-right area, and window controls (minimize/maximize/close) on the far right for a custom chrome look.
  - A **Content Frame/Navigation Host** beneath the header that swaps between Home, Show Detail, and Player screens.
- Navigation model: a simple back-stack (Home ? Show Detail ? Player), with a **Back button** appearing in the header whenever the content frame is not on Home.
- Transitions between screens should be a quick fade/slide to reinforce the "Netflix-like" feel, but must remain subtle and non-blocking.
- The header bar auto-hides or becomes semi-transparent while the Player screen is active and playback controls are visible, to maximize video real estate.

---

## 2. Home Screen (Library Grid)

### Structure (top to bottom)
1. **Continue Watching Row** (only rendered if at least one in-progress item exists)
2. **All Shows Grid** (primary browsing area)
3. Optional future rows (e.g., "Recently Added") follow the same row pattern.

### Continue Watching Row
- A horizontally-scrolling **row/carousel** container, wider cards than the main grid (landscape orientation, since these use episode thumbnails, not vertical posters).
- Each card top-to-bottom: thumbnail image ? thin progress bar overlaid at the bottom edge of the thumbnail ? show title + "S{season} E{episode}" subtitle beneath.
- Row has a left-aligned section header label ("Continue Watching") above it, consistent with subsequent rows.
- If the row overflows the window width, only a partial next card is revealed to visually hint at scrollability; a scroll affordance (mouse wheel, arrow buttons on hover, or touch/trackpad swipe) reveals more.

### All Shows Grid
- Section header label ("Shows" or "My Library") above the grid.
- A **wrapping grid** of uniformly-sized **poster cards** (portrait orientation, 2:3 aspect ratio), flowing left-to-right and wrapping to new rows as the window width allows — this is the primary "Netflix-style" browse surface.
- Each card: poster image fills the card; on hover, a subtle scale-up (~1.05x) and a soft shadow/border highlight; show title appears either permanently beneath the poster or only on hover as an overlay at the bottom of the image with a gradient scrim for legibility.
- Cards are uniform width/height; spacing (gutter) between cards is consistent both horizontally and vertically.
- Clicking/tapping a card navigates to the **Show Detail** screen for that show.

### Layout containers
- Outer content area: a vertically-scrolling container (single scrollbar for the whole Home screen) holding stacked rows.
- Each row: a horizontal container for Continue Watching; a wrapping container for the main grid.
- Section headers use consistent typography/margins across all rows for visual rhythm.

---

## 3. Show Detail Screen

### Structure (top to bottom or left/right split)
1. **Hero/Header Area**: large backdrop or poster image spanning the top of the screen (or a hero split: poster on the left, metadata on the right), with a gradient scrim so overlaid text remains legible.
   - Show title (large, bold typography).
   - Short description/synopsis text (2–4 lines, truncated with ellipsis if longer).
   - Metadata line (year, number of seasons, genre tags if available).
   - Primary action button: "Play" or "Resume" (resumes the most recently watched unfinished episode if applicable), plus a secondary "Play from S1E1" option if resuming.
2. **Season Selector**: a horizontal row of season tabs/pills ("Season 1", "Season 2", …) or a dropdown if the number of seasons is large; the selected season is visually highlighted (accent underline or filled pill).
3. **Episode List**: below the season selector, a list of episode cards for the selected season.

### Episode List Layout
- Prefer a **vertical list** (single column, full-width rows) rather than a grid, since episode cards benefit from showing more metadata per item:
  - Left: episode thumbnail (landscape, fixed width) with a small progress bar overlay at the bottom if partially watched, and a checkmark/"Watched" badge if completed.
  - Right of thumbnail: episode number + title (bold), duration, and a short description beneath.
  - Entire row is clickable/hoverable (subtle background highlight on hover) and navigates to the Player screen for that episode.
- Rows are separated by thin dividers or subtle background banding for readability in long lists.
- If a season has many episodes, the episode list scrolls independently within its section while the hero area and season selector remain visible/sticky at the top.

### Layout containers
- Hero area: a fixed-height container with layered image + text panel (either an overlay panel on top of the backdrop, or a two-column split panel).
- Season selector: a horizontal stack/wrap container of selectable pill controls.
- Episode list: a vertically-scrolling stacked list container.

---

## 4. Episode Playback Screen (Player)

### Structure (layered, full-window)
1. **Video Surface** (bottom-most layer): fills the entire content area (and ideally can expand to true fullscreen), maintaining the video's aspect ratio with letterboxing/pillarboxing as needed on mismatched window ratios.
2. **Top Overlay Bar** (appears on mouse movement / tap, auto-hides after a few seconds of inactivity during playback):
   - Back/close button (left) to return to Show Detail.
   - Episode title + "S{season} E{episode}" label (center or left-aligned next to back button).
3. **Bottom Control Bar** (same show/hide behavior as top overlay):
   - **Seek/progress bar** spanning the width, showing current position, buffered range (if applicable), and total duration; draggable scrubber handle.
   - Below or beside the seek bar, a row of transport controls: Play/Pause toggle (center, largest button), Previous/Next Episode (either side of Play/Pause), Rewind/Forward-skip (e.g., ±10s), Volume slider/mute toggle, elapsed time / remaining time labels, and a Fullscreen toggle at the far right.
4. **Center overlay controls (optional)**: a large Play/Pause icon that briefly flashes in the center of the video on toggle, and a subtle loading/buffering spinner when applicable.

### Behavior details
- Overlays are semi-transparent dark panels layered above the video, never resizing/pushing the video surface — they float on top so the video always occupies the full available area.
- Single click/tap on the video surface toggles Play/Pause; mouse movement reveals overlays; overlays fade out after ~3 seconds of no interaction during active playback.
- On approaching the end of the episode (last ~10–15 seconds), an optional "Next Episode" prompt card can appear in the bottom-right corner with a countdown, allowing autoplay-next or dismissal — mirroring the Netflix pattern.
- Progress bar updates in near-real-time from the playback position and is the same visual component (styled consistently) used elsewhere (Continue Watching, episode list) for design consistency.

### Layout containers
- A single **layered/overlapping container** (z-order based) hosting: video surface at the base, top bar anchored to the top, bottom control bar anchored to the bottom, and optional center/corner overlay elements positioned absolutely within the same layered container.

---

## 5. Metadata Presentation Guidelines

- **Posters** (Show-level): portrait 2:3 images used consistently across Home grid and Show Detail hero; if no local/online poster exists, use a standardized placeholder poster with the show title text centered.
- **Episode thumbnails**: landscape 16:9 images generated from the video; used in Continue Watching, Episode List, and the "Next Episode" prompt; placeholder is a generic film-frame icon if generation fails.
- **Titles**: consistent typography scale — largest for Show Detail hero title, medium for episode titles/show card captions, small for subtitles (season/episode numbers, duration).
- **Descriptions**: truncated with ellipsis in list/grid contexts; full text only shown in the Show Detail hero or an expandable "More info" panel.
- **Progress bars**: a single consistent visual style (thin bar, accent color fill over a muted track) reused across Continue Watching cards, Episode List rows, and the Player's seek bar, so users learn one visual language for "watched" state.

---

## 6. Responsive/Adaptive Behavior

- **Grid and row containers** (Home screen, Episode thumbnails in rows) use wrapping/flow layouts so the number of visible columns increases or decreases fluidly with window width, rather than fixed column counts — card size stays constant while column count adapts.
- **Minimum window size** should guarantee at least 2–3 poster cards per row and legible header/controls; below this, allow horizontal scrolling rather than shrinking cards below a readable size.
- **Show Detail hero area** collapses from a two-column (poster + text) layout to a stacked single-column layout (backdrop image on top, text beneath) on narrower windows.
- **Player controls** scale their touch/click target sizes and spacing based on window size, and the bottom control bar wraps secondary controls (volume, next-episode) into a collapsed menu on very narrow windows while keeping Play/Pause and the seek bar always visible.
- **Text truncation and ellipsis** rules apply consistently across all card types when horizontal space is constrained, rather than allowing text to wrap unpredictably and break card alignment.
- The overall design should remain usable when maximized on large displays (wider grids, larger hero images) and when resized to a smaller windowed mode (fewer columns, stacked hero layout), without requiring separate designs per breakpoint — layout containers should reflow rather than switch to entirely different structures.
