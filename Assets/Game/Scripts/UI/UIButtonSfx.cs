using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonSfx : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private SfxCue cue = SfxCue.ButtonClick;
    [SerializeField] private float volumeScale = 0.5f;

    private void OnEnable()
    {
        if (button == null) return;

        button.onClick.RemoveListener(PlayCue);
        button.onClick.AddListener(PlayCue);
    }

    private void OnDisable()
    {
        if (button == null) return;
        button.onClick.RemoveListener(PlayCue);
    }

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void PlayCue()
    {
        AudioManager.Ins?.PlaySFX(cue, volumeScale);
    }
}
