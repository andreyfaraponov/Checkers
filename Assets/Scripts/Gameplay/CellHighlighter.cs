using DG.Tweening;
using UnityEngine;

namespace Gameplay
{
    public class CellHighlighter : MonoBehaviour
    {
        private const float Duration = .3f;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        
        [SerializeField] private MeshRenderer moveToCellHighlight;
        [SerializeField] private MeshRenderer selectPieceHighlight;

        [SerializeField, ColorUsage(false, true)]
        private Color attackColor;

        [SerializeField, ColorUsage(false, true)]
        private Color commonColor;

        public void HighlightMoveCell(bool isAttack)
        {
            moveToCellHighlight.gameObject.SetActive(true);
            selectPieceHighlight.gameObject.SetActive(false);
            moveToCellHighlight.material.SetColor(EmissionColor, isAttack ? attackColor : commonColor);
        }

        public void HighlightSelectCell(bool isToAttack)
        {
            moveToCellHighlight.gameObject.SetActive(false);
            selectPieceHighlight.gameObject.SetActive(true);
            selectPieceHighlight.material.SetColor(EmissionColor, isToAttack ? attackColor : commonColor);
        }

        public void ClearHighlight()
        {
            moveToCellHighlight.gameObject.SetActive(false);
            selectPieceHighlight.gameObject.SetActive(false);
        }
    }
}