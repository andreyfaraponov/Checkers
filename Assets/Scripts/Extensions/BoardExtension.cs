using Gameplay;

namespace Extensions
{
	public static class BoardExtension
	{
		public static void SetFigure(this int[,] board,
			int x,
			int y,
			Figure figure)
		{
			var figureValue = figure.IsBlack ? 2 : 1;
			figureValue += figure.IsQueen ? 2 : 0;
			board[y, x] = figureValue;
		}
	}
}