using System;
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

		private void Awake() => 
			_resultsPopup.PlayOneMoreGameEvent += (() => PlayOneMoreGameEvent?.Invoke() );

		public void ShowTurn(bool isUserTurn)
		{
			_userPlate.UpScale(isUserTurn);
			_computerPlate.UpScale(!isUserTurn);
			_turnView.ShowTurn(isUserTurn);
		}

		public void ShowGameResults(bool isUserWin, int userPoints, int computerPoints) => 
			_resultsPopup.ShowResult(isUserWin ? "ПЕРЕМОГА!" : "Програш...",$"Результат: {userPoints} - {computerPoints}");

		public void UpdateUserPoints(int points)
		{
			
		}
		
		public void UpdateComputerPoints(int points)
		{
			
		}

		public void HideAll()
		{
			_resultsPopup.Hide();
			_turnView.Hide();
		}
	}
}
