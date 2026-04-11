using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelUI : UIPanel
{
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Button closeButton;

    [Header("Tab Button")]
    [SerializeField] private Button packTabBtn;
    [SerializeField] private Button skinCapsTabBtn;

    [Header("Tab Panels")]
    [SerializeField] private GameObject packTabPanel;
    [SerializeField] private GameObject skinCapsTabPanel;
    [Header("Tab Visual")]
    [SerializeField] private Sprite activeTabSprite;
    [SerializeField] private Sprite inactiveTabSprite;

    private List<Button> tabButtons;
    private List<GameObject> tabPanels;
    private ShopTab currentTab = ShopTab.Packages;

    public event Action OnCloseShop;

    private void Awake()
    {
        InitializeTabs();

        if(closeButton) closeButton.onClick.AddListener(() => OnCloseShop?.Invoke());

        if(packTabBtn) packTabBtn.onClick.AddListener(() => SwitchTab(ShopTab.Packages));
        if(skinCapsTabBtn) skinCapsTabBtn.onClick.AddListener(() => SwitchTab(ShopTab.TubeCaps));
    }

    public override void Show()
    {
        base.Show();
        RefreshCoins();
        InitializeTabs();
        SwitchTab(ShopTab.Packages);

        if (ShopService.Ins != null)
        {
            ShopService.Ins.OnCoinsChanged -= HandleCoinsChanged;
            ShopService.Ins.OnCoinsChanged += HandleCoinsChanged;
        }
    }

 
    public override void Hide()
    {
        if (ShopService.Ins != null)
            ShopService.Ins.OnCoinsChanged -= HandleCoinsChanged;
        base.Hide();
    }
    private void SwitchTab(ShopTab tab)
    {
        InitializeTabs();
        if (tabButtons == null || tabPanels == null) return;

        currentTab = tab;
        int activeIndex = (int)tab;

        for (int i = 0; i < tabButtons.Count; i++)
        {
            if (tabButtons[i] == null) continue;

            Image img = tabButtons[i].GetComponent<Image>();
            if (img != null)
                img.sprite = (i == activeIndex) ? activeTabSprite : inactiveTabSprite;

            if (i < tabPanels.Count && tabPanels[i] != null)
                tabPanels[i].SetActive(i == activeIndex);
        }
    }
    private void RefreshCoins()
    {
        if(coinText && ShopService.Ins != null)
        {
            coinText.text = ShopService.Ins.Coins.ToString();
        }
    }
    private void HandleCoinsChanged(int newAmount)
    {
        if (coinText) coinText.text = newAmount.ToString();
    }

    private void InitializeTabs()
    {
        tabButtons ??= new List<Button>();
        tabPanels ??= new List<GameObject>();

        tabButtons.Clear();
        tabPanels.Clear();

        tabButtons.Add(packTabBtn);
        tabButtons.Add(skinCapsTabBtn);

        tabPanels.Add(packTabPanel);
        tabPanels.Add(skinCapsTabPanel);
    }
}

