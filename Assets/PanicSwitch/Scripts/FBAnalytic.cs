// Facebook SDK 相关逻辑已移除（原工程未包含 Facebook.Unity 程序集）。
// 仅保留 GameAnalytics 初始化，避免破坏预制体上的 FBAnalytic 组件引用。

using UnityEngine;
using GameAnalyticsSDK;

public class FBAnalytic : MonoBehaviour
{
    void Awake()
    {
        GameAnalytics.Initialize();
    }
}
