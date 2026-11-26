using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_WEBGL && !UNITY_EDITOR
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
#endif

/*
 * 抖音平台广告（真机实现：仅在 WebGL 非编辑器环境编译；编辑器下见文件末尾占位实现）
 */
#if UNITY_WEBGL && !UNITY_EDITOR
public class TikTokPlatform : IAdPlatform
{
    /// <summary>
    /// 是否向第三方（ecpm.xxhygames.com）上报 openid/机型等数据做 ECPM 与转化回传。
    /// 默认关闭：上面报会把用户信息发到外部服务器，抖音审核和隐私政策里必须申报，
    /// 确认是自己的后台、且已在上架材料里写明后再改成 true。
    /// </summary>
    public static bool EnableRemoteTracking = false;

    // 小游戏ID
    private readonly string miniAppid;

    // 激励视频
    private TTRewardedVideoAd rewardedVideoAd = null;
    private string videoAdId = "";
    private Action onRewardSuccess;
    private Action<string> onRewardFail;
    private float prevTimeScale = 1f;

    // 插屏广告
    private TTInterstitialAd interAdIns;
    private string interAdId = "";
    private float moduleStartTime;
    private float lastShowTime;

    // 埋点
    private readonly string ecpmURL = "https://ecpm.xxhygames.com/openid/openlog/insertlog";  // ECPM后台地址
    private readonly string conversionURL = "https://analytics.oceanengine.com/api/v2/conversion";   // 巨量引擎API接口

    private TTSystemInfo systemInfo;

    public TikTokPlatform(string appId, string videoAdId, string interAdId)
    {
        this.miniAppid = appId ?? "";
        this.videoAdId = videoAdId;
        this.interAdId = interAdId;
        if (string.IsNullOrEmpty(this.miniAppid))
        {
            // AppID 只用于 ECPM/转化回传，广告本身按广告位 ID 播放，不影响收益流程。
            Debug.Log("[抖音广告] 未填写小游戏 AppID，广告可正常播放，仅埋点上报不可用");
        }
        else if (EnableRemoteTracking && !this.miniAppid.EndsWith("07"))
        {
            // 只有开启回传时才需要 AppID 格式校验，否则这条警告纯属噪音
            Debug.LogWarning($"[抖音广告] AppID「{this.miniAppid}」格式可能不匹配回传接口，广告播放不受影响");
        }

        if (EnableRemoteTracking)
        {
            try
            {
                this.Login();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[抖音广告] 埋点初始化失败（不影响广告）：{e.Message}");
            }
        }

        // 编辑器里 TT 接口是 mock，可能直接抛异常；真机上按正常流程创建。
        try
        {
            this.CreateRewardedVideoAd();
            this.CreateInterstitialAd();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[抖音广告] 初始化广告实例失败（真机上会自动重试）：{e.Message}");
        }

        moduleStartTime = Time.realtimeSinceStartup;
        lastShowTime = -30f;
    }


    #region 插屏广告

    /// 创建并播放插屏广告，在广告模块启动15s内不允许展示插屏广告，在任何地方调用API.GetStarkAdManager，就会启动广告模块
    /// 两个插屏广告展示间隔不能少于30s
    /// 插屏广告支持多实例。简单回调设置，插屏广告完成和错误的回调函数
    /// 插屏广告实例只支持展示一次，出现加载错误，或 展示成功，点击关闭按钮或视频广告外的其他地方，广告实例都会自动销毁。下一次需再次创建。
    /// 提供接口可以手动销毁
    /// 错误码由sdk传出，原因参考
    /// https://microapp.bytedance.com/docs/zh-CN/mini-game/develop/open-capacity/ads/tt-create-interstitial-ad
    public override void ShowInterstitialAd(string AdId)
    {
        Debug.Log("播放插屏广告");

        // 广告位变化时重建实例，避免继续使用旧广告位
        if (!string.IsNullOrEmpty(AdId) && this.interAdId != AdId)
        {
            this.interAdId = AdId;
            this.RemoveInterstitialAd();
        }

        float now = Time.realtimeSinceStartup;
        if (now - moduleStartTime < 15f)
        {
            Debug.LogWarning("广告模块启动15秒内不能展示插屏广告");
            return;
        }
        if (now - lastShowTime < 30f)
        {
            float remainingTime = 30f - (now - lastShowTime);
            Debug.LogWarning($"两次插屏广告展示间隔需大于30秒，剩余时间: {remainingTime:F1} 秒");
            return;
        }

        if (interAdIns == null)
        {
            try
            {
                this.CreateInterstitialAd();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[抖音广告] 创建插屏失败：{e.Message}");
            }
            Debug.LogWarning("插屏广告开始加载，本次先不展示");
            return;
        }

        bool loaded = false;
        try
        {
            loaded = interAdIns.IsLoaded();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[抖音广告] 查询插屏状态异常：{e.Message}");
        }

        if (!loaded)
        {
            Debug.LogWarning("插屏广告尚未加载完成，本次跳过");
            return;
        }

        try
        {
            interAdIns.Show();
            lastShowTime = now;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[抖音广告] 展示插屏异常：{e.Message}");
        }
    }

    private void CreateInterstitialAd()
    {
        interAdIns = TT.CreateInterstitialAd(new CreateInterstitialAdParam { InterstitialAdId = this.interAdId });
        interAdIns.OnClose += () =>
        {
            Debug.Log("插屏广告关闭");
            // 1.销毁实例
            this.RemoveInterstitialAd();
            // 2.重新创建新的广告实例
            try { this.CreateInterstitialAd(); }
            catch (Exception e) { Debug.LogWarning($"[抖音广告] 预创建插屏失败：{e.Message}"); }
            // 3.发送埋点
            this.SendGameAddiction();
        };
        interAdIns.OnLoad += () => Debug.Log("插屏广告加载完成");
        interAdIns.OnError += (code, message) =>
        {
            Debug.LogWarning($"插屏广告异常 ： {code}  {message}");
            this.RemoveInterstitialAd();
        };
        interAdIns.Load();
    }

    private void RemoveInterstitialAd()
    {
        interAdIns?.Destroy();
        interAdIns = null;
    }

    #endregion


    #region 侧边栏

    public override void NavigateToScene(Action onSuccess, Action<string> onFail)
    {
        var data = new JsonData { ["scene"] = "sidebar" };
        TT.NavigateToScene(data,
            () =>
            {
                Debug.Log("侧边栏唤起成功");
                onSuccess?.Invoke();
            },
            () => { },
            (errCode, errMsg) =>
            {
                onFail?.Invoke(errMsg);
                Debug.LogError($"侧边栏显示异常:{errCode}, {errMsg}");
            });
    }

    #endregion

    #region 激励视频
    public override void ShowRewardedVideoAd(string videoAdId, Action onSuccess, Action<string> onFail)
    {
        Debug.Log("播放激励视频广告");
        this.prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0; // 看广告需要暂停游戏

        // 广告位发生变化时必须重建实例，否则会继续播旧的广告位
        bool needRecreate = rewardedVideoAd == null || this.videoAdId != videoAdId;
        this.videoAdId = videoAdId;
        onRewardSuccess = onSuccess;
        onRewardFail = onFail;

        if (needRecreate)
        {
            this.DelRewardedVideoAd();
            try
            {
                this.CreateRewardedVideoAd();
            }
            catch (Exception e)
            {
                Debug.LogError($"[抖音广告] 创建激励视频异常：{e.Message}");
            }
        }

        if (rewardedVideoAd == null)
        {
            // 创建失败必须恢复暂停，否则游戏会一直卡在 timeScale=0
            var fail = onRewardFail;
            this.RestoreTimeScale();
            fail?.Invoke("激励视频创建失败");
            return;
        }

        try
        {
            rewardedVideoAd.Show();
        }
        catch (Exception e)
        {
            Debug.LogError($"[抖音广告] 展示激励视频异常：{e.Message}");
            var fail = onRewardFail;
            this.RestoreTimeScale();
            fail?.Invoke("激励视频展示失败");
        }
    }

    private void RestoreTimeScale()
    {
        onRewardSuccess = null;
        onRewardFail = null;
        Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
    }

    private void CreateRewardedVideoAd()
    {
        rewardedVideoAd = TT.CreateRewardedVideoAd(new CreateRewardedVideoAdParam() { AdUnitId = videoAdId });
        rewardedVideoAd.OnClose += (ended, count) =>
        {
            
            Debug.Log($"是否看完广告:{ended} 次数:{count}");
            var success = onRewardSuccess;
            var fail = onRewardFail;
            this.RestoreTimeScale();
            if (ended)
            {
                success?.Invoke();
                this.SendGameAddiction();
            }
            else
            {
                Debug.Log("广告未看完，不发放奖励");
                fail?.Invoke("广告未看完");
            }

            // 关闭广告后,创建新的实例（失败也不影响下次点击，下次会自动重建）
            this.DelRewardedVideoAd();
            try { this.CreateRewardedVideoAd(); }
            catch (Exception e) { Debug.LogWarning($"[抖音广告] 预创建激励视频失败：{e.Message}"); }
        };
        rewardedVideoAd.OnError += (errorCode, errorMessage) =>
        {
            this.DelRewardedVideoAd();
            var fail = onRewardFail;
            this.RestoreTimeScale();
            Debug.LogError($"激励视频错误 errorCode: {errorCode}, message: {errorMessage}");
            fail?.Invoke(string.IsNullOrEmpty(errorMessage) ? $"广告加载失败（{errorCode}）" : errorMessage);
            // 不在这里重建实例，避免广告位无效时反复创建；下次点击时 needRecreate 会自动重建
        };
    }

    private void DelRewardedVideoAd()
    {
        rewardedVideoAd?.Destroy();
        rewardedVideoAd = null;
    }

    #endregion


    #region 埋点

    private void Login()
    {
        // 1.获取系统信息
        systemInfo = TT.GetSystemInfo();
        if (systemInfo?.system == null)
            return;

        // 2.登录-获取临时登录凭证 
        TT.Login(this.onLoginSuccess, (errMsg) =>
        {
            Debug.LogError("登录失败:" + errMsg);
        }, true);
        // 3.初始化埋点
        this.InitMaidian();
    }

    private void onLoginSuccess(string code, string anonymousCode, bool isLogin)
    {
        // 2.1获取用户的基本信息
        TT.GetUserInfo((ref TTUserInfo scUserInfo) => this.onGetUserInfo(ref scUserInfo, code, anonymousCode),
            (msg) =>
            {
                Debug.LogError($"用户信息获取失败: {msg}");
            });
    }

    private void onGetUserInfo(ref TTUserInfo scUserInfo, string code, string anonymousCode)
    {
        // 2.2向ECPM后台上传设备信息
        Debug.Log(scUserInfo);
        AdManager.Instance.StartCoroutine(this.SendOpenIdLog(
            miniAppid, scUserInfo.nickName, code, anonymousCode, systemInfo, (success, res) =>
            {
                if (success && res.d != null && res.d.user != null && !string.IsNullOrEmpty(res.d.user.openid))
                {
                    string useropenid = res.d.user.openid;
                    Debug.Log("openidlog==succ== " + res);
                    string currentDate = DateTime.UtcNow.ToString("o");
                    PlayerPrefs.SetString(miniAppid + TT.GetSystemInfo().model, currentDate);
                }
                else
                {
                    Debug.Log("openidlog==fail== " + res);
                }
            }));
    }

    private IEnumerator SendOpenIdLog(string appid, string nickName, string code, string anonymousCode, TTSystemInfo sysinfores, Action<bool, OpenIdResponse> callback)
    {
        var query = $"?appid={Uri.EscapeDataString(appid)}" +
                    $"&nickName={Uri.EscapeDataString(nickName)}" +
                    $"&code={Uri.EscapeDataString(code)}" +
                    $"&anonymousCode={Uri.EscapeDataString(anonymousCode)}" +
                    $"&system={Uri.EscapeDataString(sysinfores.system)}" +
                    $"&platform={Uri.EscapeDataString(sysinfores.platform)}" +
                    $"&brand={Uri.EscapeDataString(sysinfores.brand)}" +
                    $"&modelx={Uri.EscapeDataString(sysinfores.model)}" +
                    $"&version={Uri.EscapeDataString(sysinfores.ttVersion)}" +
                    $"&SDKVersion={Uri.EscapeDataString(sysinfores.sdkVersion)}" +
                    $"&appName={Uri.EscapeDataString(sysinfores.hostName)}";

        var www = UnityWebRequest.Get(this.ecpmURL + query);
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("openidlog==fail== " + www.error);
            callback(false, null);
        }
        else
        {
            try
            {
                var response = JsonUtility.FromJson<OpenIdResponse>(www.downloadHandler.text);
                callback(true, response);
            }
            catch (Exception ex)
            {
                Debug.LogError("SendOpenIdLog ERROR：" + ex.Message);
                callback(false, null);
            }
        }
    }

    private void InitMaidian()
    {
        // 3.1获取小游戏启动时的参数
        var launchOptions = TT.GetLaunchOptionsSync();
        Debug.Log($"小游戏启动参数: {launchOptions}");
        if (launchOptions == null || launchOptions.Query == null || !launchOptions.Query.ContainsKey("ad_params"))
        {
            Debug.LogWarning($"小游戏:{miniAppid}没添加埋点参数");
            return;
        }

        string decodedString = Uri.UnescapeDataString(launchOptions.Query["ad_params"]);
        AdParamsData jsonval = JsonUtility.FromJson<AdParamsData>(decodedString);
        string clickid = jsonval?.log_extra?.clickid;
        if (string.IsNullOrEmpty(clickid))
        {
            Debug.LogWarning($"小游戏:{miniAppid}没点击事件埋点");
            return;
        }

        Debug.Log($"clickid: {clickid}");
        PlayerPrefs.SetString(miniAppid + "ClickId", clickid);
        if (!CanClaimReward(miniAppid + "Active"))
        {
            Debug.LogWarning($"小游戏:{miniAppid}奖励已领取，无法重复领取");
            return;
        }

        // 3.2 API上报激活数据
        long timenow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        AdManager.Instance.StartCoroutine(SendConversionRequest(
            "active", clickid, timenow,
            (success, response) =>
            {
                Debug.Log($"激活埋点事件==={(success ? "success" : "fail")}");
                Debug.Log($"激活结果={response}");
            }
        ));
        string currentDate = DateTime.UtcNow.ToString("o");
        PlayerPrefs.SetString(miniAppid + "Active", currentDate);
    }

    /*是否允许领取奖励*/
    private bool CanClaimReward(string key)
    {
        string lastClaimDateStr = PlayerPrefs.GetString(miniAppid + key);
        if (string.IsNullOrEmpty(lastClaimDateStr))
        {
            return true;
        }

        if (!DateTime.TryParse(lastClaimDateStr, out DateTime lastClaimDate))
        {
            return true;
        }
        DateTime oneMonthAgo = DateTime.Now.AddMonths(-1);
        return lastClaimDate < oneMonthAgo;
    }

    /// <summary>
    /// API上报关键行为
    /// </summary>
    private void SendGameAddiction()
    {
        string clickid = PlayerPrefs.GetString(miniAppid + "ClickId");
        if (string.IsNullOrEmpty(clickid))
        {
            Debug.LogWarning($"小游戏:{miniAppid}没点击事件埋点");
            return;
        }

        long timenow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        AdManager.Instance.StartCoroutine(SendConversionRequest(
                "game_addiction", clickid, timenow,
                (success, response) =>
                {
                    Debug.Log($"埋点事件===={(success ? "success" : "fail")}");
                    Debug.Log($"关键行为结果={response}");
                }
            ));
        Debug.Log("已触发激活埋点");
    }

    /// <summary>
    /// 转化回传 API
    /// </summary>
    /// <param name="eventType">回传的事件：</param>
    /// <param name="clickid">clickid 参数</param>
    /// <param name="timestamp">事件发生的毫秒级时间戳</param>
    /// <param name="callback"></param>
    /// https://event-manager.oceanengine.com/docs/8650/app_api_docs
    /// <returns></returns>
    public IEnumerator SendConversionRequest(string eventType, string clickid, long timestamp, Action<bool, string> callback)
    {
        var data = new ConversionData
        {
            event_type = eventType,
            context = new Context { ad = new Ad { callback = clickid } },
            timestamp = timestamp
        };

        string json = JsonUtility.ToJson(data);
        byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);

        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Content-Type", "application/json");

        using var www = new UnityWebRequest(conversionURL, "POST");
        www.uploadHandler = new UploadHandlerRaw(postData);
        www.downloadHandler = new DownloadHandlerBuffer();
        foreach (var header in headers)
        {
            www.SetRequestHeader(header.Key, header.Value);
        }
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            callback(false, www.error);
        }
        else
        {
            callback(true, www.downloadHandler.text);
        }
    }
    public override void ShareGame(Action<bool> callBack)
    {
        // 通用分享
        JsonData shareJson = new JsonData();
        shareJson["channel"] = "video";
        shareJson["title"] = "Some Title";
        shareJson["extra"] = new JsonData();
        shareJson["extra"]["videoPath"] = "/xxx/xxx.mp4";//录屏分享的话，路径是 OnRecordComplete 拿到的路径
        JsonData videoTopics = new JsonData();
        videoTopics.SetJsonType(JsonType.Array);
        videoTopics.Add("Some Topic1");
        videoTopics.Add("Some Topic2");
        shareJson["extra"]["videoTopics"] = videoTopics;
        shareJson["extra"]["hashtag_list"] = videoTopics;

        Debug.Log($"ShareAppMessageBtnClicked jsonData: {videoTopics.ToJson()}");
        TT.ShareAppMessage(videoTopics, (data) =>
        {
            callBack?.Invoke(true);
            Debug.Log($"ShareAppMessage success: {videoTopics.ToJson()}");//
        },
            (errMsg) =>
            {
                callBack?.Invoke(false);
                Debug.Log($"ShareAppMessage failed: {errMsg}");
            },
            () =>
            {
                callBack?.Invoke(false);
                Debug.Log($"ShareAppMessage cancel");
            });
    }

    #endregion
}

#else

/// <summary>
/// 编辑器占位实现：Editor 下没有 TTSDK，保持与真机同名同接口，
/// 让场景编译通过，并在编辑器里跑通「点按钮 → 发奖励」链路（跳过真实广告播放）。
/// </summary>
public class TikTokPlatform : IAdPlatform
{
    public static bool EnableRemoteTracking = false;

    public TikTokPlatform(string appId, string videoAdId, string interAdId)
    {
        Debug.Log("[抖音广告] 占位模式：编辑器下跳过 TTSDK 初始化（真机 WebGL 才启用）");
    }

    public override void NavigateToScene(Action onSuccess, Action<string> onFail)
    {
        Debug.Log("[抖音广告] 占位模式：跳过侧边栏跳转");
        onSuccess?.Invoke();
    }

    public override void ShowRewardedVideoAd(string videoAdId, Action onSuccess, Action<string> onFail)
    {
        Debug.Log($"[抖音广告] 占位模式：跳过激励视频（广告位 {videoAdId}），直接发放奖励");
        onSuccess?.Invoke();
    }

    public override void ShowInterstitialAd(string AdId)
    {
        Debug.Log($"[抖音广告] 占位模式：跳过插屏广告（广告位 {AdId}）");
    }

    public override void ShareGame(Action<bool> callBack)
    {
        Debug.Log("[抖音广告] 占位模式：跳过分享");
        callBack?.Invoke(true);
    }
}

#endif