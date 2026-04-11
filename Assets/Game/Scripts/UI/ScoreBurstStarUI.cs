using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScoreBurstStarUI : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;

    private Sequence activeSequence;

    public RectTransform RectTransform => rectTransform != null ? rectTransform : transform as RectTransform;

    private void Reset()
    {
        CacheReferences();
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void OnValidate()
    {
        CacheReferences();
    }

    public void Prepare(RectTransform parent, Vector2 anchoredPosition, float size, Color tint)
    {
        CacheReferences();
        StopAnimation();

        RectTransform.SetParent(parent, false);
        RectTransform.anchoredPosition = anchoredPosition;
        RectTransform.sizeDelta = Vector2.one * size;
        RectTransform.localScale = Vector3.zero;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (iconImage != null)
        {
            iconImage.color = tint;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        gameObject.SetActive(true);
    }

    public void PlayBurst(
        Vector2 scatterPoint,
        Vector2 controlPoint,
        Vector2 targetPoint,
        float startDelay,
        float scatterDuration,
        float flyDuration,
        Action onComplete)
    {
        StopAnimation();

        activeSequence = DOTween.Sequence();
        activeSequence.SetTarget(RectTransform);
        activeSequence.AppendInterval(startDelay);
        activeSequence.Append(RectTransform.DOScale(1f, scatterDuration).SetEase(Ease.OutBack));
        activeSequence.Join(RectTransform.DOAnchorPos(scatterPoint, scatterDuration).SetEase(Ease.OutQuad));

        if (canvasGroup != null)
        {
            activeSequence.Join(canvasGroup.DOFade(1f, scatterDuration));
        }

        activeSequence.Append(DOVirtual.Float(0f, 1f, flyDuration, value =>
        {
            RectTransform.anchoredPosition = EvaluateQuadraticBezier(scatterPoint, controlPoint, targetPoint, value);
        }).SetEase(Ease.InQuad));
        activeSequence.Join(RectTransform.DOScale(0.72f, flyDuration).SetEase(Ease.InQuad));

        if (canvasGroup != null)
        {
            activeSequence.Join(canvasGroup.DOFade(0.75f, flyDuration));
        }

        activeSequence.OnComplete(() =>
        {
            activeSequence = null;
            onComplete?.Invoke();
        });
    }

    public void ResetForPool()
    {
        CacheReferences();
        StopAnimation();

        RectTransform.anchoredPosition = Vector2.zero;
        RectTransform.localScale = Vector3.one;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        gameObject.SetActive(false);
    }

    public void StopAnimation()
    {
        if (activeSequence != null && activeSequence.IsActive())
        {
            activeSequence.Kill();
        }

        activeSequence = null;
        RectTransform.DOKill();

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }
    }

    private void CacheReferences()
    {
        rectTransform ??= transform as RectTransform;
        canvasGroup ??= GetComponent<CanvasGroup>();
        iconImage ??= GetComponentInChildren<Image>(true);
    }

    private static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        float u = 1f - t;
        return u * u * start + 2f * u * t * control + t * t * end;
    }
}
