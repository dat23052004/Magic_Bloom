using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    [SerializeField] private List<LevelDataSO> levels = new();

    [SerializeField] private TubeView tuberPrefab;
    [SerializeField] private Transform spawnRoot;

    [SerializeField] private int maxTopRow = 6;
    [SerializeField] private float rowOffsetWorld = 9f;

    [SerializeField]
    private AnimationCurve spacingCurve = new AnimationCurve(
    new Keyframe(1, 0.0f),
    new Keyframe(2, 0.9f),
    new Keyframe(3, 0.7f),
    new Keyframe(4, 0.55f),
    new Keyframe(5, 0.4f),
    new Keyframe(6, 0.22f)
);
    public int CurrentLevel { get; private set; } = 1;
    public List<TubeView> CurrentViews { get; private set; } = new();
    public List<TubeModel> CurrentModels { get; private set; } = new();

    internal void LoadLevel(int levelNumber)
    {
        var data = FindLevel(levelNumber);
        if (data == null) return;

        CurrentLevel = levelNumber;
        ClearSpawned();
        CurrentModels.Clear();
        CurrentViews.Clear();

        for (int i = 0; i < data.tubes.Count; i++)
        {
            var model = BuildModel(data.capacity,data.totalColor, data.tubes[i]);
            TubeView view = Instantiate(tuberPrefab, spawnRoot);
            view.Init(model);
            CurrentModels.Add(model);
            CurrentViews.Add(view);
        }

        ApplyAutoLayout(CurrentViews);
    }

    public void LoadNextLevel() => LoadLevel(CurrentLevel + 1);
    private LevelDataSO FindLevel(int levelNumber) => levels.Find(l => l.level == levelNumber);


    private TubeModel BuildModel(int capacity,int totalColor, TubeData tubeData)
    {
        var model = new TubeModel(capacity, totalColor);

        for (int i = 0; i < tubeData.segmentsBottomToTop.Count; i++)
        {
            var seg = tubeData.segmentsBottomToTop[i];
            if (seg.Amount <= 0) continue;
            model.segments.Add(seg); 
        }
        return model;
    }
    private void ClearSpawned()
    {
        if (spawnRoot == null) return;
        for (int i = spawnRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(spawnRoot.GetChild(i).gameObject);
        }
    }



    // Auto layout
    private void ApplyAutoLayout(List<TubeView> tubes)
    {
        if (tubes == null || tubes.Count == 0) return;
        if (spawnRoot == null) return;

        int totalTubes = tubes.Count;

        Camera cam = Camera.main;
        float camHelfHeight = cam.orthographicSize;
        float camHelfWidth = camHelfHeight * cam.aspect;

        float left = -camHelfWidth;
        float right = camHelfWidth;
        float availableWidth = right - left;

        ComputeRows(totalTubes, out int topCount, out int bottomCount);
        bool twoRows = bottomCount > 0;

        float twoRowOffset = rowOffsetWorld * 0.5f;
        float topY = twoRows ? spawnRoot.position.y + twoRowOffset : spawnRoot.position.y;
        float bottomY = -spawnRoot.position.y - twoRowOffset;

        // scale theo hangf rong nhat
        float tubeWidth = EstimateTubeWidth(tubes[0].transform);


        LayoutRowAutoSpacing(CurrentViews, 0, topCount, topY, spawnRoot.position.x, tubeWidth, availableWidth);

        if (bottomCount > 0)
            LayoutRowAutoSpacing(CurrentViews, topCount, bottomCount, bottomY, spawnRoot.position.x, tubeWidth, availableWidth);

    }
    private void LayoutRowAutoSpacing(List<TubeView> tubes, int start, int count, float y, float centerX, float tubeWidth, float availableWidth)
    {
        if (count <= 0) return;

        if (count == 1)
        {
            tubes[start].transform.position = new Vector3(centerX, y, 0f);
            return;
        }

        float spacingWanted = spacingCurve.Evaluate(count);


        float step = tubeWidth + spacingWanted;
        float mid = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float x = centerX + (i - mid) * step;
            var t = tubes[start + i].transform;
            t.position = new Vector3(x, y, 0f);
        }
    }


    private void ComputeRows(int total, out int topRow, out int bottomRow)
    {
        if (total <= 6)
        {
            topRow = total;
            bottomRow = 0;
            return;
        }

        topRow = Mathf.CeilToInt(total / 2f);
        topRow = Mathf.Min(topRow, 6);
        bottomRow = total - topRow;
    }

    private float EstimateTubeWidth(Transform transform)
    {
        var front = transform.Find("Tube_Front");
        if (front != null)
        {
            var srFront = front.GetComponent<SpriteRenderer>();
            if (srFront != null) return srFront.bounds.size.x;
        }

        return 1f;
    }


}
