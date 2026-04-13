using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    [SerializeField] private List<LevelDataSO> levels = new();

    [SerializeField] private TubeView tuberPrefab;
    [SerializeField] private Transform spawnRoot;

    [SerializeField] private float rowOffsetWorld = 9f;
    [SerializeField]
    private AnimationCurve spacingCurve = new AnimationCurve(
    new Keyframe(1, 0.0f),
    new Keyframe(2, 0.9f));

    [SerializeField] private int maxExtraTubes = 2;
    [SerializeField] private float sfxVolume = .2f;
    public int CurrentLevel { get; private set; } = 1;
    public List<TubeView> CurrentViews { get; private set; } = new();
    public List<TubeModel> CurrentModels { get; private set; } = new();

    private int extraTubesUsed = 0;
    private bool isShuffleSelectMode = false;
    public bool IsShuffleSelectMode => isShuffleSelectMode;
    public event Action<bool> OnShuffleSelectModeChanged;
    public event Action OnExtraTubeStateChanged;

    private void Start()
    {
        if (levels == null || levels.Count == 0) LoadLevelsFromResources();
    }
    private void LoadLevelsFromResources()
    {
        levels = new List<LevelDataSO>();
        var loadedLevels = Resources.LoadAll<LevelDataSO>("Levels");

        foreach (var level in loadedLevels)
        {
            levels.Add(level);
        }

        // Sort by level number
        levels.Sort((a, b) => a.level.CompareTo(b.level));

        Debug.Log($"Loaded {levels.Count} levels from Resources");
    }

    internal void LoadLevel(int levelNumber)
    {
        UIManager.Ins.UpdateLevel(levelNumber);
        UndoManager.Ins?.ClearHistory();
        ScoreManager.Ins?.ResetScore();
        var data = FindLevel(levelNumber);
        if (data == null) return;

        CurrentLevel = levelNumber;
        extraTubesUsed = 0;
        NotifyExtraTubeStateChanged();
        SetShuffleSelectMode(false);
        ClearSpawned();
        CurrentModels.Clear();
        CurrentViews.Clear();

        for (int i = 0; i < data.tubes.Count; i++)
        {
            var model = BuildModel(data.capacity, data.totalColor, data.tubes[i]);
            TubeView view = Instantiate(tuberPrefab, spawnRoot);
            view.Init(model);
            CurrentModels.Add(model);
            CurrentViews.Add(view);
        }

        ApplyAutoLayout(CurrentViews);
    }

    public void LoadNextLevel() => LoadLevel(CurrentLevel + 1);
    private LevelDataSO FindLevel(int levelNumber) => levels.Find(l => l.level == levelNumber);

    private TubeModel BuildModel(int capacity, int totalColor, TubeData tubeData)
    {
        var model = new TubeModel(capacity, totalColor);

        for (int i = 0; i < tubeData.GetSegments().Count; i++)
        {
            var seg = tubeData.GetSegments()[i];
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
        float availableWidth = camHelfWidth * 2f;

        ComputeRows(totalTubes, out int topCount, out int bottomCount);
        bool twoRows = bottomCount > 0;
        foreach (var tube in tubes)
            tube.transform.localScale = Vector3.one;
        // ── Scale khi > 12 ──
        float scale = 1f;
        if (totalTubes > Constant.MAX_TUBES_NO_SCALE)
        {
            // Lấy tubeWidth ở scale=1 để tính
            float originalWidth = EstimateTubeWidth(tubes[0].transform);
            int maxRowCount = Mathf.Max(topCount, bottomCount);
            float spacingAtRow = spacingCurve.Evaluate(Mathf.Min(maxRowCount, 7));
            float neededWidth = maxRowCount * originalWidth + (maxRowCount - 1) * spacingAtRow;

            // Fit vào 90% màn hình (giữ padding 2 bên)
            float usableWidth = availableWidth * 0.9f;
            if (neededWidth > usableWidth) scale = usableWidth / neededWidth;

            scale = Mathf.Clamp(scale, 0.55f, 1f);
            Debug.Log(scale);
        }

        // Apply scale
        foreach (var tube in tubes)
            tube.transform.localScale = Vector3.one * scale;

        float tubeWidth = EstimateTubeWidth(tubes[0].transform); // bounds đã tính scale

        float twoRowOffset = rowOffsetWorld * 0.5f * scale;
        float topY = twoRows ? spawnRoot.position.y + twoRowOffset : spawnRoot.position.y;
        float bottomY = -spawnRoot.position.y - twoRowOffset;

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
        topRow = Mathf.Min(topRow, Constant.MAX_PER_ROW);
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

    public bool IsWin()
    {
        return CurrentModels.All(t => Rules.IsCompleted(t) || t.isEmpty);
    }


    #region Undo
    public void RecordMode(int fromIndex, int toIndex, ColorSegment segment)
    {
        UndoManager.Ins?.RecordMove(fromIndex, toIndex, segment);
    }

    // undo 1 bước
    public bool PerformUndo()
    {
        if (UndoManager.Ins == null || !UndoManager.Ins.HasHistory) return false;
        bool success = UndoManager.Ins.Undo(CurrentModels, CurrentViews);
        if (success)
        {
            AudioManager.Ins?.PlaySFX(SfxCue.Undo,sfxVolume);
            ComboTracker.Ins?.ResetCombo();
        }
        return success;
    }
    #endregion

    #region Add empty tube
    public bool CanAddExtraTube() => extraTubesUsed < maxExtraTubes;
    public bool AddExtraTube()
    {
        if (!CanAddExtraTube()) return false;
        var data = FindLevel(CurrentLevel);
        if (data == null) return false;

        int capacity = data.capacity;
        int totalColor = data.totalColor;

        var model = new TubeModel(capacity, totalColor);
        TubeView view = Instantiate(tuberPrefab, spawnRoot);
        view.Init(model);
        CurrentModels.Add(model);
        CurrentViews.Add(view);
        extraTubesUsed++;
        NotifyExtraTubeStateChanged();

        ApplyAutoLayout(CurrentViews);
        AudioManager.Ins?.PlaySFX(SfxCue.AddTube, sfxVolume);
        return true;
    }
    #endregion

    #region Shuffle Select Mode
    // bật tắt chế độ để chọn tube ne
    public void ToggleShuffleSelectMode()
    {
        SetShuffleSelectMode(!isShuffleSelectMode);
        Debug.Log($"[PowerUp] Shuffle select mode: {isShuffleSelectMode}");
    }

    public bool ShuffleTube(int tubeIndex)
    {
        if (tubeIndex < 0 || tubeIndex >= CurrentModels.Count) return false;

        var model = CurrentModels[tubeIndex];
        if (model.isEmpty) return false;
        if (Rules.IsCompleted(model)) return false;
        if (model.segments.Count <= 1) return false;
        if (InventoryService.Ins == null || !InventoryService.Ins.UseItem(ItemType.ShuffleTube))
        {
            SetShuffleSelectMode(false);
            return false;
        }

        // Fisher-Yates shuffle trên chính list segments
        var segs = model.segments;
        for (int i = segs.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (segs[i], segs[j]) = (segs[j], segs[i]);
        }

        // Merge segments liền kề cùng màu (edge case sau shuffle)
        for (int i = segs.Count - 1; i > 0; i--)
        {
            if (segs[i].colorId == segs[i - 1].colorId)
            {
                segs[i - 1] = new ColorSegment
                {
                    colorId = segs[i - 1].colorId,
                    Amount = segs[i - 1].Amount + segs[i].Amount
                };
                segs.RemoveAt(i);
            }
        }

        CurrentViews[tubeIndex].Refresh();

        SetShuffleSelectMode(false);

        UndoManager.Ins?.ClearHistory();
        AudioManager.Ins?.PlaySFX(SfxCue.Shuffle, sfxVolume);
        return true;
    }
    #endregion

    private void NotifyExtraTubeStateChanged()
    {
        OnExtraTubeStateChanged?.Invoke();
    }

    private void SetShuffleSelectMode(bool enabled)
    {
        if (isShuffleSelectMode == enabled)
        {
            OnShuffleSelectModeChanged?.Invoke(isShuffleSelectMode);
            return;
        }

        isShuffleSelectMode = enabled;
        OnShuffleSelectModeChanged?.Invoke(isShuffleSelectMode);
    }
}
