# Playback Progress JSON Schema

This document defines the JSON structure used to persist playback progress for the local media player app. The file (e.g., `progress.json`) is stored in the app's local data folder and is the single source of truth for resume positions and the "Continue Watching" section.

---

## 1. Top-Level Structure

```
{
  "schemaVersion": 1,
  "lastUpdatedUtc": "2024-05-01T12:34:56Z",
  "episodes": {
    "<episodeKey>": { ...EpisodeProgress... },
    "<episodeKey>": { ...EpisodeProgress... }
  },
  "continueWatching": [
    "<episodeKey>",
    "<episodeKey>"
  ]
}
```

- **`episodes`** is a dictionary/map keyed by a stable, unique **episode key** (not an array), so a single episode's record can be looked up and updated in O(1) without scanning a list.
- **`continueWatching`** is an ordered list of episode keys (most recent first) referencing entries in `episodes`, rather than duplicating episode data — avoids data duplication/drift between the two sections.
- **`episodeKey`** should be a deterministic identifier derived from the show name, season number, and episode number (e.g., `"show-name_s25_e01"`), or alternatively a hash of the full file path. Using the show/season/episode combination (rather than the raw file path) keeps progress stable even if files are moved between drives/folders, as long as naming stays consistent.

---

## 2. Top-Level Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `schemaVersion` | integer | Required | Version number of this JSON schema, used to support future migrations. |
| `lastUpdatedUtc` | string (ISO 8601 UTC datetime) | Required | Timestamp of the last write to this file, useful for diagnostics/sync. |
| `episodes` | object (map of `episodeKey` ? `EpisodeProgress`) | Required | All tracked episode progress records, keyed by episode key. |
| `continueWatching` | array of strings (`episodeKey`) | Required (may be empty array) | Ordered list of episode keys to display in the "Continue Watching" row, most recently watched first. |

---

## 3. `EpisodeProgress` Object

| Field | Type | Required | Description |
|---|---|---|---|
| `showName` | string | Required | Display name of the show, derived from the top-level folder name (cleaned of dots/underscores). |
| `season` | integer | Required | Season number, parsed from the `Sxx` token in the filename/folder. |
| `episode` | integer | Required | Episode number, parsed from the `Eyy` token in the filename. |
| `episodeTitle` | string | Optional | Episode title, if known/parsed from filename or fetched metadata. |
| `filePath` | string | Required | Full absolute path to the video file on disk, used to load the media for playback. |
| `positionTicks` | integer (long) | Required | Last playback position, stored in **ticks** (100-nanosecond units, matching .NET `TimeSpan.Ticks`) for precision; alternatively stored in seconds (see note below). |
| `durationTicks` | integer (long) | Optional | Total duration of the episode in ticks, once known (may be absent until first played, since duration is only available after the media is loaded). |
| `status` | string enum: `"NotStarted"`, `"InProgress"`, `"Completed"` | Required | Current watch status of the episode. |
| `lastWatchedUtc` | string (ISO 8601 UTC datetime) | Required | Timestamp of the last playback session for this episode; used to sort `continueWatching`. |
| `thumbnailPath` | string | Optional | Path to the cached generated thumbnail image for this episode, if already generated. |

### Notes on `positionTicks` / `durationTicks`
- Ticks are recommended over plain seconds for sub-second precision consistent with .NET `TimeSpan`, but a simpler **seconds-based** alternative (`positionSeconds`: double, `durationSeconds`: double) is equally valid if simplicity is preferred over precision — pick one convention and apply it consistently across the schema.
- `status` should be derived/maintained as follows:
  - `NotStarted`: no meaningful position recorded yet (position ? 0).
  - `InProgress`: `positionTicks` is greater than a small "just started" threshold (e.g., > 2% of duration) and less than a "near end" threshold (e.g., < 95% of duration).
  - `Completed`: position ? ~95% of duration, or an explicit "end reached" event fired.

---

## 4. Example

```json
{
  "schemaVersion": 1,
  "lastUpdatedUtc": "2024-05-01T20:15:30Z",
  "episodes": {
    "the-great-show_s25_e01": {
      "showName": "The Great Show",
      "season": 25,
      "episode": 1,
      "episodeTitle": "New Beginnings",
      "filePath": "D:\\Videos\\The.Great.Show\\Season 25\\The.Great.Show.S25E01.mkv",
      "positionTicks": 12345678900,
      "durationTicks": 22345678900,
      "status": "InProgress",
      "lastWatchedUtc": "2024-05-01T20:15:30Z",
      "thumbnailPath": "data\\thumbnails\\the-great-show_s25_e01.jpg"
    },
    "the-great-show_s25_e02": {
      "showName": "The Great Show",
      "season": 25,
      "episode": 2,
      "filePath": "D:\\Videos\\The.Great.Show\\Season 25\\The.Great.Show.S25E02.mkv",
      "positionTicks": 22345678900,
      "durationTicks": 22345678900,
      "status": "Completed",
      "lastWatchedUtc": "2024-04-28T19:02:11Z"
    }
  },
  "continueWatching": [
    "the-great-show_s25_e01"
  ]
}
```

---

## 5. Reading and Writing the File

### Reading
- On application startup, the **Progress Store Service** loads `progress.json` (if it exists) and deserializes it into an in-memory dictionary keyed by `episodeKey`.
- If the file is missing (first run), initialize an empty in-memory store with `schemaVersion` set to the current version and an empty `episodes`/`continueWatching`.
- If `schemaVersion` in the file is older than the app's current version, run a lightweight migration step before use (e.g., adding new optional fields with defaults) rather than discarding existing progress.
- If the file is corrupt/unreadable (invalid JSON), fall back to an empty store and optionally back up the corrupt file (e.g., rename to `progress.json.bak`) rather than crashing the app.

### Writing
- Updates happen in memory first (e.g., on `TimeChanged`, `Paused`, `Stopped`, `EndReached` events from the Playback Service), then flushed to disk:
  - **Debounced writes** during active playback (e.g., at most once every few seconds) to avoid excessive disk I/O from frequent position updates.
  - **Immediate flush** on pause, stop, app close, or when an episode transitions to `Completed`.
- Writes should use an **atomic save pattern**: serialize to a temporary file, then replace/rename over the actual `progress.json`, preventing corruption if the app crashes or loses power mid-write.
- After updating an `EpisodeProgress` entry, the app must also update the `continueWatching` list:
  - Remove the episode key if `status` becomes `Completed` or reverts to `NotStarted`.
  - Otherwise, move/insert the episode key at the front of `continueWatching` (most recently watched first), optionally trimming the list to a maximum length (e.g., 20 entries) for UI performance.
- `lastUpdatedUtc` at the top level is refreshed on every successful write, useful for diagnostics and potential future sync/backup features.
