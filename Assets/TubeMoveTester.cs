using UnityEngine;
using DG.Tweening;

public class TubeMoveTester : MonoBehaviour
{
    [Header("Refs")]
    public TubeView[] tubes;

    [Header("Test Indices")]
    public int fromIndex = 0;
    public int toIndex = 1;

    [Header("Target A (for PourPoint)")]
    public float pourAboveReceive = 3f;
    public float sideOffsetAmount = 1.2f;

    [Header("Timing by Speed")]
    public float moveSpeed = 8f; // units/sec (tính theo quãng đường của POUR)
    [Range(0f, 0.5f)]
    public float rotateDelayRatio = 0.1f; // 10% đầu chưa xoay
    [Range(0.01f, 0.2f)]
    public float splitBlendRatio = 0.06f; // 6% timeline để blend giữa 2 đoạn

    [Header("Angles (Z)")]
    public float angleAtA = -40f;     // góc khi tới/đang tới A (nghiêng nhẹ)
    public float angleAtPour = -70f;  // góc khi đổ (nghiêng mạnh)

    [Header("Ease")]
    public Ease ease = Ease.InOutSine;

    private Sequence seq;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            PlayMove();
    }

    public void PlayMove()
    {
        if (!IsValid()) return;

        seq?.Kill();
        seq = DOTween.Sequence();

        TubeView from = tubes[fromIndex];
        TubeView to = tubes[toIndex];

        Transform tube = from.transform;
        Transform pour = from.pourPoint;
        Transform receive = to.receivePoint;

        seq.Append(
      MoveAndPour_OneStep_TwoStage(
          tube, pour, receive,
          pourAboveReceive,
          sideOffsetAmount,
          angleAtA,        // ví dụ 40
          angleAtPour,     // ví dụ 70
          moveSpeed,
          rotateDelayRatio // delay cho 0->angleA
      )
      );

        // tua ngược về ban đầu
        seq.OnComplete(() => seq.PlayBackwards());

    }
    private Tween MoveAndPour_OneStep_TwoStage(
    Transform tube,
    Transform pour,
    Transform receive,
    float upOffset,
    float sideOffset,
    float angleA,
    float anglePour,
    float speed,
    float delayRatio
)
    {
        delayRatio = Mathf.Clamp(delayRatio, 0f, 0.95f);

        Vector3 startTubePos = tube.position;
        Quaternion startRot = tube.rotation;
        Vector3 startPourPos = pour.position;

        float dirX = Mathf.Sign(tube.position.x - receive.position.x);
        if (dirX == 0) dirX = 1f;

        Vector3 approachPourPos =
            receive.position + Vector3.up * upOffset + Vector3.right * dirX * sideOffset;

        Vector3 finalPourPos =
            receive.position + Vector3.up * upOffset;

        float d1 = Vector3.Distance(startPourPos, approachPourPos);
        float d2 = Vector3.Distance(approachPourPos, finalPourPos);
        float total = d1 + d2;

        float duration = (speed <= 0f) ? 0f : total / speed;
        float split = (total <= 0.000001f) ? 1f : (d1 / total);

        // cửa sổ blend quanh split
        float blend = Mathf.Clamp(splitBlendRatio, 0.01f, 0.2f);
        float a = Mathf.Clamp01(split - blend * 0.5f);
        float b = Mathf.Clamp01(split + blend * 0.5f);

        return DOTween.To(() => 0f, tLin =>
        {
            // ===== SEG1 (từ start -> approach) =====
            float u1Lin = (split <= 0.000001f) ? 1f : Mathf.Clamp01(tLin / split);
            float u1 = DOVirtual.EasedValue(0f, 1f, u1Lin, ease);
            Vector3 p1 = Vector3.Lerp(startPourPos, approachPourPos, u1);

            float rt1Lin = (u1Lin <= delayRatio) ? 0f : Mathf.InverseLerp(delayRatio, 1f, u1Lin);
            float rt1 = DOVirtual.EasedValue(0f, 1f, rt1Lin, ease);
            float z1 = Mathf.Lerp(0f, angleA, rt1);

            // ===== SEG2 (từ approach -> final) =====
            float u2Lin = (1f - split <= 0.000001f) ? 1f : Mathf.Clamp01(Mathf.InverseLerp(split, 1f, tLin));
            float u2 = DOVirtual.EasedValue(0f, 1f, u2Lin, ease);
            Vector3 p2 = Vector3.Lerp(approachPourPos, finalPourPos, u2);

            float z2 = Mathf.Lerp(angleA, anglePour, DOVirtual.EasedValue(0f, 1f, u2Lin, ease));

            // ===== BLEND quanh split để hết “khựng” =====
            float w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, tLin)); // 0..1
            Vector3 pourPos = Vector3.Lerp(p1, p2, w);
            float z = Mathf.Lerp(z1, z2, w);

            Quaternion rot = Quaternion.Euler(0f, 0f, z);
            tube.rotation = rot;

            Vector3 offset = rot * Quaternion.Inverse(startRot) * (startTubePos - startPourPos);
            tube.position = pourPos + offset;

        }, 1f, duration)
        .SetEase(Ease.Linear); // giữ tLin tuyến tính cho đúng logic
    }



    private bool IsValid()
    {
        if (tubes == null || tubes.Length == 0) return false;
        if (fromIndex < 0 || fromIndex >= tubes.Length) return false;
        if (toIndex < 0 || toIndex >= tubes.Length) return false;
        if (fromIndex == toIndex) return false;

        if (tubes[fromIndex] == null || tubes[toIndex] == null) return false;
        if (tubes[fromIndex].pourPoint == null) return false;
        if (tubes[toIndex].receivePoint == null) return false;

        return true;
    }
}
