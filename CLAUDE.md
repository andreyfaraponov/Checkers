# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 3D checkers game project built with C# (.NET). The game features a player vs AI implementation with async/await patterns using UniTask for managing turn-based gameplay.

**Unity Version**: 6000.2.7f2
**Key External Packages**:
- UniTask (Cysharp) - for async/await patterns
- DOTween - for animations and tweening
- Unity Input System 1.14.2
- Cinemachine 2.10.4

## Development Commands

### Opening the Project
Open the project in Unity Hub or directly in Unity Editor 6000.2.7f2.

### Building
Use Unity Editor's Build Settings (File > Build Settings) to build for your target platform.

### Running Tests
Use Unity Test Runner (Window > General > Test Runner) to execute tests.

### Editor Menu Commands
The project includes custom editor commands accessible via the Unity menu bar:
- `Board/Generate Board` - Generates the checkerboard in the scene
- `Board/Clear board` - Clears all board cells
- `Board/Detect black cells` - Debug utility to log black cell positions

## Architecture

### Core Game Loop
The game uses an async turn-based architecture managed by `GameController.cs`:
- `GameController` orchestrates the game loop, alternating between player and opponent turns
- Uses `StartGameLoopAsync()` to continuously await moves from each player controller
- The game loop runs until `_isGameEnd` is true (game end detection is currently a TODO)

### Player Controller Pattern
Both human and AI players implement `IPlayerController` interface with a single method:
- `UniTask AwaitMove()` - Returns a UniTask that completes when the player's turn is finished

**Implementations**:
- `PlayerWithInputController` - Handles human player input via mouse clicks on figures and board positions
- `EasyBotController` - AI implementation that runs move logic on a background thread and executes on main thread

### Board Management
The `Board` class (`Board.cs`) manages the game state:
- `_boardPositions[,]` - 2D array (8x8) representing the board state
- `_points` - List of all `PositionPoint` objects for event handling
- Generates board procedurally in `GenerateBoard()` method
- `LocateFigures()` places initial checkers pieces (white on bottom 3 rows, black on top 3 rows)

### Key Classes

**`PositionPoint.cs`**
- Represents a single cell on the board
- Stores position (X, Y), color (black/white), and current figure
- Fires `PointClickEvent` when clicked
- Handles figure movement with DOTween animation via `SetFigure()`

**`Figure.cs`**
- Represents a checker piece
- Tracks color (IsBlack), queen status (IsQueen), and knocked-out state
- Fires `PickFigureEvent` when clicked (via OnMouseDown)

**`CheckersBasics.cs`**
- Contains static utility methods for game rules
- `GetAvailableAttacksDictionary()` - Checks all 4 diagonal directions for valid attack moves
- Returns `AttackData` objects containing start position, attack position, and final landing position

### Move Validation
Move logic distinguishes between:
1. **Simple moves** - Diagonal moves to empty adjacent cells
2. **Attack moves** - Jumping over opponent pieces
3. **Forced attacks** - When attacks are available, they must be taken (enforced in player controller)
4. **Multi-jump attacks** - After an attack, if another attack is available with the same piece, player must continue

### Thread Safety
The `EasyBotController` uses `UniTask.RunOnThreadPool()` for move calculation, then switches to main thread with `UniTask.SwitchToMainThread()` before executing Unity API calls. This pattern avoids blocking the main thread during AI thinking time.

## Code Patterns

### Event-Driven Input
The player controller subscribes to events from board positions and figures:
- Subscribe/unsubscribe pattern in `ToggleEventSubscription()` ensures clean event handling
- Events are only active during the player's turn
- Always unsubscribe when turn completes

### Async/Await with UniTask
The project uses UniTask instead of standard C# Tasks for better Unity integration:
- `UniTaskCompletionSource` is used to create awaitable turn completion signals
- Async methods use `async UniTask` return type
- Use `.Forget()` on fire-and-forget tasks (e.g., `StartGameLoopAsync().Forget()`)

### Material and Visual Feedback
- Pieces and board cells use Unity Materials to show color (black/white)
- Highlight system uses `_highlightObject` GameObject activation to show valid moves
- DOTween handles smooth figure movement animations

## Known TODOs

From the codebase analysis:
1. `GameController.cs:41` - Game end detection and win condition checking not implemented
2. `PlayerWithInputController.cs:156` - Need feedback UI when player must attack but selects wrong piece
3. Queen/King promotion logic is defined (`IsQueen` property exists) but promotion mechanics not implemented
4. Commented-out code in `EasyBotController.cs` (lines 122-203) shows old attack detection methods - should be cleaned up or removed
