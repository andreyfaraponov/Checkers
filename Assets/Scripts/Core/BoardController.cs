using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions;
using Gameplay;
using UnityEngine;

namespace Core
{
	public class BoardController : MonoBehaviour
	{
		public const int BoardSize = 8;

		public event Action<Vector2Int> CellClickEvent; 
		public event Action<Figure> FigureAttackedEvent;

		[Header("Points")]
		[SerializeField]
		private List<PositionPoint> _points;

		[SerializeField]
		private PositionPoint _cellPrefab;

		[SerializeField]
		private Transform _cellsRoot;

		[SerializeField]
		private float _cellSize = 0.5f;

		[Header("Figures")]
		[SerializeField]
		private Figure _figurePrefab;

		[SerializeField]
		private Transform _figuresRoot;

		[SerializeField]
		private RemovedFiguresHolder _removedFiguresHolder;

		private int[,] _board = new int[BoardSize, BoardSize];

		public int[,] CurrentBoard => _board;

		public Figure GetFigureAt(Vector2Int position)
		{
			if (position.x < 0 || position.x >= BoardSize || position.y < 0 || position.y >= BoardSize)
				return null;
			
			int index = position.y * BoardSize + position.x;
			if (index >= 0 && index < _points.Count)
				return _points[index].Figure;
			
			return null;
		}

		public void RefreshBoard()
		{
			Clear();

			float startPosX = transform.position.x - _cellSize * BoardSize / 2f + _cellSize / 2f;
			float startPosY = transform.position.y - _cellSize * BoardSize / 2f + _cellSize / 2f;

			FillBoard(startPosX, startPosY);

			if (_points.Count > 0) 
				UpdateCellColors();
		}

		public async UniTask MakeMoveAsync(Vector2Int from, Vector2Int to)
		{
			var figure = _points[from.y * BoardSize + from.x].Figure;
			await _points[to.y * BoardSize + to.x].SetFigureAsync(figure);
			_points[from.y * BoardSize + from.x].SetFigure(null);
			_board[to.y, to.x] = figure.BoardValue;
			_board[from.y, from.x] = 0;
			CheckAndPromoteToQueen(figure, to);
		}

		public async UniTask MakeAttackAsync(Vector2Int from,
			Vector2Int to,
			Vector2Int victimPosition)
		{
			var attackerFigure = _points[from.y * BoardSize + from.x].Figure;
			var victimFigure = _points[victimPosition.y * BoardSize + victimPosition.x].Figure;
			await _points[victimPosition.y * BoardSize + victimPosition.x].SetFigureAsync(attackerFigure, .15f);
			FigureAttackedEvent?.Invoke(victimFigure);
			_points[victimPosition.y * BoardSize + victimPosition.x].SetFigure(null);
			_removedFiguresHolder.AddPiece(victimFigure);
			// TODO Make Animation of elemination
			await _points[to.y * BoardSize + to.x].SetFigureAsync(attackerFigure, .15f);
			
			// RESULT
			_board[to.y, to.x] = attackerFigure.BoardValue;
			_board[victimPosition.y, victimPosition.x] = 0;
			_board[from.y, from.x] = 0;
			CheckAndPromoteToQueen(attackerFigure, to);
		}

		private void CheckAndPromoteToQueen(Figure figure, Vector2Int position)
		{
			if (figure.IsQueen)
				return;

			// White pieces reach top row (y = 7), black pieces reach bottom row (y = 0)
			int promotionRow = figure.IsBlack ? 0 : BoardSize - 1;

			if (position.y == promotionRow)
			{
				figure.SetQueen();
				_board[position.y, position.x] = figure.BoardValue;
			}
		}

		public void Clear()
		{
			foreach (var pt in _points)
			{
				if (pt.Figure != null)
					DestroyImmediate(pt.Figure.gameObject);

				DestroyImmediate(pt.gameObject);
			}

            _removedFiguresHolder.Clear();
			_board = new int[BoardSize, BoardSize];
			_points.Clear();
		}

		public void SpawnInitialFigures()
		{
			for (int y = 0; y < 3; y++)
			{
				for (int x = 0; x < BoardSize; x++)
				{
					if (!IsCellBlack(x, y))
						continue;

					var figure = Instantiate(_figurePrefab, _figuresRoot);
					_board.SetFigure(x, y, figure);
					_points[y * BoardSize + x].SetFigure(figure);
				}
			}

			for (int y = BoardSize - 1; y > BoardSize - 4; --y)
			{
				for (int x = 0; x < BoardSize; x++)
				{
					if (!IsCellBlack(x, y))
						continue;

					var figure = Instantiate(_figurePrefab, _figuresRoot);
					figure.SetBlack();
					_board.SetFigure(x, y, figure);
					_points[y * BoardSize + x].SetFigure(figure);
				}
			}
		}

		private static bool IsCellBlack(int x, int y) =>
			(x + y) % 2 == 0;

		private void UpdateCellColors()
		{
			for (int y = 0; y < BoardSize; y++)
			{
				for (int x = 0; x < BoardSize; x++)
				{
					if (IsCellBlack(x, y))
						_points[y * BoardSize + x].SetBlack();
				}
			}
		}

		private void FillBoard(float startPosX, float startPosY)
		{
			for (int y = 0; y < BoardSize; y++)
			{
				var initPos = new Vector3(startPosX, startPosY + y * _cellSize);

				for (int x = 0; x < BoardSize; x++)
				{
					var pos = new Vector3(initPos.x + x * _cellSize, 0, initPos.y);
					var point = Instantiate(_cellPrefab, _cellsRoot);

					if ((y + x) % 2 == 0) 
						point.SetBlack();
					
					point.PointClickEvent += OnPointClick;
					point.transform.position = pos;
					point.SetPosition(x, y);
					_board[y, x] = point.Figure?.BoardValue ?? 0;
					_points.Add(point);
				}
			}
		}

		private void OnPointClick(PositionPoint obj) => 
			CellClickEvent?.Invoke(new Vector2Int(obj.X, obj.Y));

		public void ResetHighlights()
		{
			_points.ForEach(p => p.ClearHighlight());
		}

		public void HighlightMoveToPosition(Vector2Int position, bool attackPosition = false)
		{
			var point = _points[position.y * BoardSize + position.x];
            point.HighlightMove(attackPosition);
		}

        public void HighlightSelectionAtPosition(Vector2Int pos, bool isAttack = false)
        {
            var point = _points[pos.y * BoardSize + pos.x];
            point.HighlightSelection(isAttack);
        }
    }
}