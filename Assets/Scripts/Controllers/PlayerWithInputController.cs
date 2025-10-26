using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

namespace Controllers
{
	public class PlayerWithInputController : IPlayerController
	{
		private readonly BoardController _boardController;

		private UniTaskCompletionSource _currentTurnCompletionSource;

		private bool _isFigureSelected;
		private Vector2Int _selectedFigurePosition;

		private Dictionary<Vector2Int, AttackData> _moveToAttackPoints = new();

		private Dictionary<Vector2Int, Dictionary<Vector2Int, AttackData>> _figuresThatCanAttack =
			new();

		private List<Vector2Int> _availableMoves = new();
		private bool _isBlackSide;
		private bool _turnInProgress;
		private bool _isFigureLocked;

		public PlayerWithInputController(BoardController boardController, bool isBlackSide = false)
		{
			_isBlackSide = isBlackSide;
			_boardController = boardController;

			_boardController.FigureClickEvent += OnFigureClicked;
		}

		private void OnFigureClicked(Vector2Int pos)
		{
			Debug.LogError($"Figure clicked at position: {pos}");
			if (!_turnInProgress)
				return;

			if (IsPlayerFigureAtPosition(pos))
				ProceedFigureClick(pos);
		}

		public UniTask AwaitMove()
		{
			_turnInProgress = true;
			_availableMoves.Clear();
			_moveToAttackPoints.Clear();
			CheckAvailableMoves();

			_currentTurnCompletionSource = new UniTaskCompletionSource();
			return _currentTurnCompletionSource.Task;
		}

		private void CheckAvailableMoves()
		{
			_figuresThatCanAttack.Clear();
			_availableMoves.Clear();

			for (int y = 0; y < BoardController.BoardSize; y++)
			{
				for (int x = 0; x < BoardController.BoardSize; x++)
				{
					var pos = new Vector2Int(x, y);

					if (IsPlayerFigureAtPosition(pos))
					{
						var attackMoves = GetAvailableAttackMoves(pos);
						if (attackMoves.Count > 0)
							_figuresThatCanAttack.Add(pos, attackMoves);

						var moveMoves =
							CheckersBasics.GetAvailableSimpleMovesForFigure(
								_boardController.CurrentBoard, pos);
						_availableMoves.AddRange(moveMoves);
					}
				}
			}

			if (_figuresThatCanAttack.Count == 0 && _availableMoves.Count == 0)
			{
				// No moves available - end turn immediately
				CompleteTurn();
			}
		}

		private async void ProceedFigureClick(Vector2Int pos)
		{
			if (_isFigureLocked)
			{
				if (_moveToAttackPoints.ContainsKey(pos))
				{
					await StrikeAsync(pos, _moveToAttackPoints[pos]);
					UpdateAttackHighlightForSelectedFigure(pos);
					
					if (_moveToAttackPoints.Count == 0)
						CompleteTurn();
				}
				else
				{
					Debug.LogError($"ONE MORE ATTACK POSSIBLE BUT CLICKED WRONG POSITION");
					// GIVE FIDBACK THAT FIGURE IS LOCKED
				}
				
				return;
			}

			_boardController.ResetHighlights();

			if (_isFigureSelected)
			{
				if (_figuresThatCanAttack.Count > 0)
				{
					if (_moveToAttackPoints.TryGetValue(pos, out var attackData))
					{
						await StrikeAsync(pos, attackData);
						UpdateAttackHighlightForSelectedFigure(pos);
						_isFigureLocked = true;
						
						if (_moveToAttackPoints.Count == 0)
							CompleteTurn();
					}
					else
					{
						DeselectFigure(pos);
					}
					
					return;
				}

				if (_availableMoves.Contains(pos))
				{
					await ProceedSimpleMoveAsync(pos);
					CompleteTurn();
					return;
				}

				DeselectFigure(pos);
			}
			else
			{
				_moveToAttackPoints = GetAvailableAttackMoves(pos);
				_availableMoves =
					GetSimpleMoveForFigure(pos);

				if (_moveToAttackPoints.Count > 0)
				{
					foreach (var position in _moveToAttackPoints.Keys)
					{
						_boardController.HighlightPosition(position);
					}

					_isFigureSelected = true;
					_selectedFigurePosition = pos;
				}
				else if (_availableMoves.Count > 0)
				{
					foreach (var position in _availableMoves)
					{
						_boardController.HighlightPosition(position);
					}

					_isFigureSelected = true;
					_selectedFigurePosition = pos;
				}
			}
		}

		private UniTask ProceedSimpleMoveAsync(Vector2Int pos)
		{
			_boardController.ResetHighlights();
			return _boardController.MakeMoveAsync(_selectedFigurePosition, pos);
		}

		private void UpdateAttackHighlightForSelectedFigure(Vector2Int pos)
		{
			_moveToAttackPoints = GetAvailableAttackMoves(pos);

			foreach (var position in _moveToAttackPoints.Keys) 
				_boardController.HighlightPosition(position);
		}

		private void DeselectFigure(Vector2Int pos)
		{
			_isFigureSelected = false;
		}

		private async Task StrikeAsync(Vector2Int pos, AttackData attackData)
		{
			_boardController.ResetHighlights();
			
			await _boardController.MakeAttackAsync(_selectedFigurePosition, pos,
				attackData.AttackPosition);

			_selectedFigurePosition = pos;
		}

		private bool IsPlayerFigureAtPosition(Vector2Int pos)
		{
			if (_boardController.CurrentBoard[pos.y, pos.x] == 0)
				return false;

			bool isBlackFigure = _boardController.CurrentBoard[pos.y, pos.x] % 2 == 0;
			return isBlackFigure == _isBlackSide;
		}

		private List<Vector2Int> GetSimpleMoveForFigure(Vector2Int pos) =>
			CheckersBasics.GetAvailableSimpleMovesForFigure(_boardController.CurrentBoard,
				pos);

		private Dictionary<Vector2Int, AttackData> GetAvailableAttackMoves(Vector2Int pos) =>
			CheckersBasics.GetAvailableAttacksForFigure(_boardController.CurrentBoard, pos);

		private void CompleteTurn()
		{
			Debug.LogError($"Player completed turn");
			_turnInProgress = false;
			_isFigureLocked = false;
			_isFigureSelected = false;
			_currentTurnCompletionSource.TrySetResult();
		}
	}
}