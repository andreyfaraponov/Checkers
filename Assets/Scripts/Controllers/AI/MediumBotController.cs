using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Cysharp.Threading.Tasks;
using Gameplay;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Controllers.AI
{
	/// <summary>
	/// Medium difficulty AI that uses score-based move evaluation
	/// Considers positional advantage, advancement, and tactical safety
	/// </summary>
	public class MediumBotController : BaseBotController
	{
		// Scoring weights
		private const float ATTACK_MOVE_BASE_SCORE = 100f;
		private const float ADVANCEMENT_PER_ROW = 10f;
		private const float CENTER_CONTROL_BONUS = 5f;
		private const float EDGE_PENALTY = -5f;
		private const float QUEEN_BONUS = 20f;
		private const float VULNERABILITY_PENALTY = -30f;
		private const float RANDOMNESS_FACTOR = 0.1f; // ±10% randomness

		private readonly Dictionary<PositionPoint, List<PositionPoint>> _possibleMoves = new();

		public MediumBotController(PositionPoint[,] board, List<PositionPoint> points, Board boardReference = null) 
			: base(board, points, boardReference)
		{
		}

		protected override async UniTask MakeMove()
		{
			_possibleMoves.Clear();

			List<ScoredMove> scoredAttacks = EvaluateAttackMoves();
			List<ScoredMove> scoredSimpleMoves = EvaluateSimpleMoves();
			
			Debug.LogError($"Medium AI: {scoredAttacks.Count} attacks, {scoredSimpleMoves.Count} simple moves");

			// Attacks always take priority
			if (scoredAttacks.Count > 0)
			{
				ExecuteBestAttack(scoredAttacks);

				// Continue multi-jump if available
				while (_lastAttackFigure != null)
				{
					await UniTask.Delay(500);
					TryAttackOneMoreTime();
				}
			}
			else if (scoredSimpleMoves.Count > 0)
			{
				ExecuteBestSimpleMove(scoredSimpleMoves);
			}

			await UniTask.Delay(500);

			_currentTurnCompletionSource.TrySetResult();
		}

		/// <summary>
		/// Evaluate all possible attack moves and assign scores
		/// </summary>
		private List<ScoredMove> EvaluateAttackMoves()
		{
			List<ScoredMove> scoredMoves = new List<ScoredMove>();

			foreach (var point in _points.Where(p => p.Figure != null && p.Figure.IsBlack))
			{
				var attackActions = GetAttackActions(point);
				var attackData = GetAttackDataForPoint(point);

				foreach (var (action, data) in attackActions.Zip(attackData, (a, d) => (a, d)))
				{
					float score = EvaluateAttackMove(data.StartPosition, data.FinalPosition, point.Figure);
					scoredMoves.Add(new ScoredMove
					{
						Action = action,
						Score = score,
						From = data.StartPosition,
						To = data.FinalPosition
					});
				}
			}

			return scoredMoves;
		}

		/// <summary>
		/// Get attack data for a specific point
		/// </summary>
		private List<AttackData> GetAttackDataForPoint(PositionPoint point)
		{
			var figurePosition = _points.First(p => p.Figure == point.Figure);
			return CheckersBasics.GetAvailableAttacksDictionary(_board, figurePosition).Values.ToList();
		}

		/// <summary>
		/// Evaluate score for an attack move
		/// </summary>
		private float EvaluateAttackMove(PositionPoint from, PositionPoint to, Figure figure)
		{
			float score = ATTACK_MOVE_BASE_SCORE;

			// Evaluate the destination position
			score += EvaluatePosition(to, figure);

			// Check if move leaves piece vulnerable
			score += EvaluateVulnerability(to, figure);

			// Add small randomness
			score += Random.Range(-score * RANDOMNESS_FACTOR, score * RANDOMNESS_FACTOR);

			return score;
		}

		/// <summary>
		/// Evaluate all possible simple moves and assign scores
		/// </summary>
		private List<ScoredMove> EvaluateSimpleMoves()
		{
			List<ScoredMove> scoredMoves = new List<ScoredMove>();

			foreach (var point in _points)
			{
				if (point.Figure != null && point.Figure.IsBlack)
				{
					var moves = GetAvailableMoves(point.Figure);
					_possibleMoves[point] = moves;

					foreach (var targetPoint in moves)
					{
						float score = EvaluateSimpleMove(point, targetPoint, point.Figure);
						scoredMoves.Add(new ScoredMove
						{
							From = point,
							To = targetPoint,
							Score = score
						});
					}
				}
			}

			return scoredMoves;
		}

		/// <summary>
		/// Evaluate score for a simple (non-attack) move
		/// </summary>
		private float EvaluateSimpleMove(PositionPoint from, PositionPoint to, Figure figure)
		{
			float score = 0;

			// Evaluate the destination position
			score += EvaluatePosition(to, figure);

			// Check if move leaves piece vulnerable
			score += EvaluateVulnerability(to, figure);

			// Add small randomness
			score += Random.Range(-10f * RANDOMNESS_FACTOR, 10f * RANDOMNESS_FACTOR);

			return score;
		}

		/// <summary>
		/// Evaluate the positional value of a board position
		/// </summary>
		private float EvaluatePosition(PositionPoint position, Figure figure)
		{
			float score = 0;

			// Advancement bonus - black pieces move toward y=0
			int rowsFromStart = Board.BoardSize - 1 - position.Y;
			score += rowsFromStart * ADVANCEMENT_PER_ROW;

			// Center control bonus (middle 4x4 squares)
			if (IsInCenter(position))
			{
				score += CENTER_CONTROL_BONUS;
			}

			// Edge penalty
			if (IsOnEdge(position))
			{
				score += EDGE_PENALTY;
			}

			// Queen bonus
			if (figure.IsQueen)
			{
				score += QUEEN_BONUS;
			}

			return score;
		}

		/// <summary>
		/// Check if a position would leave the piece vulnerable to opponent attacks
		/// </summary>
		private float EvaluateVulnerability(PositionPoint position, Figure figure)
		{
			// Simulate the move and check if opponent can attack this position
			// Check all four diagonal directions for potential opponent attacks

			int x = position.X;
			int y = position.Y;

			// For black pieces, white opponent can attack from positions that would jump to (x, y)
			// White pieces move upward (increasing y), so they could attack from below

			int[,] attackDirections = { { -1, -1 }, { 1, -1 }, { -1, 1 }, { 1, 1 } };

			for (int i = 0; i < 4; i++)
			{
				int checkX = x + attackDirections[i, 0];
				int checkY = y + attackDirections[i, 1];
				int opponentX = x + attackDirections[i, 0] * 2;
				int opponentY = y + attackDirections[i, 1] * 2;

				// Check if there's an opponent piece that could attack
				if (IsValidPosition(opponentX, opponentY) && IsValidPosition(checkX, checkY))
				{
					var opponentPos = _board[opponentX, opponentY];
					if (opponentPos?.Figure != null && !opponentPos.Figure.IsBlack)
					{
						// Check if opponent can legally attack to this position
						var checkPos = _board[checkX, checkY];
						
						// For regular pieces, check movement direction
						if (!opponentPos.Figure.IsQueen)
						{
							// White pieces move up (y increases)
							if (opponentY < y) // Opponent is below, can move up to attack
							{
								if (checkPos?.Figure == null) // Middle position is empty (would be after our move)
								{
									return VULNERABILITY_PENALTY;
								}
							}
						}
						else
						{
							// Queens can attack from any diagonal
							if (checkPos?.Figure == null)
							{
								return VULNERABILITY_PENALTY;
							}
						}
					}
				}
			}

			return 0;
		}

		/// <summary>
		/// Check if position is in the center 4x4 area
		/// </summary>
		private bool IsInCenter(PositionPoint position)
		{
			return position.X >= 2 && position.X <= 5 && position.Y >= 2 && position.Y <= 5;
		}

		/// <summary>
		/// Check if position is on the edge of the board
		/// </summary>
		private bool IsOnEdge(PositionPoint position)
		{
			return position.X == 0 || position.X == Board.BoardSize - 1 ||
			       position.Y == 0 || position.Y == Board.BoardSize - 1;
		}

		/// <summary>
		/// Check if board coordinates are valid
		/// </summary>
		private bool IsValidPosition(int x, int y)
		{
			return x >= 0 && x < Board.BoardSize && y >= 0 && y < Board.BoardSize;
		}

		/// <summary>
		/// Execute the best attack from scored moves
		/// </summary>
		private void ExecuteBestAttack(List<ScoredMove> scoredMoves)
		{
			// Sort by score descending
			scoredMoves.Sort((a, b) => b.Score.CompareTo(a.Score));

			// Execute the highest scoring move
			var bestMove = scoredMoves[0];
			bestMove.Action?.Invoke();

			Debug.Log($"Medium AI Attack: Score={bestMove.Score:F1}, From=({bestMove.From.X},{bestMove.From.Y}), To=({bestMove.To.X},{bestMove.To.Y})");
		}

		/// <summary>
		/// Execute the best simple move from scored moves
		/// </summary>
		private void ExecuteBestSimpleMove(List<ScoredMove> scoredMoves)
		{
			// Sort by score descending
			scoredMoves.Sort((a, b) => b.Score.CompareTo(a.Score));

			// Execute the highest scoring move
			var bestMove = scoredMoves[0];
			ExecuteSimpleMove(bestMove.From, bestMove.To);

			Debug.Log($"Medium AI Move: Score={bestMove.Score:F1}, From=({bestMove.From.X},{bestMove.From.Y}), To=({bestMove.To.X},{bestMove.To.Y})");
		}

		/// <summary>
		/// Data structure to hold a move with its evaluation score
		/// </summary>
		private class ScoredMove
		{
			public Action Action;
			public PositionPoint From;
			public PositionPoint To;
			public float Score;
		}
	}
}
