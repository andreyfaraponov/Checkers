using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ResultsPopup : MonoBehaviour
	{
		public event Action PlayOneMoreGameEvent;

		[SerializeField]
		private CanvasGroup _canvasGroup;

		[SerializeField]
		private TMP_Text _titleText;

		[SerializeField]
		private TMP_Text _resultText;

		[SerializeField]
		private Button _playAgainButton;

		private void Awake()
		{
			_playAgainButton.onClick.AddListener(PlayAgainClick);
		}

		private void PlayAgainClick()
		{
			_canvasGroup.interactable = false;
			_canvasGroup.DOFade(0, 0.5f).OnComplete(() => PlayOneMoreGameEvent?.Invoke());
		}

		public void ShowResult(string title, string result)
		{
			_titleText.text = title;
			_resultText.text = result;

			_canvasGroup.alpha = 0;
			_canvasGroup.interactable = false;
			_canvasGroup.gameObject.SetActive(true);
			_canvasGroup.DOFade(1, 0.5f).OnComplete(() => _canvasGroup.interactable = true);
		}

		public void Hide()
		{
			_canvasGroup.gameObject.SetActive(false);
		}
	}
}