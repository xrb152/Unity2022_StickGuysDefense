using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
#endif

public class cebian : MonoBehaviour
{
    //前往按钮
    public Button btn_Go;


    private void Start()
    {
        //检查是否支持侧边栏功能
        //CheckSideBarSupport();
        btn_Go.onClick.AddListener(NavigateToSideBar);
    }

    /// <summary>
    /// 跳转到侧边栏
    /// </summary>
    public void NavigateToSideBar()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            var data = new JsonData
            {
                ["scene"] = "sidebar",
            };
            TT.NavigateToScene(data, () =>
            {
                Debug.Log("navigate to scene success");
            }, () =>
            {
                Debug.Log("navigate to scene complete");
            }, (errCode, errMsg) =>
            {
                Debug.Log($"navigate to scene error, errCode:{errCode}, errMsg:{errMsg}");
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError("跳转到侧边栏异常: " + e.Message);
        }
#else
        Debug.Log("[侧边栏] 占位模式：编辑器下跳过 TTSDK 侧边栏跳转");
#endif
    }
}
