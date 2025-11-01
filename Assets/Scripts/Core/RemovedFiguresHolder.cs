using DG.Tweening;
using Gameplay;
using UnityEngine;

namespace Core
{
    public class RemovedFiguresHolder : MonoBehaviour
    {
        [SerializeField] private Transform _whiteRoot;
        [SerializeField] private Transform _blackRoot;
        [SerializeField] private float _offsetX = 0.3f;

        private int _whiteCount = 0;
        private int _blackCount = 0;

        public void Clear()
        {
            _whiteCount = 0;
            _blackCount = 0;
            foreach (Transform piece in _whiteRoot)
                Destroy(piece.gameObject);

            foreach (Transform piece in _blackRoot)
                Destroy(piece.gameObject);
        }

        public void AddPiece(Figure piece)
        {
            if (piece.IsBlack)
            {
                AddBlackPiece(piece.transform);
            }
            else
            {
                AddWhitePiece(piece.transform);
            }
        }

        private void AddWhitePiece(Transform pieceTransform)
        {
            _whiteCount++;
            pieceTransform.SetParent(_whiteRoot);
            pieceTransform.DOLocalMove(new Vector3(_whiteCount * _offsetX, 0, 0), 0.5f).OnComplete(() =>
            {
                pieceTransform.DOScale(Vector3.one, .2f).SetEase(Ease.InOutCubic);
            });
        }

        private void AddBlackPiece(Transform pieceTransform)
        {
            _blackCount++;
            pieceTransform.SetParent(_blackRoot);
            pieceTransform.DOLocalMove(new Vector3(_blackCount * _offsetX, 0, 0), 0.5f).OnComplete(() =>
            {
                pieceTransform.DOScale(Vector3.one, .2f).SetEase(Ease.InOutCubic);
            });
        }
    }
}