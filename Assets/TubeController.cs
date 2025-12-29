using DG.Tweening;
using System;
using UnityEngine;

public class TubeController : Singleton<TubeController>
{
    [SerializeField] private float liftY = 1.3f;
    [SerializeField] private float liftTime = 0.2f;

    [SerializeField] private Ease liftEase = Ease.OutBack;

    [SerializeField] private float pourAboveReceive = 1.2f;
    [SerializeField] private float sideOffsetAmount = 1f;
    [SerializeField] private float moveSpeed = 8f; // units/sec theo quãng đường POUR

    [Header("Tilt Timing")]
    [SerializeField] private float approachTiltTime = 0.14f;  // tilt angleA (chuẩn bị) trước khi move hoặc khi tới nơi
    [SerializeField] private float minMoveTime = 0.18f;      // thời gian di chuyển ngắn nhất 
    [SerializeField] private Ease moveEase = Ease.InOutSine;
    [SerializeField] private Ease tiltEase = Ease.InOutSine;

    [Header("Angle Ranges (computed from model)")]
    [SerializeField] private float angleA_Min = -25f;         // rót ít -> chuẩn bị nhẹ
    [SerializeField] private float angleA_Max = -45f;         // rót nhiều -> chuẩn bị sâu
    [SerializeField] private float pourAngleMin = -60f;       // rót ít
    [SerializeField] private float pourAngleMax = -80f;       // rót nhiều

    private TubeView tubeSelected;
    private Vector3 selectedBaseLocalPos;
    private bool locked;
    private Tween currentTween;
    private Sequence seq;
    public void OnTubeClicked(TubeView tube)
    {
        if (locked || tube == null || tube.model == null) return;
        if (tubeSelected == null)
        {
            Select(tube);
            return;
        }

        if (tubeSelected == tube)
        {
            Deselect();
            return;
        }

        if (Rules.CanPour(tubeSelected.model, tube.model))
            PourWithArc(tubeSelected, tube);
        else
            SwitchSelect(tubeSelected, tube);
    }


    private void Select(TubeView tube)
    {
        KillTween();

        tubeSelected = tube;
        selectedBaseLocalPos = tubeSelected.transform.localPosition;

        tubeSelected.BoostSortingForPour();

        locked = true;

        currentTween = tube.transform
            .DOLocalMove(selectedBaseLocalPos + Vector3.up * liftY, liftTime)
            .SetEase(liftEase)
            .OnComplete(() => locked = false);
    }

    private void Deselect()
    {
        if (tubeSelected == null) return;
        tubeSelected.RestoreSortingAfterPour();
        KillTween();
        currentTween = tubeSelected.transform
           .DOLocalMove(selectedBaseLocalPos, liftTime)
           .SetEase(liftEase)
           .OnComplete(() =>
           {
               tubeSelected = null;
               locked = false;
           });
    }

    private void SwitchSelect(TubeView from, TubeView to)
    {
        KillTween();
        locked = true;

        Vector3 fromBase = selectedBaseLocalPos;
        Vector3 toBase = to.transform.localPosition;
        from.RestoreSortingAfterPour();
        to.BoostSortingForPour();
        seq = DOTween.Sequence();

        // A đi xuống
        seq.Append(from.transform
            .DOLocalMove(fromBase, liftTime)
            .SetEase(liftEase));

        // B đi lên CÙNG LÚC
        seq.Join(to.transform
            .DOLocalMove(toBase + Vector3.up * liftY, liftTime)
            .SetEase(liftEase));

        seq.OnComplete(() =>
        {
            tubeSelected = to;
            selectedBaseLocalPos = toBase;
            locked = false;
        });

        currentTween = seq;
    }

    private void PourWithArc(TubeView from, TubeView to)
    {
        KillTween();
        KillSeq();
        locked = true;

        // base/lift theo LOCAL như demo
        Vector3 basePos = selectedBaseLocalPos;
        Vector3 liftPos = basePos + Vector3.up * liftY;

        // ép tube đang lift (tránh sai pose)
        from.transform.localPosition = liftPos;
        from.transform.localRotation = Quaternion.identity;

        Transform tube = from.transform;
        Transform pour = from.GetPourPointForTarget(to.receivePoint);
        Transform receive = to.receivePoint;

        // lưu pose lift hiện tại (WORLD) để về lại lift trước khi hạ
        Vector3 liftWorldPos = tube.position;
        Quaternion liftWorldRot = tube.rotation;

        // tính thời gian “đổ”
        int amount = 0;
        if (from.model.GetTop(out var topSeg))
        {
            amount = topSeg.Amount;
        }


        float dir = Mathf.Sign(receive.position.x - pour.position.x);
        if (Mathf.Abs(dir) < 0.0001f) dir = 1f;

        float angleAtA = ComputeAngleA(from.model);
        float signedAngleA = -dir * angleAtA;

        float anglePour = ComputePourAngle(from.model);
        float signedAnglePour = -dir * anglePour;

        seq = DOTween.Sequence();

        Tween go = MoveToPourSpot(tube, pour, receive, pourAboveReceive, sideOffsetAmount, signedAngleA, moveSpeed, approachTiltTime, minMoveTime);

        int fromTopIndex = from.model.segments.Count - 1;
        int toTopIndex = to.model.segments.Count - 1;

        float pourDuration = 0.6f; // thời gian đổ cố định cho đẹp
        var t2 = RotateWhileSlidePourToReceiveX_ThenHold(tube, pour, receive, signedAnglePour, pourDuration);
        var flow = CreatePourFlowTween(from, to, fromTopIndex, toTopIndex, amount, pour, receive, pourDuration);

        seq.Append(go);
        seq.Append(t2).Join(flow);

        // 4) Về lại: quay thẳng trước (đẹp hơn), rồi bay về lift pose
        float backRotDur = 0.18f;
        float backMoveDur = 0.25f;

        seq.Append(tube.DORotateQuaternion(liftWorldRot, backRotDur).SetEase(tiltEase));
        seq.Append(tube.DOMove(liftWorldPos, backMoveDur).SetEase(moveEase));

        // (8) hạ xuống base (LOCAL)
        seq.Append(tube.DOLocalMove(basePos, liftTime).SetEase(liftEase));

        seq.OnComplete(() =>
        {
            if (tubeSelected != null) tubeSelected.RestoreSortingAfterPour();
            tubeSelected = null;
            locked = false;
        });
        currentTween = seq;
    }
    private Tween MoveToPourSpot(Transform tube, Transform pour, Transform receive, float upOffset
     , float sideOffset, float angleA, float speed, float approachTiltTime, float minMoveTime)
    {
        Vector3 startTubePos = tube.position;
        Quaternion startRot = tube.rotation;
        Vector3 startPourPos = pour.position;

        // đứng cùng phía so với tube nhận
        float dirX = Mathf.Sign(tube.position.x - receive.position.x);
        if (dirX == 0) dirX = 1f;

        Vector3 approachPourPos = receive.position + Vector3.up * upOffset + Vector3.right * dirX * sideOffset;
        float d = Vector3.Distance(startPourPos, approachPourPos);
        float moveDur = (speed <= 0f || d <= 0.000001f) ? 0f : d / speed;
        float duration = Mathf.Max(moveDur, approachTiltTime, minMoveTime);
        float tiltReachRatio = (duration <= 0.000001f) ? 1f : Mathf.Clamp01(approachTiltTime / duration);
        return DOVirtual.Float(0f, 1f, duration, t01 =>
            {
                float uMove = DOVirtual.EasedValue(0f, 1f, t01, moveEase);
                Vector3 pourPos = Vector3.Lerp(startPourPos, approachPourPos, uMove);

                float reach = (tiltReachRatio <= 0.000001f) ? 1f : Mathf.Clamp01(t01 / tiltReachRatio);
                float uRot = DOVirtual.EasedValue(0f, 1f, reach, tiltEase);
                float zAngle = Mathf.Lerp(0f, angleA, uRot);
                ApplyPoseKeepPour(tube, startTubePos, startRot, startPourPos, pourPos, zAngle);
            }).SetEase(Ease.Linear);
    }

    private void ApplyPoseKeepPour(Transform tube, Vector3 startTubePos, Quaternion startRot,
    Vector3 startPourPos, Vector3 pourPos, float zAngle)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, zAngle);
        tube.rotation = rot;

        Vector3 offset = rot * Quaternion.Inverse(startRot) * (startTubePos - startPourPos);
        tube.position = pourPos + offset;
    }

    private Tween RotateWhileSlidePourToReceiveX_ThenHold(Transform tube, Transform pour, Transform receive, float angleTo, float duration)
    {
        Vector3 startTubePos = default;
        Quaternion startRot = default;
        Vector3 startPourPos = default;
        Vector3 targetPourPos = default;

        float angleFrom = 0f;
        Tweener tw = null;
        tw = DOVirtual.Float(0f, 1f, 1f, t01 =>
        {
            float uRot = DOVirtual.EasedValue(0f, 1f, t01, tiltEase);
            float z = Mathf.Lerp(angleFrom, angleTo, uRot);

            Vector3 pourPos = Vector3.Lerp(startPourPos, targetPourPos, t01);

            ApplyPoseKeepPour(tube, startTubePos, startRot, startPourPos, pourPos, z);
        })
        .SetEase(Ease.Linear)
        .OnStart(() =>
        {
            // snapshot đúng thời điểm bắt đầu tween #2
            startTubePos = tube.position;
            startRot = tube.rotation;
            startPourPos = pour.position;

            angleFrom = tube.eulerAngles.z;
            if (angleFrom > 180f) angleFrom -= 360f;

            // chỉ canh X: đưa pour.x về receive.x
            targetPourPos = new Vector3(receive.position.x, startPourPos.y, startPourPos.z);
        });

        return tw;
    }
    private Tween CreatePourFlowTween(TubeView from, TubeView to, int fromTopIndex, int toTopIndex, int amounts, Transform pour, Transform receive, float duration)
    {
        Tweener tween = null;

        tween = DOVirtual.Float(0f, 1f, 1f, _ => { })
            .SetEase(Ease.Linear)
            .OnStart(() =>
            {
                Rules.Pour(from.model, to.model);
                from.AnimateTopOnly(fromTopIndex, -amounts, duration);
                to.AnimateTopOnly(toTopIndex, +amounts, duration);
            });

        return tween;
    }

    private float ComputeAngleA(TubeModel model)
    {
        if (!model.GetTop(out var topSeg)) return 0f;

        float amount01 = Mathf.Clamp01((float)model.filledAmount / model.capacity);
        return Mathf.Abs(Mathf.Lerp(angleA_Max, angleA_Min, amount01));
    }

    private float ComputePourAngle(TubeModel model)
    {
        if (!model.GetTop(out var TopSeg)) return 0f;
        float amount01 = Mathf.Clamp01((float)(model.filledAmount - TopSeg.Amount) / model.capacity);
        return Mathf.Abs(Mathf.Lerp(pourAngleMax, pourAngleMin, amount01));
    }

    void KillSeq()
    {
        if (seq != null && seq.IsActive()) seq.Kill();
        seq = null;
    }

    private void KillTween()
    {
        if (currentTween != null && currentTween.IsActive())
            currentTween.Kill();
        currentTween = null;
    }

}
