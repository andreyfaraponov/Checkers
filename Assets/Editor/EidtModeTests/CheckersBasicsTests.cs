using System.Collections.Generic;
using System.Linq;
using Core;
using Gameplay;
using NUnit.Framework;
using UnityEngine;
using Utils;

namespace Editor.EidtModeTests
{
	[TestFixture]
	public class CheckersBasicsTests
	{
		private PositionPoint[,] _board;
		private List<PositionPoint> _points;
		private GameObject _testContainer;

		[SetUp]
		public void Setup()
		{
			_testContainer = new GameObject("TestContainer");
			_board = new PositionPoint[Board.BoardSize, Board.BoardSize];
			_points = new List<PositionPoint>();

			// Create a basic board structure
			for (int y = 0; y < Board.BoardSize; y++)
			{
				for (int x = 0; x < Board.BoardSize; x++)
				{
					var pointObj = new GameObject($"Point_{x}_{y}");
					pointObj.transform.SetParent(_testContainer.transform);
					var point = pointObj.AddComponent<PositionPoint>();
					point.SetPosition(x, y);
					_board[y, x] = point;
					_points.Add(point);
				}
			}
		}

		[TearDown]
		public void TearDown()
		{
			if (_testContainer != null)
				Object.DestroyImmediate(_testContainer);
		}

		private Figure CreateFigure(bool isBlack, bool isQueen = false)
		{
			var figureObj = new GameObject("TestFigure");
			figureObj.transform.SetParent(_testContainer.transform);
			var figure = figureObj.AddComponent<Figure>();
			
			if (isBlack)
				figure.SetBlack();
			
			if (isQueen)
				figure.SetQueen();
			
			return figure;
		}

		#region Simple Moves Tests

		[Test]
		public void GetAvailableSimpleMoves_WhitePieceAtStart_ReturnsForwardDiagonalMoves()
		{
			// Arrange: White piece at (3, 2)
			var figure = CreateFigure(isBlack: false);
			_board[2, 3].SetFigure(figure);

			// Act
			var moves = CheckersBasics.GetAvailableSimpleMoves(_board, _board[2, 3]);

			// Assert
			Assert.AreEqual(2, moves.Count, "White piece should have 2 forward diagonal moves");
			Assert.IsTrue(moves.Any(m => m.X == 2 && m.Y == 3), "Should be able to move forward-left");
			Assert.IsTrue(moves.Any(m => m.X == 4 && m.Y == 3), "Should be able to move forward-right");
		}

		[Test]
		public void GetAvailableSimpleMoves_BlackPieceAtStart_ReturnsForwardDiagonalMoves()
		{
			// Arrange: Black piece at (3, 5)
			var figure = CreateFigure(isBlack: true);
			_board[5, 3].SetFigure(figure);

			// Act
			var moves = CheckersBasics.GetAvailableSimpleMoves(_board, _board[5, 3]);

			// Assert
			Assert.AreEqual(2, moves.Count, "Black piece should have 2 forward diagonal moves");
			Assert.IsTrue(moves.Any(m => m.X == 2 && m.Y == 4), "Should be able to move forward-left");
			Assert.IsTrue(moves.Any(m => m.X == 4 && m.Y == 4), "Should be able to move forward-right");
		}

		[Test]
		public void GetAvailableSimpleMoves_PieceAtBoardEdge_ReturnsOnlyValidMoves()
		{
			// Arrange: White piece at left edge (0, 2)
			var figure = CreateFigure(isBlack: false);
			_board[2, 0].SetFigure(figure);

			// Act
			var moves = CheckersBasics.GetAvailableSimpleMoves(_board, _board[2, 0]);

			// Assert
			Assert.AreEqual(1, moves.Count, "Edge piece should have only 1 valid move");
			Assert.IsTrue(moves.Any(m => m.X == 1 && m.Y == 3), "Should be able to move forward-right only");
		}

		[Test]
		public void GetAvailableSimpleMoves_BlockedByOwnPiece_ReturnsNoMoves()
		{
			// Arrange: White pieces blocking each other
			var figure1 = CreateFigure(isBlack: false);
			var figure2 = CreateFigure(isBlack: false);
			var figure3 = CreateFigure(isBlack: false);
			
			_board[2, 3].SetFigure(figure1);
			_board[3, 2].SetFigure(figure2);  // Blocks forward-left
			_board[3, 4].SetFigure(figure3);  // Blocks forward-right

			// Act
			var moves = CheckersBasics.GetAvailableSimpleMoves(_board, _board[2, 3]);

			// Assert
			Assert.AreEqual(0, moves.Count, "Piece blocked by own pieces should have no moves");
		}

		[Test]
		public void GetAvailableSimpleMoves_QueenPiece_ReturnsLongRangeMoves()
		{
			// Arrange: White queen at (3, 3)
			var queen = CreateFigure(isBlack: false, isQueen: true);
			_board[3, 3].SetFigure(queen);

			// Act
			var moves = CheckersBasics.GetAvailableSimpleMoves(_board, _board[3, 3]);

			// Assert
			Assert.Greater(moves.Count, 4, "Queen should have multiple long-range moves");
			
			// Check all 4 diagonal directions have moves
			Assert.IsTrue(moves.Any(m => m.X < 3 && m.Y < 3), "Queen should have moves in down-left direction");
			Assert.IsTrue(moves.Any(m => m.X > 3 && m.Y < 3), "Queen should have moves in down-right direction");
			Assert.IsTrue(moves.Any(m => m.X < 3 && m.Y > 3), "Queen should have moves in up-left direction");
			Assert.IsTrue(moves.Any(m => m.X > 3 && m.Y > 3), "Queen should have moves in up-right direction");
		}

		[Test]
		public void GetAvailableSimpleMoves_QueenBlockedByPiece_StopsAtBlockingPiece()
		{
			// Arrange: White queen at (3, 3), blocked by piece at (5, 5)
			var queen = CreateFigure(isBlack: false, isQueen: true);
			var blocker = CreateFigure(isBlack: false);
			
			_board[3, 3].SetFigure(queen);
			_board[5, 5].SetFigure(blocker);

			// Act
			var moves = CheckersBasics.GetAvailableSimpleMoves(_board, _board[3, 3]);

			// Assert
			Assert.IsTrue(moves.Any(m => m.X == 4 && m.Y == 4), "Queen should reach (4,4)");
			Assert.IsFalse(moves.Any(m => m.X == 5 && m.Y == 5), "Queen should not reach blocking piece");
			Assert.IsFalse(moves.Any(m => m.X == 6 && m.Y == 6), "Queen should not reach beyond blocker");
		}

		[Test]
		public void GetAvailableSimpleMoves_NullFigure_ReturnsEmptyList()
		{
			// Act
			var moves = CheckersBasics.GetAvailableSimpleMoves(_board, _board[3, 3]);

			// Assert
			Assert.AreEqual(0, moves.Count, "Empty position should return no moves");
		}

		#endregion

		#region Attack Moves Tests

		[Test]
		public void GetAvailableAttacksDictionary_RegularPieceCanAttackEnemy_ReturnsAttack()
		{
			// Arrange: White at (3, 2), Black enemy at (4, 3), landing at (5, 4)
			var whiteFigure = CreateFigure(isBlack: false);
			var blackFigure = CreateFigure(isBlack: true);
			
			_board[2, 3].SetFigure(whiteFigure);
			_board[3, 4].SetFigure(blackFigure);

			// Act
			var attacks = CheckersBasics.GetAvailableAttacksDictionary(_board, _board[2, 3]);

			// Assert
			Assert.AreEqual(1, attacks.Count, "Should have 1 attack available");
			var attackData = attacks.First().Value;
			Assert.AreEqual(_board[2, 3], attackData.StartPosition);
			Assert.AreEqual(_board[3, 4], attackData.AttackPosition);
			Assert.AreEqual(_board[4, 5], attackData.FinalPosition);
		}

		[Test]
		public void GetAvailableAttacksDictionary_MultipleEnemies_ReturnsAllAttacks()
		{
			// Arrange: White at (3, 3), enemies in multiple directions
			var whiteFigure = CreateFigure(isBlack: false);
			var blackFigure1 = CreateFigure(isBlack: true);
			var blackFigure2 = CreateFigure(isBlack: true);
			
			_board[3, 3].SetFigure(whiteFigure);
			_board[4, 4].SetFigure(blackFigure1);  // Forward-right
			_board[2, 2].SetFigure(blackFigure2);  // Backward-left

			// Act
			var attacks = CheckersBasics.GetAvailableAttacksDictionary(_board, _board[3, 3]);

			// Assert
			Assert.AreEqual(2, attacks.Count, "Should have 2 attacks available");
		}

		[Test]
		public void GetAvailableAttacksDictionary_LandingSpaceOccupied_NoAttack()
		{
			// Arrange: White at (3, 2), Black enemy at (4, 3), landing occupied
			var whiteFigure = CreateFigure(isBlack: false);
			var blackFigure = CreateFigure(isBlack: true);
			var blocker = CreateFigure(isBlack: false);
			
			_board[2, 3].SetFigure(whiteFigure);
			_board[3, 4].SetFigure(blackFigure);
			_board[4, 5].SetFigure(blocker);  // Blocks landing

			// Act
			var attacks = CheckersBasics.GetAvailableAttacksDictionary(_board, _board[2, 3]);

			// Assert
			Assert.AreEqual(0, attacks.Count, "Cannot attack if landing space is occupied");
		}

		[Test]
		public void GetAvailableAttacksDictionary_OwnPieceAdjaent_NoAttack()
		{
			// Arrange: White at (3, 2), another white at (4, 3)
			var whiteFigure1 = CreateFigure(isBlack: false);
			var whiteFigure2 = CreateFigure(isBlack: false);
			
			_board[2, 3].SetFigure(whiteFigure1);
			_board[3, 4].SetFigure(whiteFigure2);

			// Act
			var attacks = CheckersBasics.GetAvailableAttacksDictionary(_board, _board[2, 3]);

			// Assert
			Assert.AreEqual(0, attacks.Count, "Cannot attack own pieces");
		}

		[Test]
		public void GetAvailableAttacksDictionary_QueenLongRangeAttack_ReturnsAllValidAttacks()
		{
			// Arrange: White queen at (1, 1), black enemy at (3, 3)
			var whiteQueen = CreateFigure(isBlack: false, isQueen: true);
			var blackFigure = CreateFigure(isBlack: true);
			
			_board[1, 1].SetFigure(whiteQueen);
			_board[3, 3].SetFigure(blackFigure);

			// Act
			var attacks = CheckersBasics.GetAvailableAttacksDictionary(_board, _board[1, 1]);

			// Assert
			Assert.Greater(attacks.Count, 0, "Queen should be able to attack long range");
			
			// Should have multiple landing positions after the enemy
			var validLandings = new[] { _board[4, 4], _board[5, 5], _board[6, 6], _board[7, 7] };
			foreach (var landing in validLandings)
			{
				Assert.IsTrue(attacks.ContainsKey(landing), $"Queen should be able to land at ({landing.X},{landing.Y})");
			}
		}

		[Test]
		public void GetAvailableAttacksDictionary_QueenMultipleEnemiesInLine_AttacksOnlyFirst()
		{
			// Arrange: White queen at (1, 1), two black enemies at (3, 3) and (5, 5)
			var whiteQueen = CreateFigure(isBlack: false, isQueen: true);
			var blackFigure1 = CreateFigure(isBlack: true);
			var blackFigure2 = CreateFigure(isBlack: true);
			
			_board[1, 1].SetFigure(whiteQueen);
			_board[3, 3].SetFigure(blackFigure1);
			_board[5, 5].SetFigure(blackFigure2);

			// Act
			var attacks = CheckersBasics.GetAvailableAttacksDictionary(_board, _board[1, 1]);

			// Assert
			// Should be able to jump first enemy to (4,4) but stops there
			Assert.IsTrue(attacks.ContainsKey(_board[4, 4]), "Should attack first enemy to (4,4)");
			Assert.IsFalse(attacks.ContainsKey(_board[6, 6]), "Should not jump over second enemy");
		}

		[Test]
		public void GetAvailableAttacksDictionary_AtBoardEdge_NoOutOfBoundsAttacks()
		{
			// Arrange: White at corner (0, 0), black at (1, 1)
			var whiteFigure = CreateFigure(isBlack: false);
			var blackFigure = CreateFigure(isBlack: true);
			
			_board[0, 0].SetFigure(whiteFigure);
			_board[1, 1].SetFigure(blackFigure);

			// Act
			var attacks = CheckersBasics.GetAvailableAttacksDictionary(_board, _board[1, 1]);

			// Assert
			// Would need to land at (-1, -1) which is out of bounds
			Assert.AreEqual(0, attacks.Count, "Cannot attack if landing would be out of bounds");
		}

		#endregion

		#region Game State Tests

		[Test]
		public void CheckGameState_NoWhitePieces_ReturnsOpponentWin()
		{
			// Arrange: Only black pieces, no white pieces
			var blackFigure = CreateFigure(isBlack: true);
			_board[5, 3].SetFigure(blackFigure);
			// Note: _board and _points reference the same objects, setting once is enough

			// Act
			var state = CheckersBasics.CheckGameState(_board, _points);

			// Assert
			Assert.AreEqual(GameState.OpponentWin, state, "When no white pieces exist, opponent should win");
		}

		[Test]
		public void CheckGameState_NoBlackPieces_ReturnsPlayerWin()
		{
			// Arrange: Only white pieces, no black pieces
			var whiteFigure = CreateFigure(isBlack: false);
			_board[2, 3].SetFigure(whiteFigure);
			// Note: _board and _points reference the same objects, setting once is enough

			// Act
			var state = CheckersBasics.CheckGameState(_board, _points);

			// Assert
			Assert.AreEqual(GameState.PlayerWin, state, "When no black pieces exist, player should win");
		}

		[Test]
		public void CheckGameState_WhiteHasNoMoves_ReturnsOpponentWin()
		{
			// Arrange: White piece completely blocked
			var whiteFigure = CreateFigure(isBlack: false);
			var blackFigure1 = CreateFigure(isBlack: true);
			var blackFigure2 = CreateFigure(isBlack: true);
			
			_board[0, 0].SetFigure(whiteFigure);
			_board[1, 1].SetFigure(blackFigure1);
			_board[2, 2].SetFigure(blackFigure2);
			// Note: _board and _points reference the same objects, no need to set twice

			// Act
			var state = CheckersBasics.CheckGameState(_board, _points);

			// Assert
			Assert.AreEqual(GameState.OpponentWin, state, "When white has no valid moves, opponent should win");
		}

		[Test]
		public void CheckGameState_BlackHasNoMoves_ReturnsPlayerWin()
		{
			// Arrange: Black piece completely blocked at top edge
			var blackFigure = CreateFigure(isBlack: true);
			var whiteFigure1 = CreateFigure(isBlack: false);
			var whiteFigure2 = CreateFigure(isBlack: false);
			
			_board[7, 0].SetFigure(blackFigure);
			_board[6, 1].SetFigure(whiteFigure1);
			_board[5, 2].SetFigure(whiteFigure2);
			// Note: _board and _points reference the same objects, no need to set twice

			// Act
			var state = CheckersBasics.CheckGameState(_board, _points);

			// Assert
			Assert.AreEqual(GameState.PlayerWin, state, "When black has no valid moves, player should win");
		}

		[Test]
		public void CheckGameState_BothHaveMoves_ReturnsPlaying()
		{
			// Arrange: Both players have valid moves
			var whiteFigure = CreateFigure(isBlack: false);
			var blackFigure = CreateFigure(isBlack: true);
			
			_board[2, 3].SetFigure(whiteFigure);
			_board[5, 4].SetFigure(blackFigure);
			// Note: _board and _points reference the same objects, no need to set twice

			// Act
			var state = CheckersBasics.CheckGameState(_board, _points);

			// Assert
			Assert.AreEqual(GameState.Playing, state, "When both players have valid moves, game should continue");
		}

		#endregion

		#region Edge Cases and Boundary Tests

		[Test]
		public void GetAvailableSimpleMoves_PieceAtTopRow_WhiteHasNoMoves()
		{
			// Arrange: White piece at top row (can't move forward anymore)
			var whiteFigure = CreateFigure(isBlack: false);
			_board[7, 3].SetFigure(whiteFigure);

			// Act
			var moves = CheckersBasics.GetAvailableSimpleMoves(_board, _board[7, 3]);

			// Assert
			Assert.AreEqual(0, moves.Count, "White piece at top row should have no forward moves");
		}

		[Test]
		public void GetAvailableSimpleMoves_PieceAtBottomRow_BlackHasNoMoves()
		{
			// Arrange: Black piece at bottom row (can't move forward anymore)
			var blackFigure = CreateFigure(isBlack: true);
			_board[0, 3].SetFigure(blackFigure);

			// Act
			var moves = CheckersBasics.GetAvailableSimpleMoves(_board, _board[0, 3]);

			// Assert
			Assert.AreEqual(0, moves.Count, "Black piece at bottom row should have no forward moves");
		}

		[Test]
		public void GetAvailableAttacksDictionary_RegularPieceCanAttackBackward_ReturnsAttack()
		{
			// Regular pieces CAN attack backward in all directions
			// Arrange: White at (3, 3), black enemy at (2, 2) (backward-left)
			var whiteFigure = CreateFigure(isBlack: false);
			var blackFigure = CreateFigure(isBlack: true);
			
			_board[3, 3].SetFigure(whiteFigure);
			_board[2, 2].SetFigure(blackFigure);

			// Act
			var attacks = CheckersBasics.GetAvailableAttacksDictionary(_board, _board[3, 3]);

			// Assert
			Assert.AreEqual(1, attacks.Count, "Regular piece should be able to attack backward");
			var attackData = attacks.First().Value;
			Assert.AreEqual(_board[1, 1], attackData.FinalPosition);
		}

		#endregion
	}
}
