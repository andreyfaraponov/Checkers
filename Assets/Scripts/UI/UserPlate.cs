using DG.Tweening;
using UnityEngine;

namespace Game.UI
{
	public class UserPlate : MonoBehaviour
	{
		private const float UpScaleValue = 1.7f;
		private const float DownScaleValue = 1.1f;

		[SerializeField]
		private RectTransform _plate;

		public void UpScale(bool isUserTurn) => 
			_plate.DOScale(Vector3.one * (isUserTurn ? UpScaleValue : DownScaleValue), 0.2f);
	}
}