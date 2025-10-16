using System;
using DG.Tweening;
using Game.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	public class UserPlate : MonoBehaviour
	{
		private const float UpScaleValue = 1.7f;
		private const float DownScaleValue = 1.1f;
		private const int MaxScore = 12;

		[SerializeField]
		private RectTransform _plate;
		
		[SerializeField]
		private TMP_Text _scoreText;
		
		[SerializeField]
		private Slider _healthSlider;

		[SerializeField]
		private Slider _difficultySlider;

		public void UpScale(bool isUserTurn) => 
			_plate.DOScale(Vector3.one * (isUserTurn ? UpScaleValue : DownScaleValue), 0.2f);

		public void SetBotDifficulty(Difficulty difficulty)
		{
			switch (difficulty)
			{
				case Difficulty.Easy:
					_healthSlider.value = 0.1f;
					break;
				case Difficulty.Medium:
					_healthSlider.value = 0.5f;
					break;
				case Difficulty.Hard:
					_healthSlider.value = 1f;
					break;
			}
		}

		public void HealthUpdate(int figuresCaptured)
		{
			_healthSlider.value = MaxScore - figuresCaptured;
			_scoreText.text = $"{MaxScore - figuresCaptured}/{MaxScore}";
		}
	}
}