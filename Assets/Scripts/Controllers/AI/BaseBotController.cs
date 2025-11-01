using Core;
using Cysharp.Threading.Tasks;

namespace Controllers.AI
{
	public abstract class BaseBotController : IPlayerController
	{
		private readonly BoardController _boardController;

		private bool _lastAttackPossible;

		protected BoardController BoardController => _boardController;

		protected BaseBotController(BoardController boardController)
		{
			_boardController = boardController;
		}

		public async UniTask AwaitMove()
		{
			if (await MakeAttackAsync(_boardController.CurrentBoard))
				return;

			await MakeMoveAsync(_boardController.CurrentBoard);
		}

        public void EnableInput(bool enable)
        {
            // Bots do not require input enabling/disabling
        }

        protected abstract UniTask<bool> MakeAttackAsync(int[,] currentBoardState);
		protected abstract UniTask MakeMoveAsync(int[,] currentBoardState);

		protected async UniTask MakeBoardActionAsync(ScoredMove scoredMove)
		{
			if (scoredMove.IsAttack)
			{
				await _boardController.MakeAttackAsync(scoredMove.From, scoredMove.To,
					scoredMove.VictimPosition);
			}
			else
			{
				await _boardController.MakeMoveAsync(scoredMove.From, scoredMove.To);
			}
		}
	}
}