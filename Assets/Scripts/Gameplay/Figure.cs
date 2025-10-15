using System;
using UnityEngine;

namespace Game.Gameplay
{
	public class Figure : MonoBehaviour
	{
		public event Action<Figure> PickFigureEvent;
		
		[SerializeField]
		private MeshRenderer _meshRenderer;
		
		[SerializeField]
		private MeshRenderer _queenCrownRenderer;

		[SerializeField]
		private Material _white;

		[SerializeField]
		private Material _black;

		[SerializeField]
		private GameObject _queenCrown;

		private bool _isQueen;

		public bool IsBlack { get; private set; }
		public bool IsKnockedOut { get; private set; } = false;

		public bool IsQueen => _isQueen;

		private void Awake()
		{
			if (_queenCrown != null)
				_queenCrown.SetActive(_isQueen);
		}

		public void SetBlack()
		{
			_meshRenderer.material = _black;
			_queenCrownRenderer.material = _black;
			IsBlack = true;
		}

		public void SetQueen()
		{
			_isQueen = true;
			if (_queenCrown != null)
				_queenCrown.SetActive(true);
		}

		private void OnMouseDown()
		{
			if (IsKnockedOut)
				return;
			
			PickFigureEvent?.Invoke(this);
		}
	}
}