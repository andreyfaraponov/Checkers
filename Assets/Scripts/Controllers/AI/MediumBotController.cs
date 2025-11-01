using System.Collections.Generic;
using System.Linq;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

namespace Controllers.AI
{
	/// <summary>
	/// Medium difficulty AI that uses basic evaluation to make tactical decisions
	/// - Prioritizes attacks that capture queens
	/// - Prefers advancing pieces towards promotion
	/// - Makes tactical decisions but has some randomness
	/// </summary>
	public class MediumBotController : BaseBotController
	{
		private readonly bool _isBlack;

		public MediumBotController(bool isBlack, BoardController boardController)
			: base(boardController)
		{
			_isBlack = isBlack;
		}

		protected override async UniTask<bool> MakeAttackAsync(int[,] currentBoardState)
		{
			List<ScoredMove> attackMoves = GetAllAttackMoves(currentBoardState);

			if (attackMoves.Count == 0)
				return false;

			await UniTask.Delay(700);

			// Score each attack
			foreach (var move in attackMoves)
			{
				move.Score = EvaluateAttackMove(currentBoardState, move);
			}

			// Choose best attack with slight randomness (80% best, 20% random good move)
			var selectedMove = Random.value < 0.8f
				? attackMoves.OrderByDescending(m => m.Score).First()
				: attackMoves.OrderByDescending(m => m.Score).Take(3).ElementAt(Random.Range(0, Mathf.Min(3, attackMoves.Count)));

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

			await UniTask.Delay(700);

			// Score each move
			foreach (var move in simpleMoves)
			{
				move.Score = EvaluateSimpleMove(currentBoardState, move);
			}

			// Choose best move with slight randomness (70% best, 30% random good move)
			var selectedMove = Random.value < 0.7f
				? simpleMoves.OrderByDescending(m => m.Score).First()
				: simpleMoves.OrderByDescending(m => m.Score).Take(4).ElementAt(Random.Range(0, Mathf.Min(4, simpleMoves.Count)));

			await MakeBoardActionAsync(selectedMove);
		}

		private float EvaluateAttackMove(int[,] board, ScoredMove move)
		{
			float score = 10f; // Base score for any attack

			// Get victim piece value
			int victimPiece = board[move.VictimPosition.y, move.VictimPosition.x];
			
			// Capturing a queen is highly valuable
			if (victimPiece > 2)
				score += 20f;
			else
				score += 10f;

			// Check if this move creates a queen
			int movingPiece = board[move.From.y, move.From.x];
			bool becomesQueen = WillBecomeQueen(move.To, movingPiece);
			if (becomesQueen)
				score += 15f;

			// Prefer central positions
			score += GetCentralityBonus(move.To);

			// Check if move is safe (not immediately capturable)
			if (IsPositionSafe(board, move.To, move.From))
				score += 5f;
			else
				score -= 3f;

			return score;
		}

		private float EvaluateSimpleMove(int[,] board, ScoredMove move)
		{
			float score = 0f;

			int movingPiece = board[move.From.y, move.From.x];

			// Strongly favor moves that create a queen
			if (WillBecomeQueen(move.To, movingPiece))
				score += 30f;

			// Favor advancing pieces (moving towards opponent's side)
			int forwardDirection = _isBlack ? -1 : 1;
			int advancement = (move.To.y - move.From.y) * forwardDirection;
			score += advancement * 3f;

			// Favor central positions
			score += GetCentralityBonus(move.To);

			// Penalize moves that expose the piece to capture
			if (!IsPositionSafe(board, move.To, move.From))
				score -= 8f;

			// Bonus for queen mobility
			if (movingPiece > 2)
				score += 2f;

			return score;
		}

		private bool WillBecomeQueen(Vector2Int position, int piece)
		{
			// White pieces (1) become queens on row 7, Black pieces (2) become queens on row 0
			if (piece == 1 && position.y == 7)
				return true;
			if (piece == 2 && position.y == 0)
				return true;
			return false;
		}

		private float GetCentralityBonus(Vector2Int position)
		{
			// Favor positions closer to the center
			float distanceFromCenterX = Mathf.Abs(position.x - 3.5f);
			float distanceFromCenterY = Mathf.Abs(position.y - 3.5f);
			float centrality = 3.5f - (distanceFromCenterX + distanceFromCenterY) / 2f;
			return centrality;
		}

		private bool IsPositionSafe(int[,] board, Vector2Int targetPos, Vector2Int fromPos)
		{
			// Create a temporary board state with the move applied
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

		private List<ScoredMove> GetAllAttackMoves(int[,] board)
		{
			List<ScoredMove> attackMoves = new List<ScoredMove>();

			for (int y = 0; y < BoardController.BoardSize; y++)
			{
				for (int x = 0; x < BoardController.BoardSize; x++)
				{
					if (board[y, x] == 0)
						continue;

					Vector2Int position = new Vector2Int(x, y);
					if (!IsOwnFigureAtPosition(position, board))
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
			List<ScoredMove> simpleMoves = new List<ScoredMove>();

			for (int y = 0; y < BoardController.BoardSize; y++)
			{
				for (int x = 0; x < BoardController.BoardSize; x++)
				{
					if (board[y, x] == 0)
						continue;

					if (!IsOwnFigureAtPosition(new Vector2Int(x, y), board))
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

		private bool IsOwnFigureAtPosition(Vector2Int pos, int[,] board)
		{
			if (board[pos.y, pos.x] == 0)
				return false;

			bool isBlackFigure = board[pos.y, pos.x] % 2 == 0;
			return isBlackFigure == _isBlack;
		}

		private async UniTask HandleMultiJumpAsync(int[,] currentBoard, Vector2Int lastAttackPosition)
		{
			while (true)
			{
				var updatedBoard = BoardController.CurrentBoard;
				var continuedAttacks = CheckersBasics.GetAvailableAttacksForFigure(updatedBoard, lastAttackPosition);

				if (continuedAttacks.Count == 0)
					break;

				await UniTask.Delay(700);

				// Score continuation attacks and choose the best
				List<ScoredMove> continuationMoves = new List<ScoredMove>();
				foreach (var attackData in continuedAttacks.Values)
				{
					var move = new ScoredMove
					{
						From = attackData.StartPosition,
						To = attackData.FinalPosition,
						VictimPosition = attackData.VictimPosition,
						IsAttack = true,
						Score = EvaluateAttackMove(updatedBoard, new ScoredMove
						{
							From = attackData.StartPosition,
							To = attackData.FinalPosition,
							VictimPosition = attackData.VictimPosition,
							IsAttack = true
						})
					};
					continuationMoves.Add(move);
				}

				var bestContinuation = continuationMoves.OrderByDescending(m => m.Score).First();
				await MakeBoardActionAsync(bestContinuation);

				lastAttackPosition = bestContinuation.To;
			}
		}
	}
}
