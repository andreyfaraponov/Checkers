using System.Collections.Generic;
using System.Linq;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

namespace Controllers.AI
{
	/// <summary>
	/// Easy difficulty AI that makes random moves from available options
	/// </summary>
	public class EasyBotController : BaseBotController
	{
		private readonly Dictionary<Vector2Int, List<Vector2Int>> _possibleMoves = new();
		private readonly bool _isBlack;

		public EasyBotController(bool isBlack, BoardController boardControllerReference) 
			: base(boardControllerReference)
		{
			_isBlack = isBlack;
		}
		
		protected override async UniTask<bool> MakeAttackAsync(int[,] currentBoardState)
		{
			// Find all attack moves for the AI's color
			List<ScoredMove> attackMoves = GetAllAttackMoves(currentBoardState);

			if (attackMoves.Count == 0)
				return false;

			// Add delay for AI thinking time
			await UniTask.Delay(500);

			// Choose a random attack move
			var selectedMove = attackMoves[Random.Range(0, attackMoves.Count)];

			// Execute the attack
			await MakeBoardActionAsync(selectedMove);

			// Check for multi-jump - continue attacking with the same piece if possible
			await HandleMultiJumpAsync(currentBoardState, selectedMove.To);

			return true;
		}

		protected override async UniTask MakeMoveAsync(int[,] currentBoardState)
		{
			_possibleMoves.Clear();

			// Find all simple moves for the AI's color
			List<ScoredMove> simpleMoves = GetAllSimpleMoves(currentBoardState);

			if (simpleMoves.Count == 0)
			{
				Debug.LogWarning($"{(_isBlack ? "Black" : "White")} AI has no available moves!");
				return;
			}

			// Add delay for AI thinking time
			await UniTask.Delay(500);

			// Choose a random simple move
			var selectedMove = simpleMoves[Random.Range(0, simpleMoves.Count)];

			// Execute the move
			await MakeBoardActionAsync(selectedMove);
		}

		/// <summary>
		/// Get all available attack moves for the AI's color
		/// </summary>
		private List<ScoredMove> GetAllAttackMoves(int[,] board)
		{
			List<ScoredMove> attackMoves = new List<ScoredMove>();

			for (int y = 0; y < BoardController.BoardSize; y++)
			{
				for (int x = 0; x < BoardController.BoardSize; x++)
				{
					int piece = board[y, x];
					
					// Skip empty squares
					if (piece == 0)
						continue;

					// Check if piece belongs to the AI's color
					bool isPieceBlack = (piece == 2 || piece == 4);
					if (isPieceBlack != _isBlack)
						continue;

					Vector2Int position = new Vector2Int(x, y);
					var attacks = CheckersBasics.GetAvailableAttacksForFigure(board, position);

					foreach (var attackData in attacks.Values)
					{
						attackMoves.Add(new ScoredMove
						{
							From = attackData.StartPosition,
							To = attackData.FinalPosition,
							VictimPosition = attackData.AttackPosition,
							IsAttack = true,
							Score = 0 // Easy AI doesn't use scoring
						});
					}
				}
			}

			return attackMoves;
		}

		/// <summary>
		/// Get all available simple moves for the AI's color
		/// </summary>
		private List<ScoredMove> GetAllSimpleMoves(int[,] board)
		{
			List<ScoredMove> simpleMoves = new List<ScoredMove>();

			for (int y = 0; y < BoardController.BoardSize; y++)
			{
				for (int x = 0; x < BoardController.BoardSize; x++)
				{
					int piece = board[y, x];
					
					// Skip empty squares
					if (piece == 0)
						continue;

					// Check if piece belongs to the AI's color
					bool isPieceBlack = (piece == 2 || piece == 4);
					if (isPieceBlack != _isBlack)
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
							Score = 0 // Easy AI doesn't use scoring
						});
					}
				}
			}

			return simpleMoves;
		}

		/// <summary>
		/// Handle multi-jump attacks where the same piece can attack multiple times
		/// </summary>
		private async UniTask HandleMultiJumpAsync(int[,] currentBoard, Vector2Int lastAttackPosition)
		{
			while (true)
			{
				// Get the updated board state after the attack
				var updatedBoard = BoardController.CurrentBoard;
				
				// Get attacks available from the last attack position
				var continuedAttacks = CheckersBasics.GetAvailableAttacksForFigure(updatedBoard, lastAttackPosition);

				if (continuedAttacks.Count == 0)
					break; // No more attacks available

				await UniTask.Delay(500);

				// Choose a random continuation attack
				var attackData = continuedAttacks.Values.ElementAt(Random.Range(0, continuedAttacks.Count));

				var continuedMove = new ScoredMove
				{
					From = attackData.StartPosition,
					To = attackData.FinalPosition,
					VictimPosition = attackData.AttackPosition,
					IsAttack = true,
					Score = 0
				};

				// Execute the continued attack
				await MakeBoardActionAsync(continuedMove);

				// Update for next iteration
				lastAttackPosition = attackData.FinalPosition;
			}
		}
	}
}