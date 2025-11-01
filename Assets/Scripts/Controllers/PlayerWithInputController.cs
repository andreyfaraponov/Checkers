using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

namespace Controllers
{
	public enum TurnState
	{
		SelectingFigure,
		MovingFigure,
		Attacking,
		ForceAttacking
	}

	public class PlayerWithInputController : IPlayerController
	{
		private readonly BoardController _boardController;
		private readonly Dictionary<Vector2Int, Dictionary<Vector2Int, AttackData>> _figuresThatCanAttack =
			new();

		private readonly List<Vector2Int> _availableMoves = new();
		private readonly bool _isBlackSide;

		private UniTaskCompletionSource _currentTurnCompletionSource;
		private Vector2Int _selectedFigurePosition;
		private Dictionary<Vector2Int, AttackData> _moveToAttackPoints = new();
		private List<Vector2Int> _currentFigureAvailableMoves = new();

		private TurnState _turnState;
        private bool _isInputEnabled = false;

		public PlayerWithInputController(BoardController boardController, bool isBlackSide = false)
		{
			_isBlackSide = isBlackSide;
			_boardController = boardController;

			_boardController.CellClickEvent += OnCellClicked;
		}

		public UniTask AwaitMove()
		{
			_turnState = TurnState.SelectingFigure;

			CheckAvailableMoves();

			_currentTurnCompletionSource = new UniTaskCompletionSource();
			return _currentTurnCompletionSource.Task;
		}

        public void EnableInput(bool enable)
        {
            _isInputEnabled = enable;
        }

        private async void OnCellClicked(Vector2Int pos)
		{
            if (!_isInputEnabled)
                return;
            
			switch (_turnState)
			{
				case TurnState.SelectingFigure:
					if (IsPlayerFigureAtPosition(pos))
					{
						SelectPiece(pos);
						HighlightMoves();
					}

					break;
				case TurnState.MovingFigure:
					if (IsPlayerFigureAtPosition(pos))
					{
						SelectPiece(pos);
						HighlightMoves();
					}
					else if (_currentFigureAvailableMoves.Contains(pos))
					{
						_boardController.ResetHighlights();
						await _boardController.MakeMoveAsync(_selectedFigurePosition, pos);
						CompleteTurn();
					}
					else
					{
						DeselectFigure();
					}

					break;
				case TurnState.Attacking:
					if (IsPlayerFigureAtPosition(pos))
					{
						SelectPiece(pos);
						HighlightMoves();
					}
					else if (_moveToAttackPoints.ContainsKey(pos))
					{
						await MakeAttackWithCheckAsync(_moveToAttackPoints[pos]);
					}
					else
					{
						DeselectFigure();
					}

					break;
				case TurnState.ForceAttacking:
					if (_moveToAttackPoints.ContainsKey(pos))
					{
						await MakeAttackWithCheckAsync(_moveToAttackPoints[pos]);
					}
					else
					{
						// TODO Highlight that figure is locked
						Debug.LogError($"ONE MORE ATTACK POSSIBLE BUT CLICKED WRONG POSITION");
					}

					break;
			}
		}

		private async Task MakeAttackWithCheckAsync(AttackData attackData)
		{
			_boardController.ResetHighlights();
			await _boardController.MakeAttackAsync(attackData.StartPosition,
				attackData.FinalPosition, attackData.VictimPosition);

			_moveToAttackPoints = GetAvailableAttackMoves(attackData.FinalPosition);

			if (_moveToAttackPoints.Count > 0)
			{
				_turnState = TurnState.ForceAttacking;
				HighlightMoves();
			}
			else
			{
				CompleteTurn();
			}
		}

		private void HighlightMoves()
		{
			_boardController.ResetHighlights();

			if (_moveToAttackPoints.Count > 0)
			{
				foreach (var attackPosition in _moveToAttackPoints.Keys)
					_boardController.HighlightPosition(attackPosition);
			}
			else
			{
				foreach (var movePosition in _currentFigureAvailableMoves)
					_boardController.HighlightPosition(movePosition);
			}
		}

		private void SelectPiece(Vector2Int pos)
		{
			_currentFigureAvailableMoves = GetSimpleMoveForFigure(pos);
			_moveToAttackPoints = GetAvailableAttackMoves(pos);

			if (_figuresThatCanAttack.Count > 0)
			{
				if (!_figuresThatCanAttack.ContainsKey(pos))
					return;

				_selectedFigurePosition = pos;
				_turnState = TurnState.Attacking;
			}
			else
			{
				_selectedFigurePosition = pos;
				_turnState = TurnState.MovingFigure;
			}
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

						var moveMoves = GetSimpleMoveForFigure(pos);
						_availableMoves.AddRange(moveMoves);
					}
				}
			}

			if (_figuresThatCanAttack.Count == 0 && _availableMoves.Count == 0)
			{
				CompleteTurn();
			}
		}

		private void DeselectFigure()
		{
			_turnState = TurnState.SelectingFigure;
			_boardController.ResetHighlights();
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
			Debug.Log($"Player completed turn");
			_currentTurnCompletionSource.TrySetResult();
		}
	}
}