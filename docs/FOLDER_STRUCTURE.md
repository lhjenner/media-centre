# Project Folder Structure — Local Netflix-Style Media Player

This document defines an industry-standard, clean-architecture-aligned folder structure for the WPF/LibVLCSharp media player. It separates UI, domain, application, service, and data concerns into independent, testable projects so the codebase can scale without large monolithic classes or major restructuring.

---

## 1. Solution Layout Overview

```
MediaPlayerApp.sln
??? src/
?   ??? MediaPlayerApp.UI/                 (WPF presentation layer)
?   ??? MediaPlayerApp.Application/        (use cases / orchestration)
?   ??? MediaPlayerApp.Domain/             (core entities & business rules)
?   ??? MediaPlayerApp.Infrastructure/     (filesystem, VLC, JSON, HTTP)
?   ??? MediaPlayerApp.Common/             (cross-cutting utilities)
??? tests/
?   ??? MediaPlayerApp.Domain.Tests/
?   ??? MediaPlayerApp.Application.Tests/
?   ??? MediaPlayerApp.Infrastructure.Tests/
??? assets/
?   ??? (default placeholders, icons, fonts, styles source files)
??? data/                                  (runtime-generated, gitignored)
?   ??? progress.json
?   ??? metadata-cache.json
?   ??? thumbnails/
??? docs/
    ??? (planning, schema, UI spec documents)
```

**Dependency direction** (clean architecture): `UI ? Application ? Domain`, with `Infrastructure` implementing interfaces defined in `Application`/`Domain`. Domain has no dependencies on any other layer; Infrastructure depends on Domain/Application abstractions, never the reverse.

---

## 2. `MediaPlayerApp.Domain` — Core Entities & Business Rules

Pure C# class library, no WPF/UI/framework dependencies. Contains the fundamental concepts of the media library, independent of how they're stored, displayed, or played.

```
MediaPlayerApp.Domain/
??? Entities/
?   ??? Show.cs
?   ??? Season.cs
?   ??? Episode.cs
?   ??? WatchProgress.cs
??? ValueObjects/
?   ??? EpisodeKey.cs             (stable identifier: show+season+episode)
?   ??? SeasonEpisodeNumber.cs
?   ??? PlaybackPosition.cs
??? Enums/
?   ??? WatchStatus.cs            (NotStarted / InProgress / Completed)
??? Abstractions/                 (interfaces implemented by Infrastructure)
    ??? ILibraryRepository.cs
    ??? IProgressRepository.cs
    ??? IMetadataProvider.cs
    ??? IThumbnailGenerator.cs
    ??? IMediaPlaybackEngine.cs
```

- Keeps domain rules (e.g., what makes an episode "in progress") self-contained and independently testable.
- Abstractions here define the contracts that Infrastructure must fulfill, keeping Domain free of concrete filesystem/VLC/HTTP details.

---

## 3. `MediaPlayerApp.Application` — Use Cases & Orchestration

Coordinates domain entities and infrastructure abstractions to fulfill specific application use cases. No direct file I/O, VLC calls, or UI code — only orchestration logic and view-facing state.

```
MediaPlayerApp.Application/
??? UseCases/
?   ??? LibraryScanning/
?   ?   ??? ScanLibraryUseCase.cs
?   ?   ??? FileNameParsingService.cs
?   ??? Metadata/
?   ?   ??? ResolvePosterUseCase.cs
?   ?   ??? FetchOnlineMetadataUseCase.cs
?   ??? Thumbnails/
?   ?   ??? GenerateEpisodeThumbnailUseCase.cs
?   ??? Playback/
?   ?   ??? PlayEpisodeUseCase.cs
?   ?   ??? ResumePlaybackUseCase.cs
?   ??? Progress/
?       ??? UpdateProgressUseCase.cs
?       ??? GetContinueWatchingUseCase.cs
??? DTOs/
?   ??? ShowSummaryDto.cs
?   ??? EpisodeSummaryDto.cs
?   ??? ContinueWatchingItemDto.cs
??? Interfaces/
    ??? (application-level service interfaces, e.g. INavigationService)
```

- Each use case is a small, single-responsibility class — avoids one giant "LibraryManager" god-class.
- DTOs decouple domain entities from what the UI actually binds to, allowing UI-friendly shapes without polluting Domain.

---

## 4. `MediaPlayerApp.Infrastructure` — External Concerns

Implements the abstractions defined in `Domain`/`Application`, isolating all "impure" concerns: filesystem access, LibVLCSharp, JSON persistence, and HTTP calls to external metadata providers.

```
MediaPlayerApp.Infrastructure/
??? FileSystem/
?   ??? LibraryRepository.cs           (implements ILibraryRepository)
?   ??? VideoFileExtensions.cs
?   ??? FolderNameCleaner.cs
??? Playback/
?   ??? LibVlcPlaybackEngine.cs        (implements IMediaPlaybackEngine)
?   ??? LibVlcInitializer.cs           (singleton LibVLC bootstrap)
??? Thumbnails/
?   ??? VlcSnapshotThumbnailGenerator.cs (implements IThumbnailGenerator)
??? Metadata/
?   ??? LocalPosterProvider.cs
?   ??? TmdbMetadataProvider.cs        (implements IMetadataProvider)
?   ??? MetadataCacheStore.cs
??? Persistence/
?   ??? JsonProgressRepository.cs      (implements IProgressRepository)
?   ??? AtomicFileWriter.cs
??? Configuration/
    ??? AppPaths.cs                    (resolves data/, thumbnails/, cache paths)
```

- Any future storage/provider swap (e.g., SQLite instead of JSON, a different metadata source) only requires a new class here implementing the existing interface — no changes to Domain/Application/UI.

---

## 5. `MediaPlayerApp.UI` — WPF Presentation Layer

The startup/executable project. Contains only views, view models, and UI-specific concerns (converters, styles, controls). Depends on `Application` (and indirectly `Domain`), and is wired to `Infrastructure` implementations only at composition-root/startup time (dependency injection).

```
MediaPlayerApp.UI/
??? App.xaml / App.xaml.cs                 (composition root / DI setup)
??? Views/
?   ??? Shell/
?   ?   ??? MainWindow.xaml
?   ??? Home/
?   ?   ??? HomeView.xaml
?   ?   ??? ContinueWatchingRowView.xaml
?   ??? ShowDetail/
?   ?   ??? ShowDetailView.xaml
?   ??? Player/
?       ??? PlayerView.xaml
??? ViewModels/
?   ??? Shell/
?   ?   ??? MainWindowViewModel.cs
?   ??? Home/
?   ?   ??? HomeViewModel.cs
?   ?   ??? ContinueWatchingViewModel.cs
?   ??? ShowDetail/
?   ?   ??? ShowDetailViewModel.cs
?   ??? Player/
?       ??? PlayerViewModel.cs
??? Controls/
?   ??? MediaCardControl.xaml              (poster/thumbnail + title + progress bar)
?   ??? PlaybackControlsBar.xaml
??? Converters/
?   ??? (value converters for bindings)
??? Navigation/
?   ??? NavigationService.cs               (implements Application's INavigationService)
??? Resources/
    ??? Styles/
    ??? Themes/
    ??? Images/                            (placeholder posters/thumbnails)
```

- Views and ViewModels are grouped **by feature/screen** (Home, ShowDetail, Player) rather than by type, keeping related files close together and easing future feature additions.
- `MediaCardControl` is a single reusable control shared across Home grid, Continue Watching row, and Episode list — avoids duplicating card layout logic.

---

## 6. `MediaPlayerApp.Common` — Cross-Cutting Utilities

Small shared library for utilities with no business meaning of their own, used across multiple layers (referenced by Infrastructure and UI, kept independent of Domain).

```
MediaPlayerApp.Common/
??? Logging/
?   ??? ILogger.cs / simple logging abstraction
??? Threading/
?   ??? DebounceHelper.cs
??? Extensions/
    ??? StringExtensions.cs           (e.g., folder-name cleanup helpers)
```

---

## 7. Tests

Mirrors the `src/` structure so each layer's tests sit alongside its counterpart, encouraging test coverage as the codebase grows.

```
tests/
??? MediaPlayerApp.Domain.Tests/
?   ??? (entity rules, EpisodeKey generation, WatchStatus transitions)
??? MediaPlayerApp.Application.Tests/
?   ??? (use case orchestration tests with mocked abstractions)
??? MediaPlayerApp.Infrastructure.Tests/
    ??? (filename parsing, JSON round-trip, atomic write behavior)
```

---

## 8. Why This Structure Supports Future Expansion

- **New metadata providers** (e.g., swapping TMDB for another API) only require a new class in `Infrastructure/Metadata/`, implementing the existing `IMetadataProvider` contract — no other layer changes.
- **New UI screens** (e.g., a Settings screen, a Search results screen) slot into `UI/Views/<Feature>/` and `UI/ViewModels/<Feature>/` following the same per-feature pattern already established.
- **New playback backends** or a change away from LibVLCSharp would only touch `Infrastructure/Playback/`, since `Domain`/`Application` depend only on `IMediaPlaybackEngine`.
- **New progress storage** (e.g., SQLite, cloud sync) replaces `JsonProgressRepository` behind `IProgressRepository` without touching use cases or view models.
- Keeping Domain and Application free of WPF/LibVLCSharp/HTTP dependencies means core business logic remains portable (e.g., reusable in a future cross-platform or Avalonia UI shell) and easily unit-testable in isolation.
