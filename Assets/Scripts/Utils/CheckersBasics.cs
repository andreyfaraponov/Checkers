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

		public static Dictionary<Vector2Int, AttackData> GetAvailableAttacksDictionary(
			int[,] board,
			Vector2Int figurePoint)
		{
			var result = new Dictionary<Vector2Int, AttackData>();
			var figure = board[figurePoint.y, figurePoint.x];

			if (figure == 0)
				return result;

			// Both queens and regular pieces can attack in all 4 diagonal directions
			if (figure > 2)
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

		public static List<Vector2Int> GetAvailableSimpleMoves(
			int[,] board,
			Vector2Int figurePoint)
		{
			var result = new List<Vector2Int>();
			var figure = board[figurePoint.y, figurePoint.x];

			if (figure == 0)
				return result;

			if (figure > 2)
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
				int forwardDirection = figure == 2 ? -1 : 1;
				TryAddSimpleMoveInDirection(figurePoint, board, result, forwardDirection, -1); // Forward-Left
				TryAddSimpleMoveInDirection(figurePoint, board, result, forwardDirection, 1);  // Forward-Right
			}

			return result;
		}

		private static void TryAddSimpleMoveInDirection(
			Vector2Int point,
			int[,] board,
			List<Vector2Int> list,
			int dy,
			int dx)
		{
			int newX = point.x + dx;
			int newY = point.y + dy;

			if (!IsInBounds(newX, newY))
				return;

			var targetPosition = board[newY, newX];

			if (targetPosition == 0)
				list.Add(new Vector2Int(newY, newX));
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
			Vector2Int point,
			int[,] board,
			List<Vector2Int> simpleMoves,
			Dictionary<Vector2Int, AttackData> attacks,
			int dy,
			int dx)
		{
			int distance = 1;
			Vector2Int enemyPosition = Vector2Int.one * -1;
			bool hasEncounteredEnemy = false;

			while (true)
			{
				int scanX = point.x + (dx * distance);
				int scanY = point.y + (dy * distance);

				// Stop if we've reached the board edge
				if (!IsInBounds(scanX, scanY))
					break;

				var figure = board[scanY, scanX];
				var pointFigure = board[point.y, point.x];
				
				
				var scannedFigure = board[scanY, scanX];

				// Found a piece on this square
				if (figure != 0)
				{
					// Check if it's an enemy and we haven't already passed an enemy
					bool isEnemy = figure % 2 > 0 != pointFigure % 2 > 0;

					if (isEnemy && !hasEncounteredEnemy)
					{
						// Mark this enemy for potential capture
						enemyPosition = new Vector2Int(scanY, scanX);
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
						attacks[new Vector2Int(scanY, scanX)] = new AttackData
						{
							StartPosition = point,
							AttackPosition = enemyPosition,
							FinalPosition = new Vector2Int(scanY, scanX)
						};
					}
				}
				else
				{
					// This is a valid simple move (no enemy encountered yet)
					simpleMoves?.Add(new Vector2Int(scanY, scanX));
				}

				distance++;
			}
		}

		private static void TryAddAttackInDirection(
			Vector2Int point,
			int[,] board,
			Dictionary<Vector2Int, AttackData> dict,
			int dy,
			int dx)
		{
			int adjacentX = point.x + dx;
			int adjacentY = point.y + dy;
			int landingX = point.x + 2 * dx;
			int landingY = point.y + 2 * dy;

			// Check if adjacent position is within bounds
			if (!IsInBounds(adjacentX, adjacentY))
				return;

			// Check if landing position is within bounds
			if (!IsInBounds(landingX, landingY))
				return;

			var adjacentPosition = board[adjacentY, adjacentX];
			var pointPosition = board[point.y, point.x];

			// Adjacent position must have an enemy figure
			if (adjacentPosition == 0)
				return;

			if (adjacentPosition % 2 == 0 && pointPosition % 2 == 0)
				return;

			var landingPosition = board[landingY, landingX];

			// Landing position must be empty
			if (landingPosition != 0)
				return;

			dict[new Vector2Int(landingY, landingY)] = new AttackData
			{
				AttackPosition = new Vector2Int(adjacentY, adjacentX),
				FinalPosition = new Vector2Int(landingY, landingX),
				StartPosition = point
			};
		}

		private static void TryAddQueenAttacksInDirection(
			Vector2Int point,
			int[,] board,
			Dictionary<Vector2Int, AttackData> dict,
			int dy,
			int dx)
		{
			// Use unified method for attacks only (pass null for simple moves)
			TryAddQueenMovesInDirection(point, board, simpleMoves: null, attacks: dict, dy, dx);
		}

		private static bool IsInBounds(int x, int y)
		{
			return x is >= 0 and < Board.BoardSize && y is >= 0 and < Board.BoardSize;
		}

		public static GameState CheckGameState(int[,] board, List<PositionPoint> points)
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

		private static bool HasAnyValidMoves(int[,] board, List<PositionPoint> figures)
		{
			foreach (var figurePoint in figures)
			{
				// Check if figure has any attack moves
				var attacks = GetAvailableAttacksDictionary(board, new Vector2Int(figurePoint.X, figurePoint.Y));
				if (attacks.Count > 0)
				{
					Debug.Log($"Has attacks");
					return true;
				}

				// Check if figure has any simple moves
				var moves = GetAvailableSimpleMoves(board, new Vector2Int(figurePoint.X, figurePoint.Y));
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
		public Vector2Int StartPosition { get; set; }
		public Vector2Int AttackPosition { get; set; }
		public Vector2Int FinalPosition { get; set; }
	}
}