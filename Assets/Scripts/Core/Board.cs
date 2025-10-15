using System.Collections.Generic;
using Game.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
	public class Board : MonoBehaviour
	{
		public const int BoardSize = 8;

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

		private PositionPoint[,] _boardPositions = new PositionPoint[BoardSize, BoardSize];
		
		public PositionPoint[,] CurrentBoard => _boardPositions;
		public List<PositionPoint> Points => _points;

		public void RefreshBoard()
		{
			Clear();
			
			float startPosX = transform.position.x - _cellSize * BoardSize / 2f + _cellSize / 2f;
			float startPosY = transform.position.y - _cellSize * BoardSize / 2f + _cellSize / 2f;

			for (int y = 0; y < BoardSize; y++)
			{
				var initPos = new Vector3(startPosX, startPosY + y * _cellSize);

				for (int x = 0; x < BoardSize; x++)
				{
					var pos = new Vector3(initPos.x + x * _cellSize, 0, initPos.y);
					var point = Instantiate(_cellPrefab, _cellsRoot);
					
					if ((y + x) % 2 == 0)
						point.SetBlack();
					
					point.transform.position = pos;
					point.SetPosition(x, y);
					_boardPositions[y, x] = point;
					_points.Add(point);
				}
			}

			if (_points.Count > 0)
			{
				for (int y = 0; y < BoardSize; y++)
				{
					for (int x = 0; x < BoardSize; x++)
					{
						if (IsCellBlack(x, y))
							_boardPositions[y, x].SetBlack();
					}
				}
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

			_boardPositions = new PositionPoint[BoardSize, BoardSize];
			_points.Clear();
		}

		public void LocateFigures()
		{
			for (int y = 0; y < 3; y++)
			{
				for (int x = 0; x < BoardSize; x++)
				{
					if (!IsCellBlack(x, y))
						continue;

					var figure = Instantiate(_figurePrefab, _figuresRoot);
					_boardPositions[y, x].SetFigure(figure);
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
					_boardPositions[y, x].SetFigure(figure);
				}
			}
		}

		private static bool IsCellBlack(int x, int y) =>
			(x + y) % 2 == 0;

		#region EditorData
		#if UNITY_EDITOR

		[MenuItem("Board/Detect black cells")]
		public static void DetectBlackCells()
		{
			for (int y = 0; y < BoardSize; y++)
			{
				string str = $"line[{y}] ";

				for (int x = 0; x < BoardSize; x++)
				{
					str += $" {IsCellBlack(x, y)}";
				}

				Debug.Log(str);
			}
		}

		[MenuItem("Board/Generate Board")]
		public static void GenerateCells()

		{
			var scene = SceneManager.GetActiveScene();
			var objects = scene.GetRootGameObjects();


			Board board = GetBoard(objects);
			if (board == null)
				return;

			board.RefreshBoard();
		}

		[MenuItem("Board/Clear board")]
		public static void ClearBoard()

		{
			var scene = SceneManager.GetActiveScene();
			var objects = scene.GetRootGameObjects();


			Board board = GetBoard(objects);
			if (board == null)
				return;

			board.Clear();
		}

		private static Board GetBoard(GameObject[] objects)
		{
			foreach (var obj in objects)
			{
				if (obj.TryGetComponent<Board>(out var board))
					return board;
			}

			return null;
		}
		#endif

		#endregion
	}
}