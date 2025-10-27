using System.Collections.Generic;
using DG.Tweening;
using Gameplay;
using UnityEngine;

namespace Core
{
	public class RemovedFiguresHolder : MonoBehaviour
	{
		[SerializeField]
		private Transform _whiteRoot;

		[SerializeField]
		private Transform _blackRoot;

		[SerializeField]
		private float _offsetX = 0.3f;

		private readonly List<Transform> _whitePieces = new();
		private readonly List<Transform> _blackPieces = new();
		
		public void Clear()
		{
			foreach (var piece in _whitePieces) 
				Destroy(piece.gameObject);
			
			_whitePieces.Clear();

			foreach (var piece in _blackPieces) 
				Destroy(piece.gameObject);
			
			_blackPieces.Clear();
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
			_whitePieces.Add(pieceTransform);
			pieceTransform.SetParent(_whiteRoot);
			pieceTransform.DOLocalMove(new Vector3(_whitePieces.Count * _offsetX, 0, 0), 0.5f);
		}

		private void AddBlackPiece(Transform pieceTransform)
		{
			_blackPieces.Add(pieceTransform);
			pieceTransform.SetParent(_blackRoot);
			pieceTransform.DOLocalMove(new Vector3(_blackPieces.Count * _offsetX, 0, 0), 0.5f);
		}
	}
}