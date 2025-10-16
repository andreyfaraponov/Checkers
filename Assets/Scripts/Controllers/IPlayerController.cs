using Cysharp.Threading.Tasks;

namespace Controllers
{
	public interface IPlayerController
	{
		UniTask AwaitMove();
	}
}