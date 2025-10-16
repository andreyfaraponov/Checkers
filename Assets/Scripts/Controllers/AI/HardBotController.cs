using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Gameplay;
using Game.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Controllers.AI
{
	/// <summary>
	/// Hard difficulty AI using advanced evaluation without minimax simulations
	/// Uses sophisticated scoring with multi-factor analysis for strong tactical play
	/// </summary>
	public class HardBotController : BaseBotController
	{
		// Scoring weights - more refined than Medium
		private const float ATTACK_MOVE_BASE_SCORE = 200f;
		private const float PIECE_VALUE = 15f;
		private const float QUEEN_VALUE = 45f;
		private const float ADVANCEMENT_PER_ROW = 12f;
		private const float CENTER_CONTROL_BONUS = 8f;
		private const float EDGE_PENALTY = -8f;
		private const float QUEEN_BONUS = 25f;
		private const float VULNERABILITY_PENALTY = -50f;
		private const float MATERIAL_ADVANTAGE_BONUS = 10f;
		private const float KING_ROW_PROXIMITY = 15f;
		private const float PIECE_SAFETY_BONUS = 5f;
		private const float ATTACK_THREAT_BONUS = 30f;
		private const float RANDOMNESS_FACTOR = 0.05f; // ±5% randomness

		private readonly Dictionary<PositionPoint, List<PositionPoint>> _possibleMoves = new();

		public HardBotController(PositionPoint[,] board, List<PositionPoint> points) 
			: base(board, points)
		{
		}

		protected override async UniTask MakeMove()
		{
			_possibleMoves.Clear();

			List<ScoredMove> scoredAttacks = EvaluateAttackMoves();
			List<ScoredMove> scoredSimpleMoves = EvaluateSimpleMoves();

			await UniTask.Delay(300); // Slight delay for "thinking" effect

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
		/// Evaluate all possible attack moves with advanced scoring
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
					float score = EvaluateAttackMove(data.StartPosition, data.FinalPosition, data.AttackPosition, point.Figure);
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
		/// Evaluate score for an attack move with advanced analysis
		/// </summary>
		private float EvaluateAttackMove(PositionPoint from, PositionPoint to, PositionPoint captured, Figure figure)
		{
			float score = ATTACK_MOVE_BASE_SCORE;

			// Value of captured piece
			float capturedValue = captured.Figure.IsQueen ? QUEEN_VALUE : PIECE_VALUE;
			score += capturedValue;

			// Evaluate the destination position
			score += EvaluatePosition(to, figure);

			// Check if attack leads to promotion
			if (!figure.IsQueen && to.Y == 0)
			{
				score += KING_ROW_PROXIMITY * 2; // Double bonus for reaching king row
			}

			// Check if move leaves piece vulnerable
			score += EvaluateVulnerability(to, figure);

			// Check if attack creates additional attack opportunities
			score += EvaluateAttackThreat(to, figure);

			// Material advantage after this attack
			score += EvaluateMaterialAdvantage(capturedValue);

			// Add small randomness
			score += Random.Range(-score * RANDOMNESS_FACTOR, score * RANDOMNESS_FACTOR);

			return score;
		}

		/// <summary>
		/// Evaluate all possible simple moves with advanced scoring
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
		/// Evaluate score for a simple move with advanced analysis
		/// </summary>
		private float EvaluateSimpleMove(PositionPoint from, PositionPoint to, Figure figure)
		{
			float score = 0;

			// Evaluate the destination position
			score += EvaluatePosition(to, figure);

			// Check if move leads to promotion
			if (!figure.IsQueen && to.Y == 0)
			{
				score += KING_ROW_PROXIMITY * 3; // High bonus for reaching king row
			}
			// Proximity to king row
			else if (!figure.IsQueen)
			{
				int rowsToKing = to.Y;
				score += KING_ROW_PROXIMITY * (Board.BoardSize - rowsToKing) / (float)Board.BoardSize;
			}

			// Check if move leaves piece vulnerable
			score += EvaluateVulnerability(to, figure);

			// Check if move creates attack threats
			score += EvaluateAttackThreat(to, figure);

			// Check if piece is safe in current position
			score += EvaluatePieceSafety(from, to, figure);

			// Add small randomness
			score += Random.Range(-5f * RANDOMNESS_FACTOR, 5f * RANDOMNESS_FACTOR);

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
				// Extra bonus for center-center (most valuable squares)
				if (position.X >= 3 && position.X <= 4 && position.Y >= 3 && position.Y <= 4)
				{
					score += CENTER_CONTROL_BONUS * 0.5f;
				}
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
			int x = position.X;
			int y = position.Y;

			// Check all four diagonal directions for potential opponent attacks
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
						var checkPos = _board[checkX, checkY];
						
						// For regular pieces, check movement direction
						if (!opponentPos.Figure.IsQueen)
						{
							// White pieces move up (y increases)
							if (opponentY < y && checkPos?.Figure == null)
							{
								return VULNERABILITY_PENALTY;
							}
						}
						else
						{
							// Queens can attack from any diagonal
							if (checkPos?.Figure == null)
							{
								return VULNERABILITY_PENALTY * 1.5f; // Higher penalty for queen threats
							}
						}
					}
				}
			}

			return 0;
		}

		/// <summary>
		/// Evaluate if move creates attack threats against opponent
		/// </summary>
		private float EvaluateAttackThreat(PositionPoint position, Figure figure)
		{
			int x = position.X;
			int y = position.Y;
			float threatScore = 0;

			// Check if we can attack from this position
			int[,] attackDirections = { { -1, -1 }, { 1, -1 }, { -1, 1 }, { 1, 1 } };

			for (int i = 0; i < 4; i++)
			{
				int targetX = x + attackDirections[i, 0];
				int targetY = y + attackDirections[i, 1];
				int landingX = x + attackDirections[i, 0] * 2;
				int landingY = y + attackDirections[i, 1] * 2;

				// Check if we can attack an opponent piece from this position
				if (IsValidPosition(targetX, targetY) && IsValidPosition(landingX, landingY))
				{
					var targetPos = _board[targetX, targetY];
					var landingPos = _board[landingX, landingY];

					if (targetPos?.Figure != null && !targetPos.Figure.IsBlack && landingPos?.Figure == null)
					{
						// We threaten an opponent piece
						float pieceValue = targetPos.Figure.IsQueen ? QUEEN_VALUE : PIECE_VALUE;
						threatScore += ATTACK_THREAT_BONUS + pieceValue * 0.3f;
					}
				}
			}

			return threatScore;
		}

		/// <summary>
		/// Evaluate piece safety - prefer moves that keep pieces protected
		/// </summary>
		private float EvaluatePieceSafety(PositionPoint from, PositionPoint to, Figure figure)
		{
			// Check if moving away from a threatened position
			float currentThreat = -EvaluateVulnerability(from, figure);
			float newThreat = -EvaluateVulnerability(to, figure);

			// Reward moving out of danger
			if (currentThreat < 0 && newThreat >= 0)
			{
				return PIECE_SAFETY_BONUS * 2;
			}

			return 0;
		}

		/// <summary>
		/// Evaluate material advantage after a capture
		/// </summary>
		private float EvaluateMaterialAdvantage(float capturedValue)
		{
			// Count current material
			int blackPieces = 0, whitePieces = 0;
			int blackQueens = 0, whiteQueens = 0;

			foreach (var point in _points)
			{
				if (point.Figure != null)
				{
					if (point.Figure.IsBlack)
					{
						blackPieces++;
						if (point.Figure.IsQueen) blackQueens++;
					}
					else
					{
						whitePieces++;
						if (point.Figure.IsQueen) whiteQueens++;
					}
				}
			}

			// Calculate material score (after capture)
			float blackMaterial = (blackPieces - blackQueens) * PIECE_VALUE + blackQueens * QUEEN_VALUE;
			float whiteMaterial = (whitePieces - whiteQueens) * PIECE_VALUE + whiteQueens * QUEEN_VALUE - capturedValue;

			float advantage = blackMaterial - whiteMaterial;

			return advantage > 0 ? MATERIAL_ADVANTAGE_BONUS : 0;
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

			Debug.Log($"Hard AI Attack: Score={bestMove.Score:F1}, From=({bestMove.From.X},{bestMove.From.Y}), To=({bestMove.To.X},{bestMove.To.Y})");
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

			Debug.Log($"Hard AI Move: Score={bestMove.Score:F1}, From=({bestMove.From.X},{bestMove.From.Y}), To=({bestMove.To.X},{bestMove.To.Y})");
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
