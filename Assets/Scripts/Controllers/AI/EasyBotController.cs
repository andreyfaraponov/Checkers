using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Gameplay;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Controllers.AI
{
	/// <summary>
	/// Easy difficulty AI that makes random moves from available options
	/// </summary>
	public class EasyBotController : BaseBotController
	{
		private readonly Dictionary<PositionPoint, List<PositionPoint>> _possibleMoves = new();

		public EasyBotController(PositionPoint[,] board, List<PositionPoint> points, Board boardReference = null) 
			: base(board, points, boardReference)
		{
		}

		protected override async UniTask MakeMove()
		{
			_possibleMoves.Clear();
			
			List<Action> attackActions = GetAvailableAttackMoves();
			List<PositionPoint> availableToMoveFigures = GetAvailableToMoveFiguresWithCache();

			await UniTask.Delay(500);
			await UniTask.SwitchToMainThread();

			if (attackActions.Count > 0)
			{
				// Execute random attack
				attackActions[Random.Range(0, attackActions.Count)]?.Invoke();

				// Continue attacking if multi-jump available
				while (_lastAttackFigure != null)
				{
					await UniTask.Delay(500);
					TryAttackOneMoreTime();
				}
			}
			else if (availableToMoveFigures.Count > 0)
			{
				// Make random simple move
				MakeFigureMove(availableToMoveFigures[Random.Range(0, availableToMoveFigures.Count)]);
			}

			await UniTask.Delay(500);

			_currentTurnCompletionSource.TrySetResult();
		}

		/// <summary>
		/// Get available figures and cache their possible moves for later use
		/// </summary>
		private List<PositionPoint> GetAvailableToMoveFiguresWithCache()
		{
			List<PositionPoint> result = new();

			foreach (var point in _points)
			{
				if (point.Figure != null && point.Figure.IsBlack)
				{
					var moves = GetAvailableMoves(point.Figure);
					_possibleMoves[point] = moves;

					if (moves.Count > 0)
						result.Add(point);
				}
			}

			return result;
		}

		/// <summary>
		/// Execute a random simple move for the selected figure
		/// </summary>
		private void MakeFigureMove(PositionPoint pointWithFigureToMove)
		{
			var movePoints = _possibleMoves[pointWithFigureToMove];

			PositionPoint targetPoint;
			if (movePoints.Count > 1)
			{
				// Random selection if multiple moves available
				targetPoint = Random.Range(0, 1000) > 500 ? movePoints[0] : movePoints[1];
			}
			else
			{
				targetPoint = movePoints[0];
			}

			ExecuteSimpleMove(pointWithFigureToMove, targetPoint);
		}
	}
}