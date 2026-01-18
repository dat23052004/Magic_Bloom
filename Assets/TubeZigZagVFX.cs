using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UIElements;

public class TubeZigZagVFX : MonoBehaviour
{
    [SerializeField] private Transform leftRoot;
    [SerializeField] private Transform rightRoot;

    [SerializeField] private Transform tubeBottom;
    [SerializeField] private Transform tubeTop;

    [SerializeField] private int zigCount = 3;
    [SerializeField] private float startSideOffsetX = 1;
    [Range(0f, 1f)]

    [SerializeField] private float moveTime = 0.75f;
    [SerializeField] private Ease moveEase = Ease.OutSine;

    [SerializeField] private float delay = 0.1f;

    private bool isRightSide = false;

    private Sequence _seq;

    private void Awake()
    {
        if(leftRoot) leftRoot.gameObject.SetActive(false);
        if(rightRoot) rightRoot.gameObject.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Play();
        }
    }

    public void Play()
    {
        if (!leftRoot || !rightRoot || !tubeBottom || !tubeTop) return;

        Vector3 bottomPos = tubeBottom.position;
        Vector3 topPos = tubeTop.position;

        Vector3[] leftPath = BuildPath(bottomPos, topPos, tubeBottom, startSideOffsetX, zigCount, isRightSide);
        Vector3[] rightPath = BuildPath(bottomPos + Vector3.down * 0.5f, topPos, tubeBottom, startSideOffsetX, zigCount, !isRightSide);

        // Chạy 2 PS song song

        _seq?.Kill();
        _seq = DOTween.Sequence();

        _seq.Insert(0f, MoveRoot(leftRoot, leftPath, moveTime));
        _seq.Insert(delay, MoveRoot(rightRoot, rightPath, Mathf.Max(0.01f, moveTime - delay)));

        _seq.Play();
    }

    private Tween MoveRoot(Transform root, Vector3[] path, float time)
    {
        root.DOKill();
        if (!root.gameObject.activeInHierarchy) root.gameObject.SetActive(true);

        root.position = path[0];

        return root.DOPath(path, time, PathType.CatmullRom)
                   .SetEase(moveEase)
                   .OnComplete(()=>
                   {
                       DOVirtual.DelayedCall(0.2f, () =>
                       {
                           root.gameObject.SetActive(false);
                       });
                   });
    }

  
    private static Vector3[] BuildPath(
     Vector3 bottom, Vector3 top, Transform tubeBasis,
     float sideOffset, int zigCount, bool isRightSide)
    {
        zigCount = Mathf.Max(1, zigCount);

        int pointCount = zigCount + 2;
        Vector3[] pts = new Vector3[pointCount];

        Vector3 axis = top - bottom;
        Vector3 up = axis.normalized;

        Vector3 side = tubeBasis ? tubeBasis.right : Vector3.right;
        side = Vector3.ProjectOnPlane(side, up).normalized;

        // start lệch trái/phải
        pts[0] = bottom + side * (isRightSide ? sideOffset : -sideOffset);

        // ✅ mẫu "cắt nửa nhịp cuối"
        float denom = zigCount + 0.5f;

        for (int i = 1; i <= zigCount; i++)
        {
            float t = i / denom;              // thay vì i/(zigCount+1)
            t = Mathf.Min(t, 1f);             // an toàn

            Vector3 center = Vector3.Lerp(bottom, top, t);

            // đổi bên, có tính cả vệt trái/phải để 2 vệt đan xen đúng
            float sign = (i % 2 == 1) ? 1f : -1f;
            if (isRightSide) sign *= -1f;

            pts[i] = center + side * sideOffset * sign;
        }

        // end đúng top ở giữa
        pts[pointCount - 1] = top;
        return pts;
    }



    private static float GetMaxStartLifetime(ParticleSystem p)
    {
        var lt = p.main.startLifetime;
        if (lt.mode == ParticleSystemCurveMode.Constant) return lt.constant;
        if (lt.mode == ParticleSystemCurveMode.TwoConstants) return lt.constantMax;
        return Mathf.Max(1f, lt.constantMax);
    }

    void OnDrawGizmos()
    {
        if (!tubeBottom || !tubeTop) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(tubeBottom.position, tubeTop.position);
    }

}
