using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Gameplay;
using Game.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Game.Controllers.AI
{
	public class EasyBotController : IPlayerController
	{
		private readonly PositionPoint[,] _board;
		private readonly List<PositionPoint> _points;

		private readonly Dictionary<PositionPoint, List<PositionPoint>> _possibleMoves = new();

		private UniTaskCompletionSource _currentTurnCompletionSource;
		private Figure _lastAttackFigure;

		public EasyBotController(PositionPoint[,] board, List<PositionPoint> points)
		{
			_points = points;
			_board = board;
		}

		public UniTask AwaitMove()
		{
			Debug.Log($"AI Move");
			_possibleMoves.Clear();
			_currentTurnCompletionSource = new UniTaskCompletionSource();
			UniTask.RunOnThreadPool(MakeMove);

			return _currentTurnCompletionSource.Task;
		}

		private async UniTask MakeMove()
		{
			List<Action> attackActions = GetAvailableAttackMoves();
			List<PositionPoint> availableToMoveFigures = GetAvailableToMoveFigures();

			await UniTask.Delay(500);
			await UniTask.SwitchToMainThread();

			if (attackActions.Count > 0)
			{
				attackActions[Random.Range(0, attackActions.Count)]?.Invoke();

				while (_lastAttackFigure != null)
				{
					await UniTask.Delay(500);
					TryAttackOneMoreTime();
				}
			}
			else if (availableToMoveFigures.Count > 0)
			{
				MakeFigureMove(availableToMoveFigures[Random.Range(0, availableToMoveFigures.Count)]);
			}

			await UniTask.Delay(500);

			_currentTurnCompletionSource.TrySetResult();
		}

		private void TryAttackOneMoreTime()
		{
			var figurePosition = _points.First(p => p.Figure == _lastAttackFigure);
			var possibleAttacks = GetAttackActions(figurePosition);

			if (possibleAttacks.Count > 0)
				possibleAttacks[0]?.Invoke();
			else
				_lastAttackFigure = null;
		}

		private List<Action> GetAvailableAttackMoves()
		{
			List<Action> result = new List<Action>();

			foreach (var point in _points.Where(p => p.Figure != null && p.Figure.IsBlack))
			{
				result.AddRange(GetAttackActions(point));
			}

			return result;
		}

		private List<Action> GetAttackActions(PositionPoint point)
		{
			List<Action> resultList = new List<Action>();
			var figurePosition = _points.First(p => p.Figure == point.Figure);
			
			foreach (var attackData in CheckersBasics.GetAvailableAttacksDictionary(_board,
						 figurePosition).Values)
			{
				resultList.Add(CreateAttackAction(attackData.StartPosition, attackData.FinalPosition,
					attackData.AttackPosition));
			}

			return resultList;
		}

		private Action CreateAttackAction(PositionPoint from,
			PositionPoint to,
			PositionPoint attack)
		{
			return () =>
			{
				var figureMove = from.Figure;
				to.SetFigure(figureMove);
				from.SetFigure(null);
				Object.Destroy(attack.Figure.gameObject);
				attack.SetFigure(null);
				CheckAndPromoteToQueen(figureMove, to);
				_lastAttackFigure = figureMove;
			};
		}

		private void MakeFigureMove(PositionPoint pointWithFigureToMove)
		{
			var movePoints = _possibleMoves[pointWithFigureToMove];
			var figure = pointWithFigureToMove.Figure;

			PositionPoint targetPoint;
			if (movePoints.Count > 1)
			{
				targetPoint = Random.Range(0, 1000) > 500 ? movePoints[0] : movePoints[1];
			}
			else
			{
				targetPoint = movePoints[0];
			}

			targetPoint.SetFigure(figure);
			pointWithFigureToMove.SetFigure(null);
			CheckAndPromoteToQueen(figure, targetPoint);
		}

		private List<PositionPoint> GetAvailableToMoveFigures()
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

		private List<PositionPoint> GetAvailableMoves(Figure selectedFigure)
		{
			var figurePosition = _points.First(p => p.Figure == selectedFigure);
			return CheckersBasics.GetAvailableSimpleMoves(_board, figurePosition);
		}

		private void CheckAndPromoteToQueen(Figure figure, PositionPoint position)
		{
			if (figure.IsQueen)
				return;

			// White pieces reach top row (y = 7), black pieces reach bottom row (y = 0)
			int promotionRow = figure.IsBlack ? 0 : Board.BoardSize - 1;

			if (position.Y == promotionRow)
			{
				figure.SetQueen();
				Debug.Log($"{(figure.IsBlack ? "Black" : "White")} AI piece promoted to Queen at ({position.X}, {position.Y})");
			}
		}
	}
}