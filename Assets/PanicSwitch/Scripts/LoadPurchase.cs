// IAP（内购）相关功能已整体移除。
// 保留 LoadPurchase 组件名（不再依赖 UnityEngine.Purchasing），供预制体引用。

using UnityEngine;

namespace CompleteProject
{
    public class LoadPurchase : MonoBehaviour
    {
        public string GetPrice(string productID) { return ""; }
    }
}
