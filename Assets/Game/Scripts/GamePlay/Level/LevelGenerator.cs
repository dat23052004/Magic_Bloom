using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LevelGenerator
{
    private const int GEN_SALT = unchecked((int)0x3D2E1F0B);

    public static LevelConfig GetLevelConfig(int levelNumber) => new LevelConfig(levelNumber);

    public static List<TubeData> GenerateTubes(LevelConfig config)
    {
        if (config.colorCount < 2) throw new ArgumentException("colorCount >= 2");
        if (config.emptyTubes < 1) throw new ArgumentException("emptyTubes >= 1");
        if (config.capacity < 2) throw new ArgumentException("capacity >= 2");

        int baseSeed = config.level * 6789 + GEN_SALT;

        for (int attempt = 0; attempt < 60; attempt++)
        {
            UnityEngine.Random.InitState(baseSeed + attempt * 1013);

            var tubes = BuildLayout(config);
            if (tubes != null) return tubes;
        }

        Debug.LogWarning($"[LevelGen] Level {config.level}: fallback to solved state.");
        return CreateSolved(config);
    }

    //  PIPELINE: Plan → Fill → Swap

    private static List<TubeData> BuildLayout(LevelConfig cfg)
    {
        int C = cfg.colorCount, K = cfg.capacity, d = cfg.mixDepth;

        // ── Step 1: Segment Plan ──
        var plans = BuildSegmentPlans(C, K, d);
        if (plans == null) return null;

        // ── Step 2: Fill solved (mỗi tube = 1 màu, chia boundary theo plan) ──
        var tubes = CreateSolved(cfg);
        var segMap = BuildSegmentMap(plans, C, K);

        // ── Step 3: Segment Swap ──
        int swapSteps = C * (d + 2);
        DoSegmentSwaps(tubes, segMap, C, K, swapSteps);

        // ── Step 4: Check mỗi tube đã mixed ──
        for (int i = 0; i < C; i++)
            if (IsSingleColor(tubes[i])) return null;

        return tubes;
    }

    //  STEP 1: Segment Plan

    /// <summary>
    /// Trả về plans[tubeIdx] = int[] { segLen1, segLen2, ... } với sum = K.
    /// d<=2: tất cả tube dùng chung 1 pattern.
    /// d>=3: mỗi tube có thể dùng pattern khác, thỏa Rule A/B.
    /// </summary>
    private static int[][] BuildSegmentPlans(int C, int K, int mixDepth)
    {
        int minSeg = mixDepth <= 2 ? 2 : 1; // Rule C: d<=2 cấm singleton

        if (mixDepth <= 2)
            return BuildUniformPlans(C, K, mixDepth, minSeg);
        else
            return BuildDiversePlans(C, K, mixDepth, minSeg);
    }

    // ── d<=2: 1 pattern chung ──

    private static int[][] BuildUniformPlans(int C, int K,int mixDepth, int minSeg)
    {
        // Tạo 1 partition ngẫu nhiên của K, mỗi part >= minSeg
        var pattern = RandomPartition(K, mixDepth, minSeg);
        if (pattern == null) return null;

        var plans = new int[C][];
        for (int i = 0; i < C; i++)
            plans[i] = (int[])pattern.Clone();

        return plans;
    }

    // ── d>=3: đa dạng, thỏa Rule A/B ──

    private static int[][] BuildDiversePlans(int C, int K,int mixDepth, int minSeg)
    {
        // Tạo C patterns, sau đó validate & fix pool
        for (int retry = 0; retry < 30; retry++)
        {
            var plans = new int[C][];
            for (int i = 0; i < C; i++)
                plans[i] = RandomPartition(K, mixDepth, minSeg);

            if (plans.Any(p => p == null)) continue;

            // Rule A: mỗi length phải xuất hiện >= 2 lần
            if (FixPoolBalance(plans, C, K, mixDepth, minSeg))
                return plans;
        }

        // Fallback: dùng uniform
        return BuildUniformPlans(C, K, mixDepth, minSeg);
    }

    /// <summary>
    /// Rule A & B: đếm frequency mỗi length, fix những length xuất hiện lẻ.
    /// </summary>
    private static bool FixPoolBalance(int[][] plans, int C, int K,int mixDepth, int minSeg)
    {
        for (int fix = 0; fix < 50; fix++)
        {
            // Đếm tần suất mỗi length
            var freq = new Dictionary<int, int>();
            for (int i = 0; i < C; i++)
                foreach (int len in plans[i])
                    freq[len] = freq.GetValueOrDefault(len, 0) + 1;

            // Tìm length xuất hiện < 2 (Rule A violation)
            int badLen = -1;
            foreach (var kv in freq)
                if (kv.Value < 2) { badLen = kv.Key; break; }

            if (badLen < 0) return true; // All good

            // Fix: chọn 1 tube random, re-partition nó
            int t = UnityEngine.Random.Range(0, C);
            plans[t] = RandomPartition(K, mixDepth, minSeg);
            if (plans[t] == null) plans[t] = new int[] { K }; // fallback
        }

        // Check final
        var finalFreq = new Dictionary<int, int>();
        for (int i = 0; i < C; i++)
            foreach (int len in plans[i])
                finalFreq[len] = finalFreq.GetValueOrDefault(len, 0) + 1;

        return finalFreq.Values.All(v => v >= 2);
    }

    /// <summary>
    /// Partition K thành 2-4 parts, mỗi part >= minSeg.
    /// </summary>
    private static int[] RandomPartition(int K,int mixDepth, int minSeg)
    {
        int n = 0;
        // Chọn số segment: 2-4
        if (mixDepth == 1) n = UnityEngine.Random.Range(2, 4);
        else if(mixDepth == 2) n = 3;
        else if (mixDepth == 3) n = UnityEngine.Random.Range(3, 5);
        else if (mixDepth == 4) n = 4;
        else if (mixDepth == 5) n = (UnityEngine.Random.value <= 0.6) ? 4 : 5;
        else  n = 5;

        if (n * minSeg > K) n = K / minSeg;
        if (n < 2) return null;

        // ── Max part: d thấp → block lớn (dễ đọc), d cao → block nhỏ (rối) ──
        int maxPart;
        if (mixDepth <= 2) maxPart = (K + 1) / 2;                // ~nửa tube
        else if (mixDepth <= 4) maxPart = Mathf.Max((K + 1) / 3 + 1, 3);
        else maxPart = Mathf.Max(K / n + 1, 2);    // gần đều

        maxPart = Mathf.Max(maxPart, minSeg);

        // ── Distribute: start minSeg, rải dư random nhưng respect maxPart ──
        var parts = new int[n];
        for (int i = 0; i < n; i++) parts[i] = minSeg;

        int remaining = K - n * minSeg;
        if (remaining < 0) return null;

        for (int r = 0; r < remaining; r++)
        {
            // Collect indices chưa đầy
            int picked = -1;
            int tries = 0;
            while (tries < n * 2)
            {
                int idx = UnityEngine.Random.Range(0, n);
                if (parts[idx] < maxPart) { picked = idx; break; }
                tries++;
            }

            // Nếu tất cả đã max → nới cho 1 slot random (hiếm khi xảy ra)
            if (picked < 0) picked = UnityEngine.Random.Range(0, n);

            parts[picked]++;
        }

        Shuffle(parts);
        return parts;
    }

    //  STEP 2: Segment Map

    private struct Segment
    {
        public int tube;
        public int start;
        public int length;
    }

    /// <summary>
    /// Từ plans, build danh sách segment với vị trí cụ thể trong mỗi tube.
    /// Trả về Dictionary: length → List of Segment.
    /// </summary>
    private static Dictionary<int, List<Segment>> BuildSegmentMap(int[][] plans, int C, int K)
    {
        var map = new Dictionary<int, List<Segment>>();

        for (int t = 0; t < C; t++)
        {
            int pos = 0;
            foreach (int len in plans[t])
            {
                if (!map.ContainsKey(len))
                    map[len] = new List<Segment>();

                map[len].Add(new Segment { tube = t, start = pos, length = len });
                pos += len;
            }
        }

        return map;
    }

    //  STEP 3: Segment Swap

    private static void DoSegmentSwaps(List<TubeData> tubes, Dictionary<int, List<Segment>> segMap,
        int C, int K, int swapSteps)
    {
        // Collect lengths có >= 2 segments (swappable)
        var swappableLens = new List<int>();
        foreach (var kv in segMap)
            if (kv.Value.Count >= 2) swappableLens.Add(kv.Key);

        if (swappableLens.Count == 0) return;

        bool[] touched = new bool[C];

        for (int step = 0; step < swapSteps; step++)
        {
            // 1. Chọn length L
            int L = swappableLens[UnityEngine.Random.Range(0, swappableLens.Count)];
            var segs = segMap[L];
            if (segs.Count < 2) continue;

            // 2. Chọn 2 segment khác tube
            int idxA = UnityEngine.Random.Range(0, segs.Count);
            int idxB = UnityEngine.Random.Range(0, segs.Count);
            if (idxA == idxB) continue;

            var sA = segs[idxA];
            var sB = segs[idxB];
            if (sA.tube == sB.tube) continue;

            // 3. Check 2 segment khác màu (ít nhất 1 cell khác)
            bool diff = false;
            for (int i = 0; i < L; i++)
            {
                if (tubes[sA.tube].layers[sA.start + i] != tubes[sB.tube].layers[sB.start + i])
                { diff = true; break; }
            }
            if (!diff) continue;

            // 4. Swap
            for (int i = 0; i < L; i++)
            {
                int pA = sA.start + i;
                int pB = sB.start + i;
                (tubes[sA.tube].layers[pA], tubes[sB.tube].layers[pB]) = (tubes[sB.tube].layers[pB], tubes[sA.tube].layers[pA]);
            }

            touched[sA.tube] = true;
            touched[sB.tube] = true;
        }

        // Nếu có tube chưa touched, cố gắng swap thêm
        for (int t = 0; t < C; t++)
        {
            if (touched[t]) continue;

            // Tìm 1 segment của tube t, swap với segment cùng length ở tube khác
            for (int li = 0; li < swappableLens.Count; li++)
            {
                int L = swappableLens[li];
                var segs = segMap[L];

                Segment? mine = null, other = null;
                foreach (var s in segs)
                {
                    if (s.tube == t && mine == null) mine = s;
                    else if (s.tube != t && other == null) other = s;
                    if (mine.HasValue && other.HasValue) break;
                }

                if (!mine.HasValue || !other.HasValue) continue;

                var sA = mine.Value;
                var sB = other.Value;

                for (int i = 0; i < L; i++)
                {
                    int pA = sA.start + i;
                    int pB = sB.start + i;
                    (tubes[sA.tube].layers[pA], tubes[sB.tube].layers[pB]) =
                        (tubes[sB.tube].layers[pB], tubes[sA.tube].layers[pA]);
                }
                break;
            }
        }
    }

    //  HELPERS

    private static bool IsSingleColor(TubeData tube)
    {
        var first = ColorId.None;
        foreach (var c in tube.layers)
        {
            if (c == ColorId.None) continue;
            if (first == ColorId.None) first = c;
            else if (c != first) return false;
        }
        return first != ColorId.None;
    }

    private static List<TubeData> CreateSolved(LevelConfig config)
    {
        var tubes = new List<TubeData>(config.colorCount + config.emptyTubes);
        for (int i = 0; i < config.colorCount; i++)
        {
            var t = new TubeData { layers = new ColorId[config.capacity] };
            Array.Fill(t.layers, (ColorId)i);
            tubes.Add(t);
        }
        for (int i = 0; i < config.emptyTubes; i++)
        {
            var t = new TubeData { layers = new ColorId[config.capacity] };
            Array.Fill(t.layers, ColorId.None);
            tubes.Add(t);
        }
        return tubes;
    }

    private static void Shuffle(int[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }
}