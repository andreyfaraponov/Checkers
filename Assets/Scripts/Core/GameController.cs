using Cysharp.Threading.Tasks;
using Game.Controllers;
using Game.Controllers.AI;
using Game.UI;
using Game.Utils;
using UnityEngine;

namespace Game.Core
{
	public class GameController : MonoBehaviour
	{
		[SerializeField]
		private Board _board;

		[SerializeField]
		private Camera _mainCamera;

		[Header("UI")]
		[SerializeField]
		private GuiController _guiController;

		private IPlayerController _player;
		private IPlayerController _opponent;

		private GameState _gameState = GameState.Playing;

		// Start is called once before the first execution of Update after the MonoBehaviour is created
		private async void Start()
		{
			Debug.Log($"GameController Start: {_mainCamera.fieldOfView}");
			await UniTask.Delay(1000);
			_mainCamera.fieldOfView = 37;
			Application.targetFrameRate = 60;
			_guiController.PlayOneMoreGameEvent += StartGame;
			StartGame();
		}

		private void StartGame()
		{
			_gameState = GameState.Playing;
			_guiController.HideAll();
			_board.RefreshBoard();
			_board.LocateFigures();
			_player = new PlayerWithInputController(_board.CurrentBoard,
				_board.Points);
			_opponent = new HardBotController(_board.CurrentBoard, _board.Points);
			StartGameLoopAsync().Forget();
		}

		private async UniTask StartGameLoopAsync()
		{
			Debug.LogError($"StartGameLoopAsync: {_gameState}");
			while (_gameState == GameState.Playing)
			{
				_guiController.ShowTurn(isUserTurn: true);
				await UniTask.Delay(500);
				await _player.AwaitMove();
				CheckGameState();

				if (_gameState != GameState.Playing)
					break;

				_guiController.ShowTurn(isUserTurn: false);
				await UniTask.Delay(500);
				await _opponent.AwaitMove();
				CheckGameState();
			}

			DisplayGameEnd();
		}

		private void CheckGameState()
		{
			_gameState = CheckersBasics.CheckGameState(_board.CurrentBoard, _board.Points);

			if (_gameState != GameState.Playing)
			{
				Debug.Log($"Game Over! State: {_gameState}");
			}
		}

		private void DisplayGameEnd()
		{
			string message = _gameState switch
			{
				GameState.PlayerWin => "You Win!",
				GameState.OpponentWin => "You Lose!",
				GameState.Draw => "Draw!",
				_ => ""
			};

			Debug.Log($"Game End: {message}");

			_guiController.ShowGameResults(_gameState == GameState.PlayerWin, 0, 0);
		}

		public void RestartGame()
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(
				UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
		}
	}
}