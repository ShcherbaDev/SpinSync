using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
	[SerializeField] private CanvasGroup _canvasGroup;

	private void Reset()
	{
		_canvasGroup = GetComponent<CanvasGroup>();
	}

	private void Awake()
	{
		if (!_canvasGroup) _canvasGroup = GetComponent<CanvasGroup>();
		_canvasGroup.alpha = 0f;
		_canvasGroup.blocksRaycasts = false;
	}

	public Tween FadeToBlack(float duration)
	{
		_canvasGroup.blocksRaycasts = true;
		return _canvasGroup.DOFade(1f, duration);
	}

	public Tween FadeFromBlack(float duration)
	{
		return _canvasGroup.DOFade(0f, duration)
			.OnComplete(() => _canvasGroup.blocksRaycasts = false);
	}
}
