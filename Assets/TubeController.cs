using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.GraphicsBuffer;

public class TubeController : Singleton<TubeController>
{
    [SerializeField] private float liftY = 1.3f;
    [SerializeField] private float liftTime = 0.2f;

    [SerializeField] private Ease liftEase = Ease.OutBack;

    [SerializeField] private float pourAboveReceive = 3f;
    [SerializeField] private float sideOffsetAmount = 1.2f;
    [SerializeField] private float moveSpeed = 8f; // units/sec theo quãng đường POUR
    [Range(0f, 0.5f)][SerializeField] private float rotateDelayRatio = 0.1f; // delay 0->angleA trong đoạn 1
    [Range(0.01f, 0.2f)][SerializeField] private float splitBlendRatio = 0.06f; // blend 2 đoạn để không khựng

    [Header("Pour Angles (Z)")]
    [SerializeField] private float angleAtA = -40f;
    [SerializeField] private float angleAtPour = -70f;

    [Header("Pour Ease")]
    [SerializeField] private Ease moveEase = Ease.InOutSine; // ease cho nội suy vị trí
    [SerializeField] private Ease tiltEase = Ease.InOutSine; // ease cho nội suy góc

    [Header("Pour Timing")]
    [SerializeField] private float pourUnitTime = 0.08f; // 1 unit nước mất bao lâu
    [SerializeField] private float pourMinTime = 0.15f;
    [SerializeField] private float pourMaxTime = 0.60f;
    [SerializeField] private float pourHoldTime = 0.2f; // đứng yên cho cảm giác đang chảy

    private TubeView tubeSelected;
    private Vector3 selectedBaseLocalPos;
    private bool locked;
    private Tween currentTween;
    private Sequence seq;
    public void OnTubeClicked(TubeView tube)
    {
        Debug.Log("Tube clicked: " + tubeSelected);
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
        Transform pour = from.pourPoint;
        Transform receive = to.receivePoint;

        // lưu pose lift hiện tại (WORLD) để về lại lift trước khi hạ
        Vector3 liftWorldPos = tube.position;
        Quaternion liftWorldRot = tube.rotation;

        // tính thời gian “đổ”
        int amount = from.model.top.HasValue ? from.model.top.Value.Amount : 0;
        float pourDur = Mathf.Clamp(amount * pourUnitTime, pourMinTime, pourMaxTime);

        seq = DOTween.Sequence();

        // 1) Đi tới + canh + nghiêng (2-stage + blend)
        Tween go = MoveAndPour_TwoStageBlend_World(
            tube, pour, receive,
            upOffset: pourAboveReceive,
            sideOffset: sideOffsetAmount,
            angleA: angleAtA,
            anglePour: angleAtPour,
            speed: moveSpeed,
            delayRatio: rotateDelayRatio,
            blendRatio: splitBlendRatio
        );
        seq.Append(go);

        // 2) Hold để nhìn “chảy”
        seq.AppendInterval(pourHoldTime);

        // 3) Logic đổ
        seq.AppendCallback(() =>
        {
            int amount = from.model.top.Value.Amount;

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
        float backMoveDur = Mathf.Clamp(go.Duration(), 0.15f, 0.6f);

        seq.Append(tube.DORotateQuaternion(liftWorldRot, backRotDur).SetEase(tiltEase));
        seq.Append(tube.DOMove(liftWorldPos, backMoveDur).SetEase(moveEase));

        // 5) Hạ xuống base (LOCAL)
        seq.Append(tube.DOLocalMove(basePos, liftTime).SetEase(liftEase));

        seq.OnComplete(() =>
        {
            tubeSelected.RestoreSortingAfterPour();
            tubeSelected = null;
            locked = false;
        });

        currentTween = seq;
    }

    private Tween MoveAndPour_TwoStageBlend_World(
        Transform tube,
        Transform pour,
        Transform receive,
        float upOffset,
        float sideOffset,
        float angleA,
        float anglePour,
        float speed,
        float delayRatio,
        float blendRatio
    )
    {
        delayRatio = Mathf.Clamp(delayRatio, 0f, 0.95f);
        blendRatio = Mathf.Clamp(blendRatio, 0.01f, 0.2f);

        Vector3 startTubePos = tube.position;
        Quaternion startRot = tube.rotation;
        Vector3 startPourPos = pour.position;

        // đứng cùng phía so với tube nhận
        float dirX = Mathf.Sign(tube.position.x - receive.position.x);
        if (dirX == 0) dirX = 1f;

        Vector3 approachPourPos =
            receive.position + Vector3.up * upOffset + Vector3.right * dirX * sideOffset;

        Vector3 finalPourPos =
            receive.position + Vector3.up * upOffset;

        float d1 = Vector3.Distance(startPourPos, approachPourPos);
        float d2 = Vector3.Distance(approachPourPos, finalPourPos);
        float total = d1 + d2;

        float duration = (speed <= 0f || total <= 0.000001f) ? 0f : total / speed;
        float split = (total <= 0.000001f) ? 1f : (d1 / total);

        float a = Mathf.Clamp01(split - blendRatio * 0.5f);
        float b = Mathf.Clamp01(split + blendRatio * 0.5f);

        return DOTween.To(() => 0f, tLin =>
        {
            // --- SEG1 ---
            float u1Lin = (split <= 0.000001f) ? 1f : Mathf.Clamp01(tLin / split);

            float u1Move = DOVirtual.EasedValue(0f, 1f, u1Lin, moveEase);
            Vector3 p1 = Vector3.Lerp(startPourPos, approachPourPos, u1Move);

            float rt1Lin = (u1Lin <= delayRatio) ? 0f : Mathf.InverseLerp(delayRatio, 1f, u1Lin);
            float u1Rot = DOVirtual.EasedValue(0f, 1f, rt1Lin, tiltEase);
            float z1 = Mathf.Lerp(0f, angleA, u1Rot);

            // --- SEG2 ---
            float u2Lin = (1f - split <= 0.000001f) ? 1f : Mathf.Clamp01(Mathf.InverseLerp(split, 1f, tLin));

            float u2Move = DOVirtual.EasedValue(0f, 1f, u2Lin, moveEase);
            Vector3 p2 = Vector3.Lerp(approachPourPos, finalPourPos, u2Move);

            float u2Rot = DOVirtual.EasedValue(0f, 1f, u2Lin, tiltEase);
            float z2 = Mathf.Lerp(angleA, anglePour, u2Rot);

            // --- BLEND ---
            float w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, tLin));
            Vector3 pourPos = Vector3.Lerp(p1, p2, w);
            float z = Mathf.Lerp(z1, z2, w);

            // apply world pose nhưng giữ pour đúng đường
            Quaternion rot = Quaternion.Euler(0f, 0f, z);
            tube.rotation = rot;

            Vector3 offset = rot * Quaternion.Inverse(startRot) * (startTubePos - startPourPos);
            tube.position = pourPos + offset;

        }, 1f, duration).SetEase(Ease.Linear);
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
