using DG.Tweening;
using Game.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class TurnView : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup _canvasGroup;
        
        [SerializeField]
        private TMP_Text _turnText;
        
        [SerializeField]
        private Image _background;

        [SerializeField]
        private Color _botBackground;
        
        [SerializeField]
        private Color _playerBackground;

        private Tween _tween;

        public void ShowTurn(bool isUserTurn, Difficulty difficulty = Difficulty.Medium)
        {
            _tween?.Complete();
            
            _background.color = isUserTurn
                ? _playerBackground
                : _botBackground;
            
            if (isUserTurn)
            {
                _turnText.text = "ТВІЙ ХІД!";
            }
            else
            {
                string difficultyText = difficulty switch
                {
                    Difficulty.Easy => "ЛЕГКИЙ",
                    Difficulty.Medium => "СЕРЕДНІЙ",
                    Difficulty.Hard => "ВАЖКИЙ",
                    _ => ""
                };
                _turnText.text = $"БОТ ({difficultyText})";
            }

            _tween = _canvasGroup.DOFade(1, 0.2f).OnComplete(() =>
            {
                _tween = _canvasGroup.DOFade(0, 1f);
            });
        }

        public void Hide()
        {
            _canvasGroup.DOFade(0, 0f);
        }
    }
}
