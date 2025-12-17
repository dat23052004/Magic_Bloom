using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TubeView : MonoBehaviour
{
    public TubeModel model;

    [SerializeField] private Transform segmentsRoot;
    [SerializeField] public Transform pourPoint;
    [SerializeField] public Transform receivePoint;
    [SerializeField] private SpriteRenderer segmentPrefab;
    [SerializeField] private SpriteRenderer waterRect;

    [Range(0f, 0.5f)]
    [SerializeField] private float bottomBoost = 0.30f;

    List<SpriteRenderer> segmentViews = new();
    private Sequence seq;
    private Tween topTween;
    public void Init(TubeModel m)
    {
        model = m;
        EnsureViewCount();
        BuildSements();
    }
    public void BuildSements()
    {
        if (model == null || waterRect == null) return;
        
        float innerH = waterRect.bounds.size.y;
        float innerW = waterRect.bounds.size.x;

        float bottomY = waterRect.transform.localPosition.y - innerH * 0.5f;

        for (int i = 0; i < segmentViews.Count; i++)
            segmentViews[i].gameObject.SetActive(false);

        // Nếu trống: giữ 1 segment dưới đáy bottomY
        if (model.segments.Count == 0)
        {
            var sr = segmentViews[0];
            sr.gameObject.SetActive(true);

            sr.size = new Vector2(innerW, 0f);
            sr.transform.localPosition = new Vector3(0f, bottomY, 0f);

            var c = sr.color;
            c.a = 0f;
            sr.color = c;
            return;
        }

        // 1) Build layer heights (chỉ bù đáy)
        int N = model.capacity;
        float ideal = innerH / N;
        float bottomH = ideal * (1+bottomBoost);             // boost đáy
        float otherH = (innerH - bottomH) / (N - 1);

        float[] layerH = new float[N];
        layerH[0] = bottomH;
        for (int i = 1; i < N; i++) layerH[i] = otherH;

        // 2) Render segments
        float yCursor = bottomY;
        int filledUnits = 0;
        int viewIndex = 0;

        foreach (var seg in model.segments)
        {
            if (viewIndex >= segmentViews.Count) break;

            float segH = 0f;
            for (int i = 0; i < seg.Amount; i++)
                segH += layerH[filledUnits + i];

            var sr = segmentViews[viewIndex++];
            sr.gameObject.SetActive(true);
            sr.size = new Vector2(innerW, segH);
            sr.transform.localPosition =
                new Vector3(0f, yCursor + segH * 0.5f, 0f);
            sr.color = ColorPalette.GetColor(seg.colorId);

            yCursor += segH;
            filledUnits += seg.Amount;
        }
    }
    private void EnsureViewCount()
    {
        int need = Mathf.Max(1, model.segments.Count);

        while (segmentViews.Count < need)
        {
            var sr = Instantiate(segmentPrefab, segmentsRoot);
            sr.name = $"Segment_{segmentViews.Count}";
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.gameObject.SetActive(false);
            segmentViews.Add(sr);
        }

        for (int i = segmentViews.Count - 1; i >= need; i--)
        {
            Destroy(segmentViews[i].gameObject);
            segmentViews.RemoveAt(i);
        }
    }

    internal void AnimateTopOnly(int topIndexBefore, int amountDelta, float dur)
    {
        if (model == null || waterRect == null) return;
        if (segmentViews == null || segmentViews.Count == 0) return;
        if (amountDelta == 0) return;

        if (topTween != null && topTween.IsActive()) topTween.Kill();

        float innerH = waterRect.bounds.size.y;
        float innerW = waterRect.bounds.size.x;
        float unitH = innerH / model.capacity;
        float bottomY = waterRect.transform.localPosition.y - innerH * 0.5f;

        // Clamp indexBefore theo pool
        topIndexBefore = Mathf.Clamp(topIndexBefore, 0, segmentViews.Count - 1);
        var sr = segmentViews[topIndexBefore];

        // ===== CASE A: FROM (amountDelta < 0) =====
        // animate segment cũ (top trước khi pop)
        if (amountDelta < 0)
        {
            float startH = sr.size.y;
            float endH = Mathf.Max(0f, startH + unitH * amountDelta);

            // belowUnits của top BEFORE = tổng unit dưới nó.
            // Với FROM sau khi TryPour đã pop top, nên filledAmount hiện tại = beforeBelowUnits.
            // => bottomOfTop chính là đáy + (filledAmount hiện tại)*unitH
            int belowUnitsBefore = model.filledAmount; // sau pop
            float bottomOfTop = bottomY + belowUnitsBefore * unitH;

            float startY = bottomOfTop + startH * 0.5f;
            float endY = bottomOfTop + endH * 0.5f;

            sr.size = new Vector2(innerW, startH);

            Sequence s = DOTween.Sequence();
            s.Join(DOTween.To(() => startH, v =>
            {
                var size = sr.size; size.y = v; sr.size = size;
            }, endH, dur).SetEase(Ease.Linear));

            s.Join(DOTween.To(() => startY, v =>
            {
                var pos = sr.transform.localPosition; pos.y = v; sr.transform.localPosition = pos;
            }, endY, dur).SetEase(Ease.Linear));

            if (endH <= 0.0001f)
                s.OnComplete(() => sr.gameObject.SetActive(false));

            topTween = s;
            return;
        }

        // ===== CASE B: TO (amountDelta > 0) =====
        // Sau TryPour: TO có thể merge vào top cũ hoặc add segment mới.
        // Nếu add segment mới, topIndexAfter = topIndexBefore + 1.
        int topIndexAfter = model.segments.Count - 1;
        topIndexAfter = Mathf.Clamp(topIndexAfter, 0, segmentViews.Count - 1);

        var srAfter = segmentViews[topIndexAfter];
        srAfter.gameObject.SetActive(true);
        srAfter.drawMode = SpriteDrawMode.Sliced;

        // set màu theo top AFTER (luôn đúng)
        var topSegAfter = model.segments[model.segments.Count - 1];
        srAfter.color = ColorPalette.GetColor(topSegAfter.colorId);

        bool addedNewSegment = (topIndexAfter != topIndexBefore);

        float startH_to = addedNewSegment ? 0f : srAfter.size.y;
        float endH_to = startH_to + unitH * amountDelta;

        // belowUnits của top AFTER = filledAmount - topAmountAfter
        int belowUnitsAfter = model.filledAmount - topSegAfter.Amount;
        float bottomOfTopAfter = bottomY + belowUnitsAfter * unitH;

        float startY_to = bottomOfTopAfter + startH_to * 0.5f;
        float endY_to = bottomOfTopAfter + endH_to * 0.5f;

        srAfter.size = new Vector2(innerW, startH_to);
        srAfter.transform.localPosition = new Vector3(0f, startY_to, 0f);

        Sequence s2 = DOTween.Sequence();
        s2.Join(DOTween.To(() => startH_to, v =>
        {
            var size = srAfter.size; size.y = v; srAfter.size = size;
        }, endH_to, dur).SetEase(Ease.Linear));

        s2.Join(DOTween.To(() => startY_to, v =>
        {
            var pos = srAfter.transform.localPosition; pos.y = v; srAfter.transform.localPosition = pos;
        }, endY_to, dur).SetEase(Ease.Linear));

        topTween = s2;
    }

}
