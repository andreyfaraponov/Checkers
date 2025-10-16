using System;
using Game.Utils;
using UnityEngine;

namespace Game.UI
{
	public class GuiController : MonoBehaviour
	{
		public event Action PlayOneMoreGameEvent;

		[SerializeField]
		private ResultsPopup _resultsPopup;

		[SerializeField]
		private TurnView _turnView;

		[SerializeField]
		private UserPlate _userPlate;

		[SerializeField]
		private UserPlate _computerPlate;

		private Difficulty _currentDifficulty = Difficulty.Medium;

		private void Awake() =>
			_resultsPopup.PlayOneMoreGameEvent += (() => PlayOneMoreGameEvent?.Invoke());

		public void SetDifficulty(Difficulty difficulty)
		{
			_currentDifficulty = difficulty;
		}

		public void ShowTurn(bool isUserTurn)
		{
			_userPlate.UpScale(isUserTurn);
			_computerPlate.UpScale(!isUserTurn);
			_turnView.ShowTurn(isUserTurn, _currentDifficulty);
		}

		public void SetDifficultyBotPlate(Difficulty difficulty) => 
			_computerPlate.SetBotDifficulty(difficulty);

		public void ShowGameResults(bool isUserWin,
			int userPoints,
			int computerPoints) =>
			_resultsPopup.ShowResult(isUserWin ? "ПЕРЕМОГА!" : "Програш...",
				$"Рахунок: \nБілі - {userPoints}\nЧорні - {computerPoints}");

		public void UpdateScore(int userPoints, int computerPoints)
		{
			_computerPlate.HealthUpdate(userPoints);
			_userPlate.HealthUpdate(computerPoints);
		}

		public void HideAll()
		{
			_resultsPopup.Hide();
			_turnView.Hide();
		}
	}
}