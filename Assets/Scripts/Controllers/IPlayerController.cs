using Cysharp.Threading.Tasks;

namespace Game.Controllers
{
	public interface IPlayerController
	{
		UniTask AwaitMove();
	}
}