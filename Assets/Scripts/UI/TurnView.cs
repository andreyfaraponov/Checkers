using DG.Tweening;
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

        public void ShowTurn(bool isUserTurn)
        {
            _tween?.Complete();
            
            _background.color = isUserTurn
                ? _playerBackground
                : _botBackground;
            
            _turnText.text = isUserTurn
                ? "ТВІЙ ХІД!"
                : "ХОДИТЬ БОТ";

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
