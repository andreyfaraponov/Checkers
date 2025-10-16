using System;
using DG.Tweening;
using Game.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	public class DifficultySelector : MonoBehaviour
	{
		public event Action GameStartEvent;

		[Header("UI Elements")]
		[SerializeField]
		private CanvasGroup _canvasGroup;

		[SerializeField]
		private Button _easyButton;

		[SerializeField]
		private Button _mediumButton;

		[SerializeField]
		private Button _hardButton;

		[SerializeField]
		private Button _startButton;

		[Header("Visual Feedback")]
		[SerializeField]
		private Color _selectedColor = new Color(0.2f, 0.8f, 0.2f);

		[SerializeField]
		private Color _normalColor = Color.white;

		private Difficulty _selectedDifficulty = Difficulty.Medium;
		private Image _easyButtonImage;
		private Image _mediumButtonImage;
		private Image _hardButtonImage;

		private void Awake()
		{
			_easyButtonImage = _easyButton.GetComponent<Image>();
			_mediumButtonImage = _mediumButton.GetComponent<Image>();
			_hardButtonImage = _hardButton.GetComponent<Image>();

			_easyButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Easy));
			_mediumButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Medium));
			_hardButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Hard));
			_startButton.onClick.AddListener(OnStartButtonClick);

			UpdateButtonVisuals();
		}

		private void SelectDifficulty(Difficulty difficulty)
		{
			_selectedDifficulty = difficulty;
			UpdateButtonVisuals();
		}

		private void UpdateButtonVisuals()
		{
			_easyButtonImage.color = _selectedDifficulty == Difficulty.Easy ? _selectedColor : _normalColor;
			_mediumButtonImage.color = _selectedDifficulty == Difficulty.Medium ? _selectedColor : _normalColor;
			_hardButtonImage.color = _selectedDifficulty == Difficulty.Hard ? _selectedColor : _normalColor;

			// Scale selected button slightly
			float selectedScale = 1.1f;
			float normalScale = 1.0f;

			_easyButton.transform.localScale = Vector3.one * (_selectedDifficulty == Difficulty.Easy ? selectedScale : normalScale);
			_mediumButton.transform.localScale = Vector3.one * (_selectedDifficulty == Difficulty.Medium ? selectedScale : normalScale);
			_hardButton.transform.localScale = Vector3.one * (_selectedDifficulty == Difficulty.Hard ? selectedScale : normalScale);
		}

		private void OnStartButtonClick()
		{
			_canvasGroup.interactable = false;
			_canvasGroup.DOFade(0, 0.3f).OnComplete(() =>
			{
				GameStartEvent?.Invoke();
				gameObject.SetActive(false);
			});
		}

		public void Show()
		{
			gameObject.SetActive(true);
			_canvasGroup.alpha = 0;
			_canvasGroup.interactable = false;
			_canvasGroup.DOFade(1, 0.3f).OnComplete(() => _canvasGroup.interactable = true);
		}

		public void Hide()
		{
			_canvasGroup.DOFade(0, 0.3f).OnComplete(() => gameObject.SetActive(false));
		}

		public Difficulty GetSelectedDifficulty() => _selectedDifficulty;
	}
}
