# Checkers

A 3D checkers game built with Unity featuring player vs AI gameplay.

## Genre
Turn-based strategy board game

## Architecture
- **Async/await pattern** using UniTask for turn-based game loop management
- **Event-driven input system** with clean subscription/unsubscription patterns
- **Interface-based player controllers** (`IPlayerController`) supporting both human and AI players
- **Threaded AI** using `UniTask.RunOnThreadPool()` for non-blocking move calculations
- **Procedural board generation** with 8x8 grid managed by `Board` class

## Key Features
- Player vs AI (Easy Bot) gameplay
- Forced attack rules enforcement
- Multi-jump attack sequences
- Visual feedback with highlights and DOTween animations
- Queen/King piece support (promotion mechanics in development)

## Complexity
**Intermediate** - Demonstrates modern Unity patterns including async/await with UniTask, event-driven architecture, background threading for AI, and clean separation of concerns between game logic and presentation.

## Tech Stack
- Unity 6000.2.7f2
- C# with UniTask (Cysharp)
- DOTween for animations
- Unity Input System 1.14.2
- Cinemachine 2.10.4
