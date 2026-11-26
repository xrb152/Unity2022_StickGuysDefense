using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 激励广告弹窗
/// </summary>
public class AdConfirm : MonoBehaviour
{
    private static AdConfirm instance;

    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnConfirm;
    private Action onCompleted;

    public static void Show(Action callBack)
    {
        if (instance == null)
        {
            // 加载 AdConfirm 预制体
            GameObject prefab = Resources.Load<GameObject>("AdConfirm");
            if (prefab == null)
            {
                Debug.LogError("AdConfirm 预制体未找到，请确保其位于 Resources 文件夹中。");
                return;
            }

            // 实例化 AdConfirm
            GameObject adConfirmInstance = Instantiate(prefab);
            instance = adConfirmInstance.GetComponent<AdConfirm>();

            if (instance == null)
            {
                Debug.LogError("AdConfirm 脚本未附加到预制体上，请检查预制体设置。");
                Destroy(adConfirmInstance);
                return;
            }
        }

        // 设置回调并显示弹窗
        instance.onCompleted = callBack;
        instance.gameObject.SetActive(true);
    }

    /// <summary>
    /// 不弹确认框，直接播放激励视频，看完后执行回调。
    /// 用于奖励不是金币的入口（升级 / 加成 / 法术等），避免复用弹窗上「添加金币」的文案。
    /// </summary>
    public static void PlayDirect(Action callBack)
    {
#if UNITY_EDITOR
        // 编辑器下没有真实广告，直接发奖
        callBack?.Invoke();
#else
        if (!EnsureAdManager())
        {
            ShowAdError("广告服务未初始化");
            return;
        }

        AdManager.Instance.ShowRewardedVideoAd(
            () =>
            {
                Debug.Log("广告观看完成（直接播放）");
                callBack?.Invoke();
            },
            ShowAdError);
#endif
    }

    /// <summary>确保场景里有 AdManager；没有就兜底补一个。返回是否可用。</summary>
    private static bool EnsureAdManager()
    {
        if (AdManager.Instance == null)
        {
            // 场景里没摆 AdManager 时兜底补一个，避免广告直接失效。
            // 注意：自动创建时用的是默认广告位，正式上线请在场景里手动放 AdManager 并填好 AppId / 广告位ID。
            Debug.LogWarning("[广告] 场景里没有 AdManager，已自动创建一个（默认广告位）");
            new GameObject("AdManager(Auto)").AddComponent<AdManager>();
        }

        return AdManager.Instance != null;
    }

    /// <summary>广告失败提示（AdError 自带 Canvas，位置无关）。</summary>
    private static void ShowAdError(string errorMessage)
    {
        Debug.LogError($"广告播放失败: {errorMessage}");

        GameObject adErrorPrefab = Resources.Load<GameObject>("AdError");
        if (adErrorPrefab != null)
        {
            GameObject adErrorInstance = Instantiate(adErrorPrefab, Vector3.zero, Quaternion.identity);
            Destroy(adErrorInstance, 1.5f);
        }
        else
        {
            Debug.LogError("AdError 预制体未找到，请确保其位于 Resources 文件夹中。");
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        gameObject.SetActive(false);
        btnClose?.onClick.AddListener(this.OnClose);
        btnConfirm?.onClick.AddListener(this.OnConfirm);
    }


    private void OnClose()
    {
        this.gameObject.SetActive(false);
    }

    void OnConfirm()
    {
#if UNITY_EDITOR
        onCompleted?.Invoke();
        this.OnClose();
#else
        if (!EnsureAdManager())
        {
            ShowAdError("广告服务未初始化");
            return;
        }

        AdManager.Instance.ShowRewardedVideoAd(OnAdWatched, OnAdError);
#endif
    }

    private void OnAdWatched()
    {
        Debug.Log("广告观看完成");
        onCompleted?.Invoke();
        OnClose();
    }

    private void OnAdError(string errorMessage)
    {
        ShowAdError(errorMessage);
    }
}
