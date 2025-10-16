using System.Collections.Generic;
using System.Linq;
using Core;
using Gameplay;
using UnityEngine;

namespace Utils
{
	public enum GameState
	{
		Playing,
		PlayerWin,
		OpponentWin,
		Draw
	}

	public class CheckersBasics
	{
		private static readonly (int dy, int dx)[] Directions =
		{
			(1, -1),   // Left-Up
			(1, 1),    // Right-Up
			(-1, 1),   // Right-Down
			(-1, -1)   // Left-Down
		};

		public static Dictionary<PositionPoint, AttackData> GetAvailableAttacksDictionary(
			PositionPoint[,] board,
			PositionPoint figurePoint)
		{
			var result = new Dictionary<PositionPoint, AttackData>();
			var figure = figurePoint.Figure;

			if (figure == null)
				return result;

			// Both queens and regular pieces can attack in all 4 diagonal directions
			if (figure.IsQueen)
			{
				// Queens can attack long range in all 4 directions
				foreach (var (dy, dx) in Directions)
				{
					TryAddQueenAttacksInDirection(figurePoint, board, result, dy, dx);
				}
			}
			else
			{
				// Regular pieces can attack in all 4 diagonal directions (one square jump)
				foreach (var (dy, dx) in Directions)
				{
					TryAddAttackInDirection(figurePoint, board, result, dy, dx);
				}
			}

			return result;
		}

		public static List<PositionPoint> GetAvailableSimpleMoves(
			PositionPoint[,] board,
			PositionPoint figurePoint)
		{
			var result = new List<PositionPoint>();
			var figure = figurePoint.Figure;

			if (figure == null)
				return result;

			if (figure.IsQueen)
			{
				// Queens can move long range in all 4 directions
				foreach (var (dy, dx) in Directions)
				{
					TryAddQueenMovesInDirection(figurePoint, board, result, attacks: null, dy, dx);
				}
			}
			else
			{
				// Regular pieces can only move forward diagonally (2 directions)
				int forwardDirection = figure.IsBlack ? -1 : 1;
				TryAddSimpleMoveInDirection(figurePoint, board, result, forwardDirection, -1); // Forward-Left
				TryAddSimpleMoveInDirection(figurePoint, board, result, forwardDirection, 1);  // Forward-Right
			}

			return result;
		}

		private static void TryAddSimpleMoveInDirection(
			PositionPoint point,
			PositionPoint[,] board,
			List<PositionPoint> list,
			int dy,
			int dx)
		{
			int newX = point.X + dx;
			int newY = point.Y + dy;

			if (!IsInBounds(newX, newY))
				return;

			var targetPosition = board[newY, newX];

			if (targetPosition.Figure == null)
				list.Add(targetPosition);
		}

		/// <summary>
		/// Scans a diagonal direction from the queen's position until hitting the board edge or a piece.
		/// Collects all valid simple moves (empty squares) and attack moves (landing squares after jumping an enemy).
		/// </summary>
		/// <param name="point">The starting position of the queen</param>
		/// <param name="board">The game board state</param>
		/// <param name="simpleMoves">Collection to populate with simple move destinations (can be null if not needed)</param>
		/// <param name="attacks">Collection to populate with attack move destinations (can be null if not needed)</param>
		/// <param name="dy">Vertical direction: -1 (down) or 1 (up)</param>
		/// <param name="dx">Horizontal direction: -1 (left) or 1 (right)</param>
		private static void TryAddQueenMovesInDirection(
			PositionPoint point,
			PositionPoint[,] board,
			List<PositionPoint> simpleMoves,
			Dictionary<PositionPoint, AttackData> attacks,
			int dy,
			int dx)
		{
			int distance = 1;
			PositionPoint enemyPosition = null;
			bool hasEncounteredEnemy = false;

			while (true)
			{
				int scanX = point.X + (dx * distance);
				int scanY = point.Y + (dy * distance);

				// Stop if we've reached the board edge
				if (!IsInBounds(scanX, scanY))
					break;

				var scannedPosition = board[scanY, scanX];
				var scannedFigure = scannedPosition.Figure;

				// Found a piece on this square
				if (scannedFigure != null)
				{
					// Check if it's an enemy and we haven't already passed an enemy
					bool isEnemy = scannedFigure.IsBlack != point.Figure.IsBlack;

					if (isEnemy && !hasEncounteredEnemy)
					{
						// Mark this enemy for potential capture
						enemyPosition = scannedPosition;
						hasEncounteredEnemy = true;
						distance++;
						continue;
					}

					// Hit a friendly piece OR a second enemy - stop scanning this direction
					break;
				}

				// Found an empty square
				if (hasEncounteredEnemy)
				{
					// This is a valid landing square after jumping the enemy
					if (attacks != null)
					{
						attacks[scannedPosition] = new AttackData
						{
							StartPosition = point,
							AttackPosition = enemyPosition,
							FinalPosition = scannedPosition
						};
					}
				}
				else
				{
					// This is a valid simple move (no enemy encountered yet)
					simpleMoves?.Add(scannedPosition);
				}

				distance++;
			}
		}

		private static void TryAddAttackInDirection(
			PositionPoint point,
			PositionPoint[,] board,
			Dictionary<PositionPoint, AttackData> dict,
			int dy,
			int dx)
		{
			int adjacentX = point.X + dx;
			int adjacentY = point.Y + dy;
			int landingX = point.X + 2 * dx;
			int landingY = point.Y + 2 * dy;

			// Check if adjacent position is within bounds
			if (!IsInBounds(adjacentX, adjacentY))
				return;

			// Check if landing position is within bounds
			if (!IsInBounds(landingX, landingY))
				return;

			var adjacentPosition = board[adjacentY, adjacentX];

			// Adjacent position must have an enemy figure
			if (adjacentPosition.Figure == null)
				return;

			if (adjacentPosition.Figure.IsBlack == point.Figure.IsBlack)
				return;

			var landingPosition = board[landingY, landingX];

			// Landing position must be empty
			if (landingPosition.Figure != null)
				return;

			dict[landingPosition] = new AttackData
			{
				AttackPosition = adjacentPosition,
				FinalPosition = landingPosition,
				StartPosition = point
			};
		}

		private static void TryAddQueenAttacksInDirection(
			PositionPoint point,
			PositionPoint[,] board,
			Dictionary<PositionPoint, AttackData> dict,
			int dy,
			int dx)
		{
			// Use unified method for attacks only (pass null for simple moves)
			TryAddQueenMovesInDirection(point, board, simpleMoves: null, attacks: dict, dy, dx);
		}

		private static bool IsInBounds(int x, int y)
		{
			return x >= 0 && x < Board.BoardSize && y >= 0 && y < Board.BoardSize;
		}

		public static GameState CheckGameState(PositionPoint[,] board, List<PositionPoint> points)
		{
			var whiteFigures = points.Where(p => p.Figure != null && !p.Figure.IsBlack).ToList();
			var blackFigures = points.Where(p => p.Figure != null && p.Figure.IsBlack).ToList();

			// Check if one side has no pieces left
			Debug.Log($"White: {whiteFigures.Count}, Black: {blackFigures.Count}");
			if (whiteFigures.Count == 0)
				return GameState.OpponentWin;

			if (blackFigures.Count == 0)
				return GameState.PlayerWin;

			// Check if white (player) has any valid moves
			bool playerHasMoves = HasAnyValidMoves(board, whiteFigures);
			bool opponentHasMoves = HasAnyValidMoves(board, blackFigures);

			// If player has no moves, opponent wins
			if (!playerHasMoves)
				return GameState.OpponentWin;

			// If opponent has no moves, player wins
			if (!opponentHasMoves)
				return GameState.PlayerWin;

			// If both have moves, game continues
			return GameState.Playing;
		}

		private static bool HasAnyValidMoves(PositionPoint[,] board, List<PositionPoint> figures)
		{
			foreach (var figurePoint in figures)
			{
				// Check if figure has any attack moves
				var attacks = GetAvailableAttacksDictionary(board, figurePoint);
				if (attacks.Count > 0)
				{
					Debug.Log($"Has attacks");
					return true;
				}

				// Check if figure has any simple moves
				var moves = GetAvailableSimpleMoves(board, figurePoint);
				if (moves.Count > 0)
				{
					Debug.Log($"Has moves");
					return true;
				}
			}

			return false;
		}
	}

	public class AttackData
	{
		public PositionPoint StartPosition { get; set; }
		public PositionPoint AttackPosition { get; set; }
		public PositionPoint FinalPosition { get; set; }
	}
}