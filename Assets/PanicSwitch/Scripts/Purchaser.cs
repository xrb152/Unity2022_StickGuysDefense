// IAP（内购）相关功能已整体移除。
// 这里保留 Purchaser 类及其原本挂在购买按钮上的公开方法签名（Buy_Gem / Buy_Coin /
// Buy_GemCoin / InitializePurchasing / RestorePurchases / GetPrice 等），
// 改为空实现，以保证预制体与 UI 按钮的 OnClick 引用不会因为脚本缺失而丢失，
// 同时不再依赖 UnityEngine.Purchasing。

using UnityEngine;

namespace CompleteProject
{
    public class Purchaser : MonoBehaviour
    {
        public void InitializePurchasing() { }

        public void Buy_Gem() { }
        public void Buy_Coin() { }
        public void Buy_GemCoin() { }

        public void RestorePurchases() { }

        public string GetPrice(string productID) { return ""; }
        public string GetPriceAnalythics(string productID) { return ""; }
        public string GetCurrency(string productID) { return ""; }
    }
}
