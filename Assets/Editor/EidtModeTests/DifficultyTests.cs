using NUnit.Framework;
using Utils;

namespace Editor.EidtModeTests
{
	[TestFixture]
	public class DifficultyTests
	{
		[Test]
		public void Difficulty_HasAllExpectedValues()
		{
			// Assert all enum values exist
			Assert.IsTrue(System.Enum.IsDefined(typeof(Difficulty), Difficulty.Easy));
			Assert.IsTrue(System.Enum.IsDefined(typeof(Difficulty), Difficulty.Medium));
			Assert.IsTrue(System.Enum.IsDefined(typeof(Difficulty), Difficulty.Hard));
		}

		[Test]
		public void Difficulty_HasCorrectCount()
		{
			// Assert there are exactly 3 values
			var values = System.Enum.GetValues(typeof(Difficulty));
			Assert.AreEqual(3, values.Length, "Difficulty should have exactly 3 values");
		}

		[Test]
		public void Difficulty_CanBeCompared()
		{
			// Arrange & Act
			var diff1 = Difficulty.Easy;
			var diff2 = Difficulty.Easy;
			var diff3 = Difficulty.Hard;

			// Assert
			Assert.AreEqual(diff1, diff2);
			Assert.AreNotEqual(diff1, diff3);
		}

		[Test]
		public void Difficulty_CanBeUsedInSwitchExpression()
		{
			// Arrange
			var difficulty = Difficulty.Medium;

			// Act
			var result = difficulty switch
			{
				Difficulty.Easy => "Easy",
				Difficulty.Medium => "Medium",
				Difficulty.Hard => "Hard",
				_ => "Unknown"
			};

			// Assert
			Assert.AreEqual("Medium", result);
		}

		[Test]
		public void Difficulty_ToString_ReturnsName()
		{
			// Arrange & Act
			var easyName = Difficulty.Easy.ToString();
			var mediumName = Difficulty.Medium.ToString();
			var hardName = Difficulty.Hard.ToString();

			// Assert
			Assert.AreEqual("Easy", easyName);
			Assert.AreEqual("Medium", mediumName);
			Assert.AreEqual("Hard", hardName);
		}

		[Test]
		public void Difficulty_CanBeCastToInt()
		{
			// Arrange & Act
			int easyValue = (int)Difficulty.Easy;
			int mediumValue = (int)Difficulty.Medium;
			int hardValue = (int)Difficulty.Hard;

			// Assert
			Assert.AreEqual(0, easyValue);
			Assert.AreEqual(1, mediumValue);
			Assert.AreEqual(2, hardValue);
		}

		[Test]
		public void Difficulty_CanBeCreatedFromInt()
		{
			// Arrange & Act
			var easy = (Difficulty)0;
			var medium = (Difficulty)1;
			var hard = (Difficulty)2;

			// Assert
			Assert.AreEqual(Difficulty.Easy, easy);
			Assert.AreEqual(Difficulty.Medium, medium);
			Assert.AreEqual(Difficulty.Hard, hard);
		}

		[Test]
		public void Difficulty_OrderIsCorrect()
		{
			// Assert difficulty values are in order of complexity
			Assert.Less((int)Difficulty.Easy, (int)Difficulty.Medium);
			Assert.Less((int)Difficulty.Medium, (int)Difficulty.Hard);
		}
	}
}
