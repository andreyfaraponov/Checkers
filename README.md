# Checkers

A 3D checkers game built with Unity featuring player vs AI gameplay with multiple difficulty levels.

## Genre
Turn-based strategy board game

## Architecture
- **Async/await pattern** using UniTask for turn-based game loop management
- **Event-driven input system** with clean subscription/unsubscription patterns
- **Interface-based player controllers** (`IPlayerController`) supporting both human and AI players
- **Threaded AI** using `UniTask.RunOnThreadPool()` for non-blocking move calculations
- **Procedural board generation** with 8x8 grid managed by `Board` class
- **Extensible AI system** with abstract `BaseBotController` supporting multiple difficulty implementations

## Key Features
- Player vs AI gameplay
- **AI Difficulty Levels**:
  - **Easy**: Random move selection from available options
  - **Medium**: Score-based evaluation with positional awareness and tactical thinking
    - Advancement toward promotion (+10 pts/row)
    - Center control preference (+5 pts)
    - Edge avoidance (-5 pts)
    - Vulnerability detection (-30 pts)
    - Queen piece valuation (+20 pts)
  - **Hard**: Minimax algorithm with alpha-beta pruning
    - 4-ply deep search (looks 4 moves ahead)
    - Multi-factor position evaluation
    - Material, positional, and mobility analysis
    - Tactical planning and strategic play
    - ~90-95% pruning efficiency
- Forced attack rules enforcement
- Multi-jump attack sequences
- Visual feedback with highlights and DOTween animations
- Queen/King piece promotion with full queen movement

## Complexity
**Intermediate** - Demonstrates modern Unity patterns including async/await with UniTask, event-driven architecture, background threading for AI, inheritance-based AI system, and clean separation of concerns between game logic and presentation.

## Tech Stack
- Unity 6000.2.7f2
- C# with UniTask (Cysharp)
- DOTween for animations
- Unity Input System 1.14.2
- Cinemachine 2.10.4

## Project Status
- ✅ Core game mechanics complete
- ✅ Easy AI difficulty implemented
- ✅ AI architecture foundation (Phase 1)
- ✅ Medium AI difficulty (Phase 2) - Score-based evaluation with tactical awareness
- ✅ Hard AI difficulty (Phase 3) - Minimax with alpha-beta pruning
- 🚧 Difficulty selection UI (Phase 4 - in planning)
- 🚧 Polish & testing (Phase 5 - in planning)
