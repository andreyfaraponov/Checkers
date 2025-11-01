using System.Collections.Generic;
using Core;
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
			(1, -1), // Left-Up
			(1, 1), // Right-Up
			(-1, 1), // Right-Down
			(-1, -1) // Left-Down
		};

		public static Dictionary<Vector2Int, AttackData> GetAvailableAttacksForFigure(
			int[,] board,
			Vector2Int startPosition)
		{
			var result = new Dictionary<Vector2Int, AttackData>();
			var figure = board[startPosition.y, startPosition.x];

			if (figure == 0)
				return result;

			// Both queens and regular pieces can attack in all 4 diagonal directions
			if (figure > 2)
			{
				// Queens can attack long range in all 4 directions
				foreach (var (dy, dx) in Directions)
				{
					TryAddQueenAttacksInDirection(startPosition, board, result,
						dy, dx);
				}
			}
			else
			{
				// Regular pieces can attack in all 4 diagonal directions (one square jump)
				foreach (var (dy, dx) in Directions)
				{
					TryAddAttackInDirection(startPosition, board, result,
						dy, dx);
				}
			}

			return result;
		}

		public static List<Vector2Int> GetAvailableSimpleMovesForFigure(
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
					result.AddRange(GetQueenSimpleMovesInDirection(figurePoint, board,
						dy, dx));
				}
			}
			else
			{
				int forwardDirection = figure % 2 == 0 ? -1 : 1;
				TryAddSimpleMoveInDirection(figurePoint, board, result,
					forwardDirection, -1); // Forward-Left
				TryAddSimpleMoveInDirection(figurePoint, board, result,
					forwardDirection, 1); // Forward-Right
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
				list.Add(new Vector2Int(newX, newY));
		}

		private static List<Vector2Int> GetQueenSimpleMovesInDirection(
			Vector2Int point,
			int[,] board,
			int dy,
			int dx)
		{
			int distance = 1;
			List<Vector2Int> result = new List<Vector2Int>();

			while (true)
			{
				int scanX = point.x + (dx * distance);
				int scanY = point.y + (dy * distance);

				// Stop if we've reached the board edge
				if (!IsInBounds(scanX, scanY))
					break;

				var figure = board[scanY, scanX];

				// Found a piece on this square
				if (figure != 0)
				{
					break;
				}

				result.Add(new Vector2Int(scanX, scanY));
				distance++;
			}
			
			return result;
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

			if ((adjacentPosition % 2 == 0) == (pointPosition % 2 == 0))
				return;

			var landingPosition = board[landingY, landingX];

			// Landing position must be empty
			if (landingPosition != 0)
				return;

			dict[new Vector2Int(landingX, landingY)] = new AttackData
			{
				VictimPosition = new Vector2Int(adjacentX, adjacentY),
				FinalPosition = new Vector2Int(landingX, landingY),
				StartPosition = point
			};
		}

		private static void TryAddQueenAttacksInDirection(
			Vector2Int point,
			int[,] board,
			Dictionary<Vector2Int, AttackData> attacks,
			int dy,
			int dx)
		{
			int distance = 1;
			Vector2Int enemyPosition = Vector2Int.one * -1;
			bool hasEncounteredEnemy = false;
			var pointFigure = board[point.y, point.x];

			while (true)
			{
				int scanX = point.x + (dx * distance);
				int scanY = point.y + (dy * distance);

				// Stop if we've reached the board edge
				if (!IsInBounds(scanX, scanY))
					break;

				var figure = board[scanY, scanX];

				// Found a piece on this square
				if (figure != 0)
				{
					// Check if it's an enemy and we haven't already passed an enemy
					bool isEnemy = (figure % 2 == 0) != (pointFigure % 2 == 0);

					if (isEnemy && !hasEncounteredEnemy)
					{
						// Mark this enemy for potential capture
						enemyPosition = new Vector2Int(scanX, scanY);
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
						attacks[new Vector2Int(scanX, scanY)] = new AttackData
						{
							StartPosition = point,
							VictimPosition = enemyPosition,
							FinalPosition = new Vector2Int(scanX, scanY)
						};
					}
				}

				distance++;
			}
		}

		private static bool IsInBounds(int x, int y)
		{
			return x is >= 0 and < BoardController.BoardSize &&
				   y is >= 0 and < BoardController.BoardSize;
		}

		public static GameState CheckGameState(int[,] board, bool isBlackTurn)
		{
			bool hasWhitePieces = false;
			bool hasBlackPieces = false;

			// Count pieces for each side
			for (int y = 0; y < BoardController.BoardSize; y++)
			{
				for (int x = 0; x < BoardController.BoardSize; x++)
				{
					int piece = board[y, x];
					if (piece is 1 or 3) // White common (1) or white queen (3)
						hasWhitePieces = true;
					else if (piece is 2 or 4) // Black common (2) or black queen (4)
						hasBlackPieces = true;

					// Early exit if both colors found
					if (hasWhitePieces && hasBlackPieces)
						break;
				}

				if (hasWhitePieces && hasBlackPieces)
					break;
			}

			// Check if either side has no pieces left
			if (!hasWhitePieces)
				return GameState.OpponentWin; // Black (opponent) wins

			if (!hasBlackPieces)
				return GameState.PlayerWin; // White (player) wins

			// Both sides have pieces, check if they have valid moves
			bool whiteHasMoves = HasAnyValidMovesForColor(board, isBlack: false);
			bool blackHasMoves = HasAnyValidMovesForColor(board, isBlack: true);

			// If neither side has moves, it's a draw (stalemate)
			if (!whiteHasMoves && !blackHasMoves)
				return GameState.Draw;

			// If white has no moves, black wins
			if (!whiteHasMoves && !isBlackTurn)
				return GameState.OpponentWin;

			// If black has no moves, white wins
			if (!blackHasMoves && isBlackTurn)
				return GameState.PlayerWin;

			// Both sides have pieces and moves, game continues
			return GameState.Playing;
		}

		/// <summary>
		/// Check if a specific color has any valid moves (attacks or simple moves)
		/// </summary>
		/// <param name="board">The game board state</param>
		/// <param name="isBlack">True to check black pieces (2, 4), false to check white pieces (1, 3)</param>
		/// <returns>True if the color has at least one valid move</returns>
		private static bool HasAnyValidMovesForColor(int[,] board, bool isBlack)
		{
			for (int y = 0; y < BoardController.BoardSize; y++)
			{
				for (int x = 0; x < BoardController.BoardSize; x++)
				{
					int piece = board[y, x];

					// Skip empty squares
					if (piece == 0)
						continue;

					// Check if piece belongs to the color we're checking
					// Black pieces: 2 (common) and 4 (queen)
					// White pieces: 1 (common) and 3 (queen)
					bool isBlackPiece = (piece == 2 || piece == 4);

					if (isBlackPiece != isBlack)
						continue;

					Vector2Int position = new Vector2Int(x, y);

					// Check if this piece has any attack moves
					var attacks = GetAvailableAttacksForFigure(board, position);
					if (attacks.Count > 0)
						return true;

					// Check if this piece has any simple moves
					var moves = GetAvailableSimpleMovesForFigure(board, position);
					if (moves.Count > 0)
						return true;
				}
			}

			return false;
		}
	}

	public class AttackData
	{
		public Vector2Int StartPosition { get; set; }
		public Vector2Int VictimPosition { get; set; }
		public Vector2Int FinalPosition { get; set; }
	}
}