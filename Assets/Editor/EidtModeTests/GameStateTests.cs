using NUnit.Framework;
using Utils;

namespace Editor.EidtModeTests
{
	[TestFixture]
	public class GameStateTests
	{
		[Test]
		public void GameState_HasAllExpectedValues()
		{
			// Assert all enum values exist
			Assert.IsTrue(System.Enum.IsDefined(typeof(GameState), GameState.Playing));
			Assert.IsTrue(System.Enum.IsDefined(typeof(GameState), GameState.PlayerWin));
			Assert.IsTrue(System.Enum.IsDefined(typeof(GameState), GameState.OpponentWin));
			Assert.IsTrue(System.Enum.IsDefined(typeof(GameState), GameState.Draw));
		}

		[Test]
		public void GameState_HasCorrectCount()
		{
			// Assert there are exactly 4 values
			var values = System.Enum.GetValues(typeof(GameState));
			Assert.AreEqual(4, values.Length, "GameState should have exactly 4 values");
		}

		[Test]
		public void GameState_CanBeCompared()
		{
			// Arrange & Act
			var state1 = GameState.Playing;
			var state2 = GameState.Playing;
			var state3 = GameState.PlayerWin;

			// Assert
			Assert.AreEqual(state1, state2);
			Assert.AreNotEqual(state1, state3);
		}

		[Test]
		public void GameState_CanBeUsedInSwitchStatement()
		{
			// Arrange
			var state = GameState.PlayerWin;
			string result = "";

			// Act
			switch (state)
			{
				case GameState.Playing:
					result = "Playing";
					break;
				case GameState.PlayerWin:
					result = "PlayerWin";
					break;
				case GameState.OpponentWin:
					result = "OpponentWin";
					break;
				case GameState.Draw:
					result = "Draw";
					break;
			}

			// Assert
			Assert.AreEqual("PlayerWin", result);
		}

		[Test]
		public void GameState_ToString_ReturnsName()
		{
			// Arrange & Act
			var playingName = GameState.Playing.ToString();
			var playerWinName = GameState.PlayerWin.ToString();
			var opponentWinName = GameState.OpponentWin.ToString();
			var drawName = GameState.Draw.ToString();

			// Assert
			Assert.AreEqual("Playing", playingName);
			Assert.AreEqual("PlayerWin", playerWinName);
			Assert.AreEqual("OpponentWin", opponentWinName);
			Assert.AreEqual("Draw", drawName);
		}
	}
}
