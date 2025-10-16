using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Cysharp.Threading.Tasks;
using Gameplay;
using UnityEngine;
using Utils;
using Object = UnityEngine.Object;

namespace Controllers.AI
{
	public abstract class BaseBotController : IPlayerController
	{
		protected readonly PositionPoint[,] _board;
		protected readonly List<PositionPoint> _points;
		protected readonly Board _boardReference;
		protected UniTaskCompletionSource _currentTurnCompletionSource;
		protected Figure _lastAttackFigure;

		protected BaseBotController(PositionPoint[,] board, List<PositionPoint> points, Board boardReference = null)
		{
			_board = board;
			_points = points;
			_boardReference = boardReference;
		}

		public async UniTask AwaitMove()
		{
			Debug.Log($"AI Move");
			_currentTurnCompletionSource = new UniTaskCompletionSource();
			await MakeMove();

			await _currentTurnCompletionSource.Task;
		}

		/// <summary>
		/// Abstract method to be implemented by derived classes with specific move logic
		/// </summary>
		protected abstract UniTask MakeMove();

		/// <summary>
		/// Try to continue attacking with the same figure if multi-jump is available
		/// </summary>
		
		protected void TryAttackOneMoreTime()
		{
			var figurePosition = _points.First(p => p.Figure == _lastAttackFigure);
			var possibleAttacks = GetAttackActions(figurePosition);

			if (possibleAttacks.Count > 0)
				possibleAttacks[0]?.Invoke();
			else
				_lastAttackFigure = null;
		}

		/// <summary>
		/// Get all available attack moves for all black pieces
		/// </summary>
		protected List<Action> GetAvailableAttackMoves()
		{
			List<Action> result = new List<Action>();

			foreach (var point in _points.Where(p => p.Figure != null && p.Figure.IsBlack))
			{
				result.AddRange(GetAttackActions(point));
			}

			return result;
		}

		/// <summary>
		/// Get attack actions for a specific piece
		/// </summary>
		protected List<Action> GetAttackActions(PositionPoint point)
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

		/// <summary>
		/// Create an action that executes an attack move
		/// </summary>
		protected Action CreateAttackAction(PositionPoint from, PositionPoint to, PositionPoint attack)
		{
			return () =>
			{
				var figureMove = from.Figure;
				bool isBlackFigure = attack.Figure.IsBlack;
				
				to.SetFigure(figureMove);
				from.SetFigure(null);
				Object.Destroy(attack.Figure.gameObject);
				attack.SetFigure(null);
				CheckAndPromoteToQueen(figureMove, to);
				_lastAttackFigure = figureMove;
				
				_boardReference?.OnFigureAttacked(isBlackFigure);
			};
		}

		/// <summary>
		/// Get all figures that have available simple moves
		/// </summary>
		protected List<PositionPoint> GetAvailableToMoveFigures()
		{
			List<PositionPoint> result = new();
			Dictionary<PositionPoint, List<PositionPoint>> possibleMoves = new();

			foreach (var point in _points)
			{
				if (point.Figure != null && point.Figure.IsBlack)
				{
					var moves = GetAvailableMoves(point.Figure);
					possibleMoves[point] = moves;

					if (moves.Count > 0)
						result.Add(point);
				}
			}

			return result;
		}

		/// <summary>
		/// Get all available simple (non-attack) moves for a figure
		/// </summary>
		protected List<PositionPoint> GetAvailableMoves(Figure selectedFigure)
		{
			var figurePosition = _points.First(p => p.Figure == selectedFigure);
			return CheckersBasics.GetAvailableSimpleMoves(_board, figurePosition);
		}

		/// <summary>
		/// Execute a simple move from one point to another
		/// </summary>
		protected void ExecuteSimpleMove(PositionPoint from, PositionPoint to)
		{
			var figure = from.Figure;
			to.SetFigure(figure);
			from.SetFigure(null);
			CheckAndPromoteToQueen(figure, to);
		}

		/// <summary>
		/// Check if a piece should be promoted to queen and promote if needed
		/// </summary>
		protected void CheckAndPromoteToQueen(Figure figure, PositionPoint position)
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
