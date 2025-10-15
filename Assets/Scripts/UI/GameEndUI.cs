using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	/// <summary>
	/// Simple UI component for displaying game end state.
	/// Attach this to a UI Panel with a TextMeshProUGUI child and a Button for restart.
	/// </summary>
	public class GameEndUI : MonoBehaviour
	{
		[SerializeField]
		private TMPro.TextMeshProUGUI _messageText;

		[SerializeField]
		private Button _restartButton;

		[SerializeField]
		private GameController _gameController;

		private void Awake()
		{
			if (_restartButton != null && _gameController != null)
			{
				_restartButton.onClick.AddListener(_gameController.RestartGame);
			}
		}

		public void SetMessage(string message)
		{
			if (_messageText != null)
				_messageText.text = message;
		}

		private void OnDestroy()
		{
			if (_restartButton != null)
			{
				_restartButton.onClick.RemoveAllListeners();
			}
		}
	}
}
