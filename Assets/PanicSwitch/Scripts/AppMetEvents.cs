  

using UnityEngine;
using System.Collections.Generic;

public class AppMetEvents : MonoBehaviour
{
    private Dictionary<string, object> eventParameters = new Dictionary<string, object>();

    public void LevelStartEvent(string type, int _level, int _gameCount)
    {
        eventParameters["level_type"] = type;
        eventParameters["level_number"] = _level.ToString();
        eventParameters["level_count"] = _gameCount.ToString();
        eventParameters["level_random"] = "0";
        AppMetrica.Instance.ReportEvent("level_start", eventParameters);
        eventParameters.Clear();
    }

    public void LevelFinishEvent(int _level, string _result, int _gameCount,float _gametimer)
    {
        eventParameters["max_wave"] = _level.ToString();
        eventParameters["result"] = _result;
        eventParameters["level_count"] = _gameCount.ToString();
        eventParameters["level_random"] = "0";
        eventParameters["level_time"] = _gametimer.ToString("f0");
        eventParameters["continue"] = QuestManager.ContinueCounter.ToString();
        AppMetrica.Instance.ReportEvent("level_finish", eventParameters);
        eventParameters.Clear();
    }

    public void VideoAdsAvailable(string adType, string _placement, int connection)
    {
        eventParameters["ad_type"] = adType;
        eventParameters["placement"] = _placement;
        eventParameters["result"] = "success";
        eventParameters["connection"] = connection;
        AppMetrica.Instance.ReportEvent("video_ads_available", eventParameters);
        eventParameters.Clear();
    }

    public void VideoAdsStartded(string adType, string _placement, int connection)
    {
        eventParameters["ad_type"] = adType;
        eventParameters["placement"] = _placement;
        eventParameters["result"] = "start";
        eventParameters["connection"] = connection;
        AppMetrica.Instance.ReportEvent("video_ads_started", eventParameters);
        eventParameters.Clear();
    }


    public void VideoAdsWatch(string adType, string _result, string _placement, int connection)
    {
        eventParameters["ad_type"] = adType;
        eventParameters["placement"] = _placement;
        eventParameters["result"] = _result;
        eventParameters["connection"] = connection;
        AppMetrica.Instance.ReportEvent("video_ads_watch", eventParameters);
        eventParameters.Clear();
    }

    // IAP（内购）相关上报方法已移除：原 IAP() / IAP_Gold() 仅由内购逻辑调用，
    // 在 IAP 功能整体移除后不再需要。

}


