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
    [SerializeField] private Button backgroundTabBtn;

    [Header("Tab Panels")]
    [SerializeField] private GameObject packTabPanel;
    [SerializeField] private GameObject skinCapsTabPanel;
    [SerializeField] private GameObject backgroundTabPanel;

    [Header("Tab Visual")]
    [SerializeField] private Color activeTabColor = Color.yellow;
    [SerializeField] private Color inactiveTabColor = new Color(0.6f, 0.7f, 1f);

    private List<Button> tabButtons;
    private List<GameObject> tabPanels;
    private ShopTab currentTab = ShopTab.Packages;

    public event Action OnCloseShop;

    private void Awake()
    {
        tabButtons = new List<Button> { packTabBtn, skinCapsTabBtn, backgroundTabBtn };
        tabPanels = new List<GameObject> { packTabPanel, skinCapsTabPanel, backgroundTabPanel };

        if(closeButton) closeButton.onClick.AddListener(() => OnCloseShop?.Invoke());

        if(packTabBtn) packTabBtn.onClick.AddListener(() => SwitchTab(ShopTab.Packages));
        if(skinCapsTabBtn) skinCapsTabBtn.onClick.AddListener(() => SwitchTab(ShopTab.TubeCaps));
        if(backgroundTabBtn) backgroundTabBtn.onClick.AddListener(() => SwitchTab(ShopTab.Backgrounds));
    }

    public override void Show()
    {
        base.Show();
        RefreshCoins();
        SwitchTab(ShopTab.Packages);

        if (ShopService.Ins != null)
            ShopService.Ins.OnCoinsChanged += HandleCoinsChanged;
    }

 
    public override void Hide()
    {
        if (ShopService.Ins != null)
            ShopService.Ins.OnCoinsChanged -= HandleCoinsChanged;
        base.Hide();
    }
    private void SwitchTab(ShopTab packages)
    {
        currentTab = packages;
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
}

