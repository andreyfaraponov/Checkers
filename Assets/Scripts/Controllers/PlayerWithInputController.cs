using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Gameplay;
using Game.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Controllers
{
	public class PlayerWithInputController : IPlayerController
	{
		private readonly PositionPoint[,] _board;
		private readonly List<PositionPoint> _points;
		private readonly Board _boardReference;

		private UniTaskCompletionSource _currentTurnCompletionSource;
		private Figure _selectedFigure;
		private Dictionary<PositionPoint, AttackData> _moveToAttackPoint = new();
		private Dictionary<Figure, Dictionary<PositionPoint, AttackData>> _figuresThatCanAttack = new();
		private List<PositionPoint> _availableMoves = new();

		public PlayerWithInputController(PositionPoint[,] board, List<PositionPoint> points, Board boardReference = null)
		{
			_board = board;
			_points = points;
			_boardReference = boardReference;
		}

		public UniTask AwaitMove()
		{
			_availableMoves.Clear();
			_moveToAttackPoint.Clear();
			Debug.Log($"Player turn");
			Subscribe();
			_currentTurnCompletionSource = new UniTaskCompletionSource();
			CheckAttackPositions();

			return _currentTurnCompletionSource.Task;
		}

		private void CheckAttackPositions()
		{
			_figuresThatCanAttack.Clear();

			var playerFigures = _points
				.Where(p => p.Figure != null && !p.Figure.IsBlack)
				.Select(p => p.Figure);

			foreach (var figure in playerFigures)
			{
				var attackMoves = GetAvailableAttackMoves(figure);
				if (attackMoves.Count > 0)
					_figuresThatCanAttack.Add(figure, attackMoves);
			}
		}

		private void PointOnPointClickEvent(PositionPoint moveTo)
		{
			if (_selectedFigure != null)
			{
				if (!IsValidMove(moveTo))
					return;

				if (IsMoveWithAttackPossible())
				{
					ExecuteAttackMove(moveTo);
				}
				else if (IsSimpleMovePossible(_selectedFigure, moveTo))
				{
					ExecuteSimpleMove(moveTo);
				}
			}

			ClearSelectionAndHighlights();
		}

		private bool IsValidMove(PositionPoint moveTo)
		{
			if (_figuresThatCanAttack.Count == 0)
				return true;

			return _figuresThatCanAttack.ContainsKey(_selectedFigure) &&
			       _figuresThatCanAttack[_selectedFigure].ContainsKey(moveTo);
		}

		private void ExecuteAttackMove(PositionPoint moveTo)
		{
			MoveFigure(_selectedFigure, moveTo);
			AttackFigure(moveTo);

			CheckAndPromoteToQueen(_selectedFigure, moveTo);

			_moveToAttackPoint = GetAvailableAttackMoves(_selectedFigure);

			if (_moveToAttackPoint.Count > 0)
			{
				CheckAttackPositions();
				FigureOnPickFigureEvent(_selectedFigure);
				return;
			}

			CompleteTurn();
		}

		private void ExecuteSimpleMove(PositionPoint moveTo)
		{
			var figurePoint = GetFigurePosition(_selectedFigure);
			figurePoint.SetFigure(null);
			moveTo.SetFigure(_selectedFigure);

			CheckAndPromoteToQueen(_selectedFigure, moveTo);
			CompleteTurn();
		}

		private void CompleteTurn()
		{
			Unsubscribe();
			_currentTurnCompletionSource.TrySetResult();
		}

		private void ClearSelectionAndHighlights()
		{
			_selectedFigure = null;
			_availableMoves.Clear();
			_moveToAttackPoint.Clear();
			HideAllHighlights();
		}

		private void AttackFigure(PositionPoint moveTo)
		{
			var pointFigureToRemove = _moveToAttackPoint[moveTo];
			bool isBlackFigure = pointFigureToRemove.AttackPosition.Figure.IsBlack;
			
			Object.Destroy(pointFigureToRemove.AttackPosition.Figure.gameObject);
			pointFigureToRemove.AttackPosition.SetFigure(null);
			
			_boardReference?.OnFigureAttacked(isBlackFigure);
		}

		private void MoveFigure(Figure figure, PositionPoint destination)
		{
			var figurePoint = GetFigurePosition(figure);
			figurePoint.SetFigure(null);
			destination.SetFigure(figure);
		}

		private PositionPoint GetFigurePosition(Figure figure) =>
			_points.First(p => p.Figure == figure);

		private void CheckAndPromoteToQueen(Figure figure, PositionPoint position)
		{
			if (figure.IsQueen)
				return;

			// White pieces (player) reach top row (y = 7), black pieces reach bottom row (y = 0)
			int promotionRow = figure.IsBlack ? 0 : Board.BoardSize - 1;

			if (position.Y == promotionRow)
			{
				figure.SetQueen();
				Debug.Log($"{(figure.IsBlack ? "Black" : "White")} piece promoted to Queen at ({position.X}, {position.Y})");
			}
		}

		private bool IsMoveWithAttackPossible() =>
			_figuresThatCanAttack.ContainsKey(_selectedFigure);

		private bool IsSimpleMovePossible(Figure _, PositionPoint point) =>
			point.Figure == null && _availableMoves.Contains(point);

		private void HideAllHighlights() =>
			_points.ForEach(p => p.Highlight(false));

		private void FigureOnPickFigureEvent(Figure figure)
		{
			HideAllHighlights();

			if (_figuresThatCanAttack.Count > 0 && !_figuresThatCanAttack.ContainsKey(figure))
				return; // TODO: Show feedback when player must attack

			var point = _points.FirstOrDefault(p => p.Figure == figure);
			if (point == null)
				return;

			_selectedFigure = figure;
			_availableMoves = GetAvailableMoves(figure);
			_moveToAttackPoint = GetAvailableAttackMoves(figure);

			if (_moveToAttackPoint.Count > 0)
			{
				foreach (var position in _moveToAttackPoint.Keys)
					position.Highlight(true);
			}
			else
			{
				foreach (var position in _availableMoves)
					position.Highlight(true);
			}
		}

		private Dictionary<PositionPoint, AttackData> GetAvailableAttackMoves(Figure figure)
		{
			var figurePosition = GetFigurePosition(figure);
			return CheckersBasics.GetAvailableAttacksDictionary(_board, figurePosition);
		}

		private List<PositionPoint> GetAvailableMoves(Figure selectedFigure)
		{
			var figurePosition = GetFigurePosition(selectedFigure);
			return CheckersBasics.GetAvailableSimpleMoves(_board, figurePosition);
		}

		private void Unsubscribe() =>
			ToggleEventSubscription(false);

		private void Subscribe() =>
			ToggleEventSubscription(true);

		private void ToggleEventSubscription(bool subscribe)
		{
			foreach (var point in _points)
			{
				if (subscribe)
					point.PointClickEvent += PointOnPointClickEvent;
				else
					point.PointClickEvent -= PointOnPointClickEvent;

				if (point.Figure != null && !point.Figure.IsBlack)
				{
					if (subscribe)
						point.Figure.PickFigureEvent += FigureOnPickFigureEvent;
					else
						point.Figure.PickFigureEvent -= FigureOnPickFigureEvent;
				}
			}
		}
	}
}