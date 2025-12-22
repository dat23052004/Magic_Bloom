using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
    private Tween topTween;

    [SerializeField] private SortingGroup sortingGroup;

    private int orderBoost = 2;
    private int orderDefault = 0;

    public void Init(TubeModel m)
    {
        model = m;
        EnsureViewCount();
        BuildSements();
    }
    public void BuildSements()
    {
        if (model == null || waterRect == null) return;

        float innerH = waterRect.size.y;
        float innerW = waterRect.size.x;

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

        if (topTween != null && topTween.IsActive())
            topTween.Kill();

        // 1) LẤY KÍCH THƯỚC LOCAL CHUẨN
        float innerH = waterRect.size.y;
        float innerW = waterRect.size.x;
        float bottomY = waterRect.transform.localPosition.y - innerH * 0.5f;

        // 2) TÍNH layerH[] (GIỐNG BUILD)
        int N = model.capacity;
        float ideal = innerH / N;
        float bottomH = ideal * (1f + bottomBoost);
        float otherH = (innerH - bottomH) / (N - 1);

        float[] layerH = new float[N];
        layerH[0] = bottomH;
        for (int i = 1; i < N; i++)
            layerH[i] = otherH;

        // Helper: cộng chiều cao theo unit
        float SumUnits(int startUnit, int count)
        {
            float h = 0f;
            for (int i = 0; i < count; i++)
                h += layerH[startUnit + i];
            return h;
        }

        // CASE A: FROM (amountDelta < 0)
        if (amountDelta < 0)
        {


            topIndexBefore = Mathf.Clamp(topIndexBefore, 0, segmentViews.Count - 1);
            var sr = segmentViews[topIndexBefore];

            int pouredUnits = -amountDelta;

            float startH = sr.size.y;

            // tổng unit BEFORE = sau pour + đã rót
            int filledUnitsBefore = model.filledAmount + pouredUnits;

            // các unit bị rót nằm ở đỉnh segment
            int startUnit = filledUnitsBefore - pouredUnits;

            float reduceH = 0f;
            for (int i = 0; i < pouredUnits; i++)
                reduceH += layerH[startUnit + i];

            float endH = Mathf.Max(0f, startH - reduceH);

            float bottomOfTop1 = bottomY + SumUnits(0, model.filledAmount);

            float startY = bottomOfTop1 + startH * 0.5f;
            float endY = bottomOfTop1 + endH * 0.5f;


            Sequence s = DOTween.Sequence();

            s.Join(DOTween.To(() => startH, v =>
            {
                sr.size = new Vector2(innerW, v);
            }, endH, dur).SetEase(Ease.Linear));

            s.Join(DOTween.To(() => startY, v =>
            {
                var p = sr.transform.localPosition;
                p.y = v;
                sr.transform.localPosition = p;
            }, endY, dur).SetEase(Ease.Linear));

                s.OnComplete(() => {
                    sr.size = new Vector2(innerW, 0f);
                    sr.gameObject.SetActive(false);
                }); 
            topTween = s;
            return;
        }

        // CASE B: TO (amountDelta > 0)
        int topIndexAfter = model.segments.Count - 1;
        topIndexAfter = Mathf.Clamp(topIndexAfter, 0, segmentViews.Count - 1);

        var srAfter = segmentViews[topIndexAfter];
        srAfter.gameObject.SetActive(true);
        srAfter.drawMode = SpriteDrawMode.Sliced;

        var topSeg = model.segments[topIndexAfter];
        srAfter.color = ColorPalette.GetColor(topSeg.colorId);

        bool addedNewSegment = (topIndexAfter != topIndexBefore);

        float startH_to = addedNewSegment ? 0f : srAfter.size.y;
        float endH_to = startH_to + SumUnits(
            model.filledAmount - topSeg.Amount,
            amountDelta
        );

        int belowUnits = model.filledAmount - topSeg.Amount;
        float bottomOfTop = bottomY + SumUnits(0, belowUnits);

        float startY_to = bottomOfTop + startH_to * 0.5f;
        float endY_to = bottomOfTop + endH_to * 0.5f;

        srAfter.size = new Vector2(innerW, startH_to);
        srAfter.transform.localPosition = new Vector3(0f, startY_to, 0f);

        Sequence s2 = DOTween.Sequence();

        s2.Join(DOTween.To(() => startH_to, v =>
        {
            srAfter.size = new Vector2(innerW, v);
        }, endH_to, dur).SetEase(Ease.Linear));

        s2.Join(DOTween.To(() => startY_to, v =>
        {
            var p = srAfter.transform.localPosition;
            p.y = v;
            srAfter.transform.localPosition = p;
        }, endY_to, dur).SetEase(Ease.Linear));

        topTween = s2;
    }

    public void BoostSortingForPour()
    {
        if (sortingGroup == null) return;
        sortingGroup.sortingOrder = orderBoost;
    }

    public void RestoreSortingAfterPour()
    {
        if (sortingGroup == null) return;
        sortingGroup.sortingOrder = orderDefault;
    }

}
