using System;
using System.Collections;
using UnityEngine;

public class ComboTracker : Singleton<ComboTracker>
{
    [SerializeField] public float comboResetTime = 5f;

    private int currentCombo = 0;
    private Coroutine resetCoroutine = null;

    public int CurrentCombo => currentCombo;
    public event Action<int> OnComboChanged;
    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        // Clear all event subscribers
        OnComboChanged = null;
    }

    public void RegisterSuccessfulPour()
    {
        currentCombo++;

        if (OnComboChanged != null) OnComboChanged.Invoke(currentCombo);

        if (resetCoroutine != null) StopCoroutine(resetCoroutine);
        resetCoroutine = StartCoroutine(ResetComboAfterDelay());
    }

    public void ResetCombo()
    {
        if (currentCombo < 1) return;
        currentCombo = 0;
        OnComboChanged?.Invoke(currentCombo);

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }
    }
    IEnumerator ResetComboAfterDelay()
    {
        yield return new WaitForSeconds(comboResetTime);
        ResetCombo();
    }

    public int GetComboScoreMultiplier()
    {
        // to do
        return 1;
    }
}
