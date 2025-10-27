using System;
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
		None,
		SelectingFigure,
		MovingFigure,
		Attacking,
		ForceAttacking
	}

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
		private List<Vector2Int> _currentFigureAvailableMoves = new();
		private bool _isBlackSide;
		private bool _isFigureLocked;

		private TurnState _turnState;

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

		private async void OnCellClicked(Vector2Int pos)
		{
			Debug.Log($"Pos Click: {pos.y} {pos.x}, current state: {_turnState}");
			switch (_turnState)
			{
				case TurnState.SelectingFigure:
					Debug.Log($"Selection");
					if (IsPlayerFigureAtPosition(pos))
					{
						SelectPiece(pos);
						HighlightMoves();
					}

					break;
				case TurnState.MovingFigure:
					Debug.Log($"Moving");
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
					Debug.Log($"Attacking");
					if (IsPlayerFigureAtPosition(pos))
					{
						SelectPiece(pos);
						HighlightMoves();
					}

					break;
//				case TurnState.ForceAttacking:
//					break;
//				default:
//					throw new ArgumentOutOfRangeException();
			}
//
//			Debug.LogError($"Figure clicked at position: {pos}");
//
//			if (IsPlayerFigureAtPosition(pos))
//				ProceedFigureClick(pos);
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
//			if (_isFigureSelected)
//			{
//				if (_figuresThatCanAttack.Count > 0)
//				{
//					if (_isFigureLocked)
//						// Try force attack or deselect
//						if (_moveToAttackPoints.TryGetValue(pos, out var attackData))
//						{
//							await StrikeAsync(pos, attackData);
//							CheckAvailableMoves();
//							UpdateAttackHighlightForSelectedFigure(pos);
//							_isFigureLocked = true;
//
//							if (_moveToAttackPoints.Count == 0)
//								CompleteTurn();
//						}
//						else
//						{
//							DeselectFigure();
//						}
//				}
//				else if (_availableMoves.Contains(pos))
//				{
//					await ProceedSimpleMoveAsync(pos);
//					CompleteTurn();
//					return;
//				}
//				else
//				{
//					DeselectFigure();
//				}
//			}
//			else
//			{
//				// try select a new figure
//			}
//
//			if (_isFigureLocked)
//			{
//				if (_moveToAttackPoints.ContainsKey(pos))
//				{
//					await StrikeAsync(pos, _moveToAttackPoints[pos]);
//					UpdateAttackHighlightForSelectedFigure(pos);
//
//					if (_moveToAttackPoints.Count == 0)
//						CompleteTurn();
//				}
//				else
//				{
//					Debug.LogError($"ONE MORE ATTACK POSSIBLE BUT CLICKED WRONG POSITION");
//					// GIVE FIDBACK THAT FIGURE IS LOCKED
//				}
//
//				return;
//			}
//
//			_boardController.ResetHighlights();
//
//			if (_isFigureSelected)
//			{
//				if (_figuresThatCanAttack.Count > 0)
//				{
//					if (_moveToAttackPoints.TryGetValue(pos, out var attackData))
//					{
//						await StrikeAsync(pos, attackData);
//						UpdateAttackHighlightForSelectedFigure(pos);
//						_isFigureLocked = true;
//
//						if (_moveToAttackPoints.Count == 0)
//							CompleteTurn();
//					}
//					else
//					{
//						DeselectFigure();
//					}
//
//					return;
//				}
//
//				if (_availableMoves.Contains(pos))
//				{
//					await ProceedSimpleMoveAsync(pos);
//					CompleteTurn();
//					return;
//				}
//
//				DeselectFigure();
//			}
//			else
//			{
//				_moveToAttackPoints = GetAvailableAttackMoves(pos);
//				_availableMoves =
//					GetSimpleMoveForFigure(pos);
//
//				if (_moveToAttackPoints.Count > 0)
//				{
//					foreach (var position in _moveToAttackPoints.Keys)
//					{
//						_boardController.HighlightPosition(position);
//					}
//
//					_isFigureSelected = true;
//					_selectedFigurePosition = pos;
//				}
//				else if (_availableMoves.Count > 0)
//				{
//					foreach (var position in _availableMoves)
//					{
//						_boardController.HighlightPosition(position);
//					}
//
//					_isFigureSelected = true;
//					_selectedFigurePosition = pos;
//				}
//			}
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

		private void DeselectFigure()
		{
			_turnState = TurnState.SelectingFigure;
			_boardController.ResetHighlights();
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
			Debug.Log($"Player completed turn");
			_isFigureLocked = false;
			_isFigureSelected = false;
			_currentTurnCompletionSource.TrySetResult();
		}
	}
}