using System.Collections.Generic;
using System.Linq;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

namespace Controllers.AI
{
	/// <summary>
	/// Hard difficulty AI that uses advanced evaluation and look-ahead
	/// - Evaluates board position value
	/// - Looks ahead to predict opponent responses
	/// - Maximizes material advantage and positional strength
	/// - Always makes optimal or near-optimal moves
	/// </summary>
	public class HardBotController : BaseBotController
	{
		private readonly bool _isBlack;
		private const int LookAheadDepth = 2; // Number of moves to look ahead

		public HardBotController(bool isBlack, BoardController boardController)
			: base(boardController)
		{
			_isBlack = isBlack;
		}

		protected override async UniTask<bool> MakeAttackAsync(int[,] currentBoardState)
		{
			List<ScoredMove> attackMoves = GetAllAttackMoves(currentBoardState);

			if (attackMoves.Count == 0)
				return false;

			await UniTask.Delay(1000);

			// Use minimax to evaluate attacks
			await UniTask.RunOnThreadPool(() =>
			{
				foreach (var move in attackMoves)
				{
					int[,] tempBoard = SimulateMove(currentBoardState, move);
					move.Score = EvaluateAttackMove(tempBoard, move) + 
					             MiniMax(tempBoard, LookAheadDepth - 1, false, float.MinValue, float.MaxValue);
				}
			});

			await UniTask.SwitchToMainThread();

			// Always choose the best move (hard AI is deterministic)
			var selectedMove = attackMoves.OrderByDescending(m => m.Score).First();

			await MakeBoardActionAsync(selectedMove);
			await HandleMultiJumpAsync(currentBoardState, selectedMove.To);

			return true;
		}

		protected override async UniTask MakeMoveAsync(int[,] currentBoardState)
		{
			List<ScoredMove> simpleMoves = GetAllSimpleMoves(currentBoardState);

			if (simpleMoves.Count == 0)
			{
				Debug.LogWarning($"{(_isBlack ? "Black" : "White")} AI has no available moves!");
				return;
			}

			await UniTask.Delay(1000);

			// Use minimax to evaluate moves
			await UniTask.RunOnThreadPool(() =>
			{
				foreach (var move in simpleMoves)
				{
					int[,] tempBoard = SimulateMove(currentBoardState, move);
					move.Score = EvaluateSimpleMove(tempBoard, move) + 
					             MiniMax(tempBoard, LookAheadDepth - 1, false, float.MinValue, float.MaxValue);
				}
			});

			await UniTask.SwitchToMainThread();

			// Always choose the best move
			var selectedMove = simpleMoves.OrderByDescending(m => m.Score).First();

			await MakeBoardActionAsync(selectedMove);
		}

		/// <summary>
		/// Minimax algorithm with alpha-beta pruning for move evaluation
		/// </summary>
		private float MiniMax(int[,] board, int depth, bool isMaximizing, float alpha, float beta)
		{
			if (depth == 0)
				return EvaluateBoardPosition(board);

			if (isMaximizing)
			{
				float maxEval = float.MinValue;
				var moves = GetAllPossibleMoves(board, _isBlack);

				foreach (var move in moves)
				{
					int[,] tempBoard = SimulateMove(board, move);
					float eval = MiniMax(tempBoard, depth - 1, false, alpha, beta);
					maxEval = Mathf.Max(maxEval, eval);
					alpha = Mathf.Max(alpha, eval);
					if (beta <= alpha)
						break; // Beta cutoff
				}

				return maxEval;
			}
			else
			{
				float minEval = float.MaxValue;
				var moves = GetAllPossibleMoves(board, !_isBlack);

				foreach (var move in moves)
				{
					int[,] tempBoard = SimulateMove(board, move);
					float eval = MiniMax(tempBoard, depth - 1, true, alpha, beta);
					minEval = Mathf.Min(minEval, eval);
					beta = Mathf.Min(beta, eval);
					if (beta <= alpha)
						break; // Alpha cutoff
				}

				return minEval;
			}
		}

		/// <summary>
		/// Simulate a move on a copy of the board
		/// </summary>
		private int[,] SimulateMove(int[,] board, ScoredMove move)
		{
			int[,] tempBoard = (int[,])board.Clone();
			int piece = tempBoard[move.From.y, move.From.x];

			// Apply move
			tempBoard[move.To.y, move.To.x] = piece;
			tempBoard[move.From.y, move.From.x] = 0;

			// Remove victim if attack
			if (move.IsAttack)
				tempBoard[move.VictimPosition.y, move.VictimPosition.x] = 0;

			// Check for promotion
			if (WillBecomeQueen(move.To, piece))
			{
				// Convert to queen (1 -> 3 for white, 2 -> 4 for black)
				tempBoard[move.To.y, move.To.x] = piece + 2;
			}

			return tempBoard;
		}

		/// <summary>
		/// Comprehensive board evaluation function
		/// </summary>
		private float EvaluateBoardPosition(int[,] board)
		{
			float score = 0f;

			for (int y = 0; y < BoardController.BoardSize; y++)
			{
				for (int x = 0; x < BoardController.BoardSize; x++)
				{
					int piece = board[y, x];
					if (piece == 0)
						continue;

					bool isPieceBlack = piece % 2 == 0;
					float pieceValue = GetPieceValue(piece, new Vector2Int(x, y));

					// Add to score if it's our piece, subtract if opponent's
					if (isPieceBlack == _isBlack)
						score += pieceValue;
					else
						score -= pieceValue;
				}
			}

			return score;
		}

		private float GetPieceValue(int piece, Vector2Int position)
		{
			float value = 0f;

			// Base piece values
			if (piece > 2)
				value = 30f; // Queen
			else
				value = 10f; // Regular piece

			// Positional bonuses
			value += GetPositionalBonus(position, piece);

			return value;
		}

		private float GetPositionalBonus(Vector2Int position, int piece)
		{
			float bonus = 0f;

			// Center control bonus
			float distanceFromCenterX = Mathf.Abs(position.x - 3.5f);
			float distanceFromCenterY = Mathf.Abs(position.y - 3.5f);
			float centrality = (7f - (distanceFromCenterX + distanceFromCenterY)) / 2f;
			bonus += centrality * 0.5f;

			// Advancement bonus for regular pieces
			if (piece <= 2)
			{
				bool isPieceBlack = piece % 2 == 0;
				int advancement = isPieceBlack ? (7 - position.y) : position.y;
				bonus += advancement * 0.8f;
			}

			// Back row bonus for defense
			if (position.y == 0 || position.y == 7)
				bonus += 1f;

			return bonus;
		}

		private float EvaluateAttackMove(int[,] board, ScoredMove move)
		{
			float score = 30f; // Base score for attacks

			// Get victim piece value
			int victimPiece = board[move.VictimPosition.y, move.VictimPosition.x];
			
			// Capturing a queen is extremely valuable
			if (victimPiece > 2)
				score += 50f;
			else
				score += 20f;

			// Promotion bonus
			int movingPiece = board[move.From.y, move.From.x];
			if (WillBecomeQueen(move.To, movingPiece))
				score += 25f;

			// Position quality
			score += GetPositionalBonus(move.To, movingPiece);

			// Safety check
			if (IsPositionSafe(board, move.To, move.From))
				score += 10f;
			else
				score -= 5f;

			return score;
		}

		private float EvaluateSimpleMove(int[,] board, ScoredMove move)
		{
			float score = 0f;

			int movingPiece = board[move.From.y, move.From.x];

			// Promotion is highly valuable
			if (WillBecomeQueen(move.To, movingPiece))
				score += 40f;

			// Advancement towards promotion
			int forwardDirection = _isBlack ? -1 : 1;
			int advancement = (move.To.y - move.From.y) * forwardDirection;
			score += advancement * 4f;

			// Positional value
			score += GetPositionalBonus(move.To, movingPiece);

			// Strong penalty for unsafe moves
			if (!IsPositionSafe(board, move.To, move.From))
				score -= 15f;

			// Queen mobility bonus
			if (movingPiece > 2)
			{
				int moveDistance = Mathf.Abs(move.To.x - move.From.x);
				score += moveDistance * 0.5f;
			}

			return score;
		}

		private bool WillBecomeQueen(Vector2Int position, int piece)
		{
			if (piece == 1 && position.y == 7)
				return true;
			if (piece == 2 && position.y == 0)
				return true;
			return false;
		}

		private bool IsPositionSafe(int[,] board, Vector2Int targetPos, Vector2Int fromPos)
		{
			int[,] tempBoard = (int[,])board.Clone();
			int piece = tempBoard[fromPos.y, fromPos.x];
			tempBoard[targetPos.y, targetPos.x] = piece;
			tempBoard[fromPos.y, fromPos.x] = 0;

			// Check if opponent can attack this position
			for (int y = 0; y < BoardController.BoardSize; y++)
			{
				for (int x = 0; x < BoardController.BoardSize; x++)
				{
					int enemyPiece = tempBoard[y, x];
					if (enemyPiece == 0)
						continue;

					bool isEnemyBlack = enemyPiece % 2 == 0;
					if (isEnemyBlack == _isBlack)
						continue;

					var attacks = CheckersBasics.GetAvailableAttacksForFigure(tempBoard, new Vector2Int(x, y));
					if (attacks.ContainsKey(targetPos))
						return false;
				}
			}

			return true;
		}

		private List<ScoredMove> GetAllPossibleMoves(int[,] board, bool isBlack)
		{
			List<ScoredMove> allMoves = new List<ScoredMove>();

			// First check for attacks (mandatory in checkers)
			var attacks = GetAllAttackMovesForColor(board, isBlack);
			if (attacks.Count > 0)
				return attacks;

			// If no attacks, return simple moves
			return GetAllSimpleMovesForColor(board, isBlack);
		}

		private List<ScoredMove> GetAllAttackMoves(int[,] board)
		{
			return GetAllAttackMovesForColor(board, _isBlack);
		}

		private List<ScoredMove> GetAllAttackMovesForColor(int[,] board, bool isBlack)
		{
			List<ScoredMove> attackMoves = new List<ScoredMove>();

			for (int y = 0; y < BoardController.BoardSize; y++)
			{
				for (int x = 0; x < BoardController.BoardSize; x++)
				{
					if (board[y, x] == 0)
						continue;

					Vector2Int position = new Vector2Int(x, y);
					if (!IsOwnFigureAtPosition(position, board, isBlack))
						continue;

					var attacks = CheckersBasics.GetAvailableAttacksForFigure(board, position);

					foreach (var attackData in attacks.Values)
					{
						attackMoves.Add(new ScoredMove
						{
							From = attackData.StartPosition,
							To = attackData.FinalPosition,
							VictimPosition = attackData.VictimPosition,
							IsAttack = true,
							Score = 0
						});
					}
				}
			}

			return attackMoves;
		}

		private List<ScoredMove> GetAllSimpleMoves(int[,] board)
		{
			return GetAllSimpleMovesForColor(board, _isBlack);
		}

		private List<ScoredMove> GetAllSimpleMovesForColor(int[,] board, bool isBlack)
		{
			List<ScoredMove> simpleMoves = new List<ScoredMove>();

			for (int y = 0; y < BoardController.BoardSize; y++)
			{
				for (int x = 0; x < BoardController.BoardSize; x++)
				{
					if (board[y, x] == 0)
						continue;

					if (!IsOwnFigureAtPosition(new Vector2Int(x, y), board, isBlack))
						continue;

					Vector2Int position = new Vector2Int(x, y);
					var moves = CheckersBasics.GetAvailableSimpleMovesForFigure(board, position);

					foreach (var targetPos in moves)
					{
						simpleMoves.Add(new ScoredMove
						{
							From = position,
							To = targetPos,
							IsAttack = false,
							Score = 0
						});
					}
				}
			}

			return simpleMoves;
		}

		private bool IsOwnFigureAtPosition(Vector2Int pos, int[,] board, bool isBlack)
		{
			if (board[pos.y, pos.x] == 0)
				return false;

			bool isBlackFigure = board[pos.y, pos.x] % 2 == 0;
			return isBlackFigure == isBlack;
		}

		private async UniTask HandleMultiJumpAsync(int[,] currentBoard, Vector2Int lastAttackPosition)
		{
			while (true)
			{
				var updatedBoard = BoardController.CurrentBoard;
				var continuedAttacks = CheckersBasics.GetAvailableAttacksForFigure(updatedBoard, lastAttackPosition);

				if (continuedAttacks.Count == 0)
					break;

				await UniTask.Delay(1000);

				// Evaluate continuation attacks with look-ahead
				List<ScoredMove> continuationMoves = new List<ScoredMove>();
				
				await UniTask.RunOnThreadPool(() =>
				{
					foreach (var attackData in continuedAttacks.Values)
					{
						var move = new ScoredMove
						{
							From = attackData.StartPosition,
							To = attackData.FinalPosition,
							VictimPosition = attackData.VictimPosition,
							IsAttack = true
						};
						
						int[,] tempBoard = SimulateMove(updatedBoard, move);
						move.Score = EvaluateAttackMove(tempBoard, move);
						continuationMoves.Add(move);
					}
				});

				await UniTask.SwitchToMainThread();

				var bestContinuation = continuationMoves.OrderByDescending(m => m.Score).First();
				await MakeBoardActionAsync(bestContinuation);

				lastAttackPosition = bestContinuation.To;
			}
		}
	}
}
