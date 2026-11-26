using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 上的「免费金币」按钮：点击弹出广告确认框，看完抖音激励视频发放金币。
/// </summary>
[RequireComponent(typeof(Button))]
public class FreeCoinButton : MonoBehaviour
{
    [SerializeField]
    private int rewardAmount = 200;

    private Wallet wallet;
    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
        // 项目里 Wallet 是全局单例式组件（FindObjectOfType），与 PowerUp / Tower 等脚本一致
        wallet = FindObjectOfType<Wallet>();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }
    }

    public void OnClick()
    {
        Debug.Log("[广告] 点击免费金币按钮，弹出激励视频确认框");
        AdConfirm.Show(GrantReward);
    }

    private void GrantReward()
    {
        if (wallet == null)
        {
            wallet = FindObjectOfType<Wallet>();
        }
        if (wallet == null)
        {
            Debug.LogError("[广告] 金币系统（Wallet）未找到，无法发放奖励");
            return;
        }

        wallet.SetMoney(rewardAmount);
        Debug.Log($"[广告] 激励视频完成，发放金币 {rewardAmount}");
    }
}
