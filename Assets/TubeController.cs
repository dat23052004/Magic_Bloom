using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.InputSystem.Controls.AxisControl;

public class TubeController : Singleton<TubeController>
{
    [SerializeField] private float liftY = 1.3f;
    [SerializeField] private float liftTime = 0.2f;

    [SerializeField] private Ease liftEase = Ease.OutBack;

    [SerializeField] private float pourAboveReceive = 3f;
    [SerializeField] private float sideOffsetAmount = 1.2f;
    [SerializeField] private float moveSpeed = 8f; // units/sec theo quãng đường POUR


    [Header("Tilt Timing")]
    [SerializeField] private float safeTiltTime = 0.12f;      // tilt an toàn khi nâng
    [SerializeField] private float approachTiltTime = 0.14f;  // tilt angleA (chuẩn bị) trước khi move hoặc khi tới nơi
    [SerializeField] private float pourTiltTime = 0.18f;      // tilt anglePour khi tới điểm
    [SerializeField] private Ease moveEase = Ease.InOutSine;
    [SerializeField] private Ease tiltEase = Ease.InOutSine;

    [Header("Angle Ranges (computed from model)")]
    [SerializeField] private float angleA_Min = -25f;         // rót ít -> chuẩn bị nhẹ
    [SerializeField] private float angleA_Max = -45f;         // rót nhiều -> chuẩn bị sâu
    [SerializeField] private float pourAngleMin = -60f;       // rót ít
    [SerializeField] private float pourAngleMax = -80f;       // rót nhiều

    [Header("Pour Timing")]
    [SerializeField] private float pourUnitTime = 0.08f;
    [SerializeField] private float pourMinTime = 0.15f;
    [SerializeField] private float pourMaxTime = 0.60f;
    [SerializeField] private float pourHoldTime = 0.2f;

    private TubeView tubeSelected;
    private Vector3 selectedBaseLocalPos;
    private bool locked;
    private Tween currentTween;
    private Sequence seq;

    private Vector3 debugApproachPos;
    private Vector3 debugFinalPos;
    private bool hasDebugPos = true;

    [SerializeField] private bool debugPhase2 = true;
    [SerializeField] private int debugSteps2 = 30;

    private Vector3 dbg2_startPourPos;
    private Vector3 dbg2_targetPourPos;
    private bool dbg2_has = false;
    private float dbg2_kMove = 1f;
    private bool dbg2_moveCanFinish = true;
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
        if(from.model.GetTop(out var topSeg))
        {
            amount = topSeg.Amount;
        }

        float pourDur = Mathf.Clamp(amount * pourUnitTime, pourMinTime, pourMaxTime);

        float dir = Mathf.Sign(receive.position.x - pour.position.x);
        if (Mathf.Abs(dir) < 0.0001f) dir = 1f;

        float angleAtA = ComputeAngleA(from.model);
        float angleAAbs = Mathf.Abs(angleAtA);
        float signedAngleA = -dir * angleAAbs;

        float anglePour = ComputePourAngle(from.model);
        float anglePourAbs = Mathf.Abs(anglePour);
        float signedAnglePour = -dir * anglePourAbs;

        seq = DOTween.Sequence();

        Tween go = MoveToPourSpot(tube, pour, receive, pourAboveReceive, sideOffsetAmount, signedAngleA, moveSpeed, 0.6f);
        seq.Append(go);

        var t2 = RotateWhileSlidePourToReceiveX_ThenHold(tube, pour, receive,signedAnglePour,moveSpeed,0.18f);
        seq.Append(t2);

        // 2) Hold để nhìn “chảy”
        seq.AppendInterval(pourHoldTime);

        // 3) Logic đổ
        seq.AppendCallback(() =>
        {
            // snapshot index trước khi model đổi
            int fromTopIndex = from.model.segments.Count - 1;
            int toTopIndex = to.model.segments.Count - 1;

            // đổi model
            Rules.Pour(from.model, to.model);

            // animate
            from.AnimateTopOnly(fromTopIndex, -amount, pourDur);
            to.AnimateTopOnly(toTopIndex, +amount, pourDur);
        });
        seq.AppendInterval(pourDur);

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
        , float sideOffset, float angleA, float speed, float tiltReachRatio)
    {
        tiltReachRatio = Mathf.Clamp01(tiltReachRatio);

        Vector3 startTubePos = tube.position;
        Quaternion startRot = tube.rotation;
        Vector3 startPourPos = pour.position;

        // đứng cùng phía so với tube nhận
        float dirX = Mathf.Sign(tube.position.x - receive.position.x);
        if (dirX == 0) dirX = 1f;

        Vector3 approachPourPos =
            receive.position + Vector3.up * upOffset + Vector3.right * dirX * sideOffset;
        Vector3 finalPourPos = receive.position;
        debugApproachPos = approachPourPos;
        debugFinalPos = finalPourPos;
        float d = Vector3.Distance(startPourPos, approachPourPos);

        float duration = (speed <= 0f || d <= 0.000001f) ? 0f : d / speed;
        return DOTween.To(() => 0f, tLin =>
        {
            float uMove = DOVirtual.EasedValue(0f, 1f, tLin, moveEase);
            Vector3 pourPos = Vector3.Lerp(startPourPos, approachPourPos, uMove);

            float reach = (tiltReachRatio <= 0.000001f) ? 1f : Mathf.Clamp01(tLin / tiltReachRatio);
            float uRot = DOVirtual.EasedValue(0f, 1f, reach, tiltEase);
            float zAngle = Mathf.Lerp(0f, angleA, uRot);
            ApplyPoseKeepPour(tube, startTubePos, startRot, startPourPos, pourPos, zAngle);

        }, 1f, duration).SetEase(moveEase);
    }

    private void ApplyPoseKeepPour(Transform tube, Vector3 startTubePos, Quaternion startRot,
    Vector3 startPourPos, Vector3 pourPos, float zAngle)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, zAngle);
        tube.rotation = rot;

        Vector3 offset = rot * Quaternion.Inverse(startRot) * (startTubePos - startPourPos);
        tube.position = pourPos + offset;
    }

    private Tween RotateWhileSlidePourToReceiveX_ThenHold(
      Transform tube,
      Transform pour,
      Transform receive,
      float angleTo,
      float xSpeed,
      float rotDur
  )
    {
        Vector3 startTubePos = default;
        Quaternion startRot = default;
        Vector3 startPourPos = default;
        Vector3 targetPourPos = default;

        float angleFrom = 0f;
        float kMove = 1f;           // thời điểm (0..1) mà move hoàn tất
        bool moveCanFinish = true;  // chỉ để bạn debug

        Tweener tw = DOTween.To(() => 0f, t01 =>
        {
            // ROT chạy suốt rotDur
            float uRot = DOVirtual.EasedValue(0f, 1f, t01, tiltEase);
            float z = Mathf.Lerp(angleFrom, angleTo, uRot);

            // MOVE: trong đoạn [0..kMove] thì lerp, sau đó giữ
            Vector3 pourPos;
            if (!moveCanFinish)
            {
                // nếu moveDur > rotDur: vẫn lerp theo t01 (không kịp tới đích)
                float uMove = DOVirtual.EasedValue(0f, 1f, t01, moveEase);
                pourPos = Vector3.Lerp(startPourPos, targetPourPos, uMove);
            }
            else if (t01 <= kMove && kMove > 0.0001f)
            {
                float uMove = DOVirtual.EasedValue(0f, 1f, t01 / kMove, moveEase);
                pourPos = Vector3.Lerp(startPourPos, targetPourPos, uMove);
            }
            else
            {
                pourPos = targetPourPos; // chạm receive.x rồi giữ
            }

            ApplyPoseKeepPour(tube, startTubePos, startRot, startPourPos, pourPos, z);

        }, 1f, rotDur)
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

            float dx = Mathf.Abs(targetPourPos.x - startPourPos.x);
            float moveDur = (xSpeed <= 0f || dx < 0.0001f) ? 0f : dx / xSpeed;

            if (moveDur <= rotDur + 0.0001f)
            {
                moveCanFinish = true;
                kMove = (rotDur <= 0.0001f) ? 1f : Mathf.Clamp01(moveDur / rotDur);
            }
            else
            {
                // không kịp tới receive.x trong rotDur
                moveCanFinish = false;
                kMove = 1f;
            }

            dbg2_startPourPos = startPourPos;
            dbg2_targetPourPos = targetPourPos;
            dbg2_kMove = kMove;
            dbg2_moveCanFinish = moveCanFinish;
            dbg2_has = true;
        });

        return tw;
    }

    private float ComputeAngleA(TubeModel model)
    {
        if (!model.GetTop(out var topSeg)) return 0f;

        float amount01 = Mathf.Clamp01((float)topSeg.Amount / model.capacity);
        return Mathf.Lerp(angleA_Min, angleA_Max, amount01);
    }

    private float ComputePourAngle(TubeModel model)
    {
        if (!model.GetTop(out var TopSeg)) return 0f;
        float amount01 = Mathf.Clamp01((float)TopSeg.Amount / model.capacity);
        return Mathf.Lerp(pourAngleMin, pourAngleMax, amount01);
    }   

    private void OnDrawGizmos()
    {
        if (!hasDebugPos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(debugApproachPos, 0.15f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(debugFinalPos, 0.12f);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(debugApproachPos, debugFinalPos);

        // ======= ĐOẠN 2: đường đi POUR POINT (miệng bình) =======
        if (!debugPhase2 || !dbg2_has) return;

        Gizmos.color = Color.cyan;

        Vector3 prev = dbg2_startPourPos;
        int steps = Mathf.Max(2, debugSteps2);

        for (int i = 1; i <= steps; i++)
        {
            float t01 = i / (float)steps;

            Vector3 p;
            if (!dbg2_moveCanFinish)
            {
                // không kịp tới đích: lerp suốt 0..1
                p = Vector3.Lerp(dbg2_startPourPos, dbg2_targetPourPos, t01);
            }
            else if (t01 <= dbg2_kMove && dbg2_kMove > 0.0001f)
            {
                float u = t01 / dbg2_kMove; // 0..1
                p = Vector3.Lerp(dbg2_startPourPos, dbg2_targetPourPos, u);
            }
            else
            {
                p = dbg2_targetPourPos; // giữ tại receive.x
            }

            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        // marker cho start/target của đoạn 2
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(dbg2_startPourPos, 0.08f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(dbg2_targetPourPos, 0.08f);

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
