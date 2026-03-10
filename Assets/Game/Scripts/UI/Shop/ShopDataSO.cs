using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopDataSO", menuName = "Game/Shop Data")]
public class ShopDataSO : ScriptableObject
{
    public List<IAPPackageData> packages = new();
    public List<CosmeticItemData> tubeCaps = new();
    public List<IAPPackageData> backgrounds = new();
}
