using UnityEngine;

namespace Gameplay
{
	public class Figure : MonoBehaviour
	{
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

		public bool IsBlack { get; private set; }

		public bool IsQueen => BoardValue > 2;

		public int BoardValue { get; private set; }

		private void Awake()
		{
			BoardValue = 1;
			if (_queenCrown != null)
				_queenCrown.SetActive(IsQueen);
		}

		public void SetBlack()
		{
			IsBlack = true;
			BoardValue = 2;
			
			if (!Application.isPlaying)
				return;
			
			_meshRenderer.material = _black;
			_queenCrownRenderer.material = _black;
		}

		public void SetQueen()
		{
			if (BoardValue > 2)
				return;
			
			BoardValue += 2;

			if (_queenCrown != null && Application.isPlaying)
				_queenCrown.SetActive(true);
		}
	}
}