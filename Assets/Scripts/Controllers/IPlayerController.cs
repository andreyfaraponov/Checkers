using Cysharp.Threading.Tasks;

namespace Controllers
{
	public interface IPlayerController
	{
		UniTask AwaitMove();
        void EnableInput(bool enable);
    }
}