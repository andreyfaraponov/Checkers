using NUnit.Framework;
using Utils;

namespace Editor.EidtModeTests
{
	[TestFixture]
	public class AttackDataTests
	{
		[Test]
		public void AttackData_SetAndGetProperties_WorksCorrectly()
		{
			// Arrange
			var attackData = new AttackData();

			// Act & Assert - Properties should be settable and gettable
			Assert.DoesNotThrow(() => attackData.StartPosition = null);
			Assert.DoesNotThrow(() => attackData.AttackPosition = null);
			Assert.DoesNotThrow(() => attackData.FinalPosition = null);
			
			Assert.IsNull(attackData.StartPosition);
			Assert.IsNull(attackData.AttackPosition);
			Assert.IsNull(attackData.FinalPosition);
		}

		[Test]
		public void AttackData_CanBeCreatedAndUsedInDictionary_WorksCorrectly()
		{
			// Arrange
			var attackData = new AttackData
			{
				StartPosition = null,
				AttackPosition = null,
				FinalPosition = null
			};

			// Act & Assert - Should be usable in collections
			Assert.IsNotNull(attackData);
			Assert.IsInstanceOf<AttackData>(attackData);
		}
	}
}
