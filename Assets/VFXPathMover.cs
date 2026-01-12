using DG.Tweening;
using System;
using UnityEngine;

public class VFXPathMover : MonoBehaviour
{
    [SerializeField] private ParticleSystem ps;
    [SerializeField] private Ease ease = Ease.OutSine;
    [SerializeField] private bool catmullRom = true;
    [SerializeField] private float extraFadeDelay = 0.05f;

    private Tween tween;
    private void Reset()
    {
        ps = GetComponent<ParticleSystem>();
    }

    public void MoveAlongPath(Vector3[] path, float duration)
    {
        tween?.Kill();
        var emission = ps.emission;
        emission.enabled = false;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Clear();
        ps.Play();

        transform.position = path[0];
        var pathType = catmullRom ? PathType.CatmullRom : PathType.Linear;

        tween = transform.DOPath(path, duration, pathType)
            .SetEase(ease)
            .OnComplete(() =>
            {
                emission.enabled = false;
                float maxLife = GetMaxStartLifeTime(ps);
                DOVirtual.DelayedCall(maxLife + extraFadeDelay, () =>
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    gameObject.SetActive(false);
                });
            });
    }

    public void StopImmediate()
    {
        tween?.Kill();
        if(ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear();
        }
        gameObject.SetActive(false);
    }
    private float GetMaxStartLifeTime(ParticleSystem ps)
    {
        var lt = ps.main.startLifetime;
        if (lt.mode == ParticleSystemCurveMode.Constant) return lt.constant;
        if (lt.mode == ParticleSystemCurveMode.TwoConstants) return Mathf.Max(lt.constantMin, lt.constantMax);
        return Mathf.Max(1f, lt.constantMax);
    }
}
