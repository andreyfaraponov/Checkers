using System;
using DG.Tweening;
using UnityEngine;

namespace Game.Gameplay
{
	public class PositionPoint : MonoBehaviour
	{
		public event Action<PositionPoint> PointClickEvent;
		
		[SerializeField]
		private MeshRenderer _meshRenderer;

		[SerializeField]
		private Material _blackMaterial;

		[SerializeField]
		private Material _whiteMaterial;

		[SerializeField]
		private GameObject _highlightObject;

		[SerializeField]
		private Figure _figure;

		public Figure Figure => _figure;

		public bool IsBlack { get; private set; }
		public int Y { get; private set; }
		public int X { get; private set; }

		private void Awake() =>
			_meshRenderer.material = _whiteMaterial;

		public void SetBlack()
		{
			_meshRenderer.material = _blackMaterial;
			IsBlack = true;
		}

		public void SetFigure(Figure figure)
		{
			_figure = figure;
			if (_figure != null)
				figure.transform.DOMove(transform.position, 0.3f);
		}

		public void Highlight(bool highlight) =>
			_highlightObject.SetActive(highlight);

		private void OnMouseDown() => 
			PointClickEvent?.Invoke(this);

		public void SetPosition(int x, int y)
		{
			X = x;
			Y = y;
		}
	}
}