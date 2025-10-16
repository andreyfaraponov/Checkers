using Controllers;
using Controllers.AI;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using Utils;

namespace Core
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

		[SerializeField]
		private DifficultySelector _difficultySelector;

		private IPlayerController _player;
		private IPlayerController _opponent;

		private GameState _gameState = GameState.Playing;
		private Difficulty _currentDifficulty = Difficulty.Medium;
		private int _playerScore = 0;
		private int _opponentScore = 0;

		// Start is called once before the first execution of Update after the MonoBehaviour is created
		private async void Start()
		{
			Debug.Log($"GameController Start: {_mainCamera.fieldOfView}");
			await UniTask.Delay(1000);
			_mainCamera.fieldOfView = 37;
			Application.targetFrameRate = 60;
			_guiController.PlayOneMoreGameEvent += ShowDifficultySelector;
			_difficultySelector.GameStartEvent += OnDifficultySelected;
			_board.FigureAttackedEvent += FigureAttackedHandler;
			ShowDifficultySelector();
		}

		private void FigureAttackedHandler(bool isBlackFigure)
		{
			if (isBlackFigure)
				_playerScore++;
			else
				_opponentScore++;
			
			_guiController.UpdateScore(_playerScore, _opponentScore);
		}

		private void ShowDifficultySelector()
		{
			_guiController.HideAll();
			_difficultySelector.Show();
		}

		private void OnDifficultySelected()
		{
			_currentDifficulty = _difficultySelector.GetSelectedDifficulty();
			_guiController.SetDifficulty(_currentDifficulty);
			_guiController.SetDifficultyBotPlate(_currentDifficulty);
			_difficultySelector.Hide();
			StartGame();
		}

		private void StartGame()
		{
			_gameState = GameState.Playing;
			_guiController.HideAll();
			_playerScore = 0;
			_opponentScore = 0;
			_board.RefreshBoard();
			_board.LocateFigures();
			_player = new PlayerWithInputController(_board.CurrentBoard,
				_board.Points, _board);
			_guiController.UpdateScore(_playerScore, _opponentScore);
			_opponent = CreateOpponent();
			StartGameLoopAsync().Forget();
		}

		private IPlayerController CreateOpponent()
		{
			return _currentDifficulty switch
			{
				Difficulty.Easy => new EasyBotController(_board.CurrentBoard, _board.Points, _board),
				Difficulty.Medium => new MediumBotController(_board.CurrentBoard, _board.Points, _board),
				Difficulty.Hard => new HardBotController(_board.CurrentBoard, _board.Points, _board),
				_ => new MediumBotController(_board.CurrentBoard, _board.Points, _board)
			};
		}

		private async UniTask StartGameLoopAsync()
		{
			Debug.Log($"StartGameLoopAsync: {_gameState}");
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

			_guiController.ShowGameResults(_gameState == GameState.PlayerWin, _playerScore, _opponentScore);
		}

		public void RestartGame()
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(
				UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
		}
	}
}