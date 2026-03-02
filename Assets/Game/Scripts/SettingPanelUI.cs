using System;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class SettingPanelUI : UIPanel
{
    [SerializeField] private GameObject dimmer;

    [Header("Toggle Buttons")]
    [SerializeField] private Button soundButton;
    [SerializeField] private Button musicButton;
    [SerializeField] private Button hapticButton;

    [Header("Toggle Images")]
    [SerializeField] private Image soundImage;
    [SerializeField] private Image musicImage;
    [SerializeField] private Image hapticImage;

    [Header("Toggle Colors")]
    [SerializeField] private Color onColor = Color.white;
    [SerializeField] private Color offColor = new Color(1f,1f,1f,0.4f);

    [Header("Action Buttons")]
    [SerializeField] private Button replayButton;
    [SerializeField] private Button rateUsButton;
    [SerializeField] private Button restorePurchaseButton;
    [SerializeField] private Button closeButton;

    [Header("Links")]
    [SerializeField] private Button privacyPolicyButton;
    [SerializeField] private string privacyPolicyURL = "https://www.example.com/privacy-policy";

    public event Action OnClose;
    public event Action OnReplay;

    private bool hapticEnabled = true;

    private void Awake()
    {
        //Toggles
        if(soundButton) soundButton.onClick.AddListener(ToggleSound);
        if(musicButton) musicButton.onClick.AddListener(ToggleMusic);
        if(hapticButton) hapticButton.onClick.AddListener(ToggleHaptic);

        //Actions
        if (closeButton) closeButton.onClick.AddListener(() => OnClose?.Invoke());
        if (replayButton) replayButton.onClick.AddListener(() => OnReplay?.Invoke());
        if(rateUsButton) rateUsButton.onClick.AddListener(OpenRateUs);
        if(restorePurchaseButton) restorePurchaseButton.onClick.AddListener(RestorePurchase);
        if (privacyPolicyButton) privacyPolicyButton.onClick.AddListener(() => Application.OpenURL(privacyPolicyURL));
    }

    public override void Show()
    {
        base.Show();
        if (dimmer) dimmer.SetActive(true);
        RefreshToggles();
    }

    public override void Hide()
    {
        if (dimmer) dimmer.SetActive(false);
        base.Hide();
    }

    private void ToggleSound()
    {
        var audio = AudioManager.Ins;
        if (audio == null) return;

        bool newState = !audio.IsSFXEnabled();
        audio.SetSFXEnabled(newState);
        UpdateToggleVisual(soundImage, newState);
    }
    private void ToggleMusic()
    {
      var audio = AudioManager.Ins;
        if (audio == null) return;
        bool newState = !audio.IsMusicEnabled();
        audio.SetMusicEnabled(newState);
        UpdateToggleVisual(musicImage, newState);
    }

    private void ToggleHaptic() 
    {
        hapticEnabled = !hapticEnabled;
        PlayerPrefs.SetInt("HapticEnabled", hapticEnabled ? 1:0);
        PlayerPrefs.Save();
        UpdateToggleVisual(musicImage, hapticEnabled);
    }

    private void RefreshToggles()
    {
        AudioManager audio = AudioManager.Ins;
        if (audio != null)
        {
            UpdateToggleVisual(soundImage, audio.IsSFXEnabled());
            UpdateToggleVisual(musicImage, audio.IsMusicEnabled());
        }

        hapticEnabled = PlayerPrefs.GetInt("HapticEnabled", 1) == 1; // Default to enabled
        UpdateToggleVisual(hapticImage, hapticEnabled);
    }

    private void UpdateToggleVisual(Image img, bool isOn)
    {
        if (img) img.color = isOn ? onColor : offColor;
    }

    private void OpenRateUs()
    {
#if UNITY_ANDROID
        Application.OpenURL("market://details?id=" + Application.identifier);
#elif UNITY_IOS
        // TODO: thay bằng Apple ID thực
        Application.OpenURL("itms-apps://itunes.apple.com/app/idYOUR_APP_ID");
#endif
    }

    private void RestorePurchase()
    {
        // TODO: Gọi IAP restore
        Debug.Log("[Setting] Restore Purchase");
    }

}
