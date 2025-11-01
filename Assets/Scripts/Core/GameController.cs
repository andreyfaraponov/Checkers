using Controllers;
using Controllers.AI;
using Cysharp.Threading.Tasks;
using Gameplay;
using UI;
using UnityEngine;
using Utils;

namespace Core
{
	public class GameController : MonoBehaviour
	{
		[SerializeField]
		private BoardController _boardController;

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
			_boardController.FigureAttackedEvent += FigureAttackedHandler;
			ShowDifficultySelector();
		}

		private void FigureAttackedHandler(Figure eliminatedFigure)
		{
			if (eliminatedFigure.IsBlack)
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
			_boardController.RefreshBoard();
			_boardController.SpawnInitialFigures();
			_player = new PlayerWithInputController(_boardController);
			_guiController.UpdateScore(_playerScore, _opponentScore);
			_opponent = CreateOpponent(isBlack: true);
			StartGameLoopAsync().Forget();
		}

		private IPlayerController CreateOpponent(bool isBlack)
		{
			return _currentDifficulty switch
			{
				Difficulty.Easy => new EasyBotController(isBlack, _boardController),
//				Difficulty.Medium => new MediumBotController(_boardController.CurrentBoard, _boardController.Points, _boardController),
//				Difficulty.Hard => new HardBotController(_boardController.CurrentBoard, _boardController.Points, _boardController),
//				_ => new MediumBotController(_boardController.CurrentBoard, _boardController.Points, _boardController)
			};
		}

		private async UniTask StartGameLoopAsync()
		{
			Debug.Log($"StartGameLoopAsync: {_gameState}");
			while (_gameState == GameState.Playing)
			{
				_guiController.ShowTurn(isUserTurn: true);
				await UniTask.Delay(500);
                _player.EnableInput(true);
				await _player.AwaitMove();
                _player.EnableInput(false);
                CheckGameState(isBlackTurn: true);

                if (_gameState != GameState.Playing)
					break;

				_guiController.ShowTurn(isUserTurn: false);
				await UniTask.Delay(500);
                _opponent.EnableInput(true);
				await _opponent.AwaitMove();
                _opponent.EnableInput(false);
				CheckGameState(isBlackTurn: false);
			}

			DisplayGameEnd();
		}

		private void CheckGameState(bool isBlackTurn)
		{
			_gameState = CheckersBasics.CheckGameState(_boardController.CurrentBoard, isBlackTurn);

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