  

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeDetails : MonoBehaviour
{
    public int Id;
    public float[] TimeResearch;
    public int[] Price;
    public int[] gemPrice;
    [HideInInspector] public int Level;
    [SerializeField] private GameObject[] activeObjects;
    [SerializeField] private TextMeshProUGUI priceDisplayText;
    [SerializeField] private TextMeshProUGUI gemPriceDisplayText;
    [SerializeField] private TextMeshProUGUI LevelDisplayText;
    [SerializeField] private TextMeshProUGUI timerDisplayText;
    [SerializeField] private Image researchProgressDisplay;
    [SerializeField] private GameObject ActiveElemeny;

    private AudioSource _clickAudios;
    private WindowsUI _windowsUI;
    private Wallet _wallet;
    private float timer;
    private float tempTimer;
    private int _isprocess;

    private void Awake()
    {
        _wallet = FindObjectOfType<Wallet>();
        _windowsUI = FindObjectOfType<WindowsUI>();
        Level = PlayerPrefs.GetInt("Level" + Id);
        ClampLevel();

        if (PlayerPrefs.GetInt("_isprocess" + Id) > 0)
        {
            timer = PlayerPrefs.GetFloat("timer" + Id);

            if (TimeLastSession.ts.TotalSeconds >= TimeResearch[Level])
            {
                UpdateLevel();
                _isprocess = 0;
                PlayerPrefs.SetInt("_isprocess" + Id, 0);
            }
            else
            {
                timer -= (float)(TimeLastSession.ts.TotalSeconds + PlayerPrefs.GetFloat("SessionTimer"));
                tempTimer = TimeResearch[Level] - timer;
            }
        }

        _isprocess = PlayerPrefs.GetInt("_isprocess" + Id);
        _clickAudios = FindObjectOfType<WindowsUI>().GetComponent<AudioSource>();

        CheckProcess(_isprocess);
        UpdateText();
    }

    private void Update()
    {
        if (_isprocess == 1)
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
                tempTimer += Time.deltaTime;
                researchProgressDisplay.fillAmount = tempTimer / TimeResearch[Mathf.Clamp(Level, 0, MaxLevel)];
                timerDisplayText.text = string.Format("{0:0}:{1:00}:{2:00}", (int)(timer / 3600), (int)(timer / 60 - (int)(timer / 3600) * 60), (int)((timer - (int)(timer / 60) * 60))); // мин. и секунды
            }
            else
            {
                _isprocess = 0;
                tempTimer = 0;
                CheckProcess(_isprocess);
                PlayerPrefs.SetInt("_isprocess" + Id, 0);
                UpdateLevel();
                UpdateText();
            }
        }
    }

    /// <summary>
    /// 最高等级索引。三张表的长度并不完全一致（例如 Id=4 的 TimeResearch 有 70 项、Price 只有 60 项），
    /// 所以取最短的那张，保证 Price / gemPrice / TimeResearch 都不会越界。
    /// </summary>
    private int MaxLevel
    {
        get
        {
            int max = 0;
            if (Price != null && Price.Length > 0) max = Price.Length - 1;
            if (gemPrice != null && gemPrice.Length > 0) max = Mathf.Min(max, gemPrice.Length - 1);
            if (TimeResearch != null && TimeResearch.Length > 0) max = Mathf.Min(max, TimeResearch.Length - 1);
            return Mathf.Max(0, max);
        }
    }

    /// <summary>是否已经满级。</summary>
    public bool IsMaxLevel => Level >= MaxLevel;

    /// <summary>当前等级的升级价格。卡片上显示的价格也走这里，保证「显示多少就扣多少」。</summary>
    public int GetCurrentPrice()
    {
        return 250 * (Level + 1);
    }

    /// <summary>把 Level 夹进合法区间，顺便自愈因为满级后继续点击而写坏的存档。</summary>
    private void ClampLevel()
    {
        int clamped = Mathf.Clamp(Level, 0, MaxLevel);
        if (clamped != Level)
        {
            Level = clamped;
            PlayerPrefs.SetInt("Level" + Id, Level);
        }
    }

    public void Upgrade()
    {
        if (IsMaxLevel)
            return;

        if (_wallet.GetMoney() >= GetCurrentPrice())
        {
            _wallet.SetMoney(-GetCurrentPrice());
            UseUpgrade();
        }
        else
        {
           // _windowsUI.GemShopOpen(0);
        }
    }

    public void UseUpgrade()
    {
        if (IsMaxLevel)
        {
            // 已满级：不再开研究进程，只把界面刷成「满级」
            UpdateText();
            return;
        }

        timer = TimeResearch[Mathf.Clamp(Level, 0, MaxLevel)];
        _isprocess = 1;
        CheckProcess(_isprocess);
        PlayerPrefs.SetInt("_isprocess" + Id, 1);
        SaveTimer();
        _clickAudios.Play();
    }

    public void AdsUpgradeProcess()
    {
        if (IsMaxLevel)
            return;

        timer -= 18000;
        tempTimer += 18000;
        SaveTimer();
        _clickAudios.Play();
    }

    public void GemUpgradeProcess()
    {
        if (IsMaxLevel)
            return;

        if (_wallet.GetGems() >= gemPrice[Mathf.Clamp(Level, 0, MaxLevel)])
        {
            _wallet.SetGems(-gemPrice[Level]);
            timer = .1f;
        }
        else
        {
           //_windowsUI.GemShopOpen(0);
        }
        _clickAudios.Play();
    }

    private void UpdateText()
    {
        if (LevelDisplayText != null)
            LevelDisplayText.text = (Level + 1).ToString();

        int lv = Mathf.Clamp(Level, 0, MaxLevel);

        priceDisplayText.text = IsMaxLevel ? "满级" : Price[lv].ToString();
        gemPriceDisplayText.text = IsMaxLevel ? "满级" : gemPrice[lv].ToString();
    }

    private void UpdateLevel()
    {
        Level = Mathf.Min(Level + 1, MaxLevel);
        PlayerPrefs.SetInt("Level" + Id, Level);

        if (ActiveElemeny.GetComponent<EarnBuild>())
            ActiveElemeny.GetComponent<EarnBuild>().Upgrade();

        if (ActiveElemeny.GetComponent<Rampage>())
            ActiveElemeny.GetComponent<Rampage>().Upgrade();

        if (ActiveElemeny.GetComponent<IncomeMoney>())
            ActiveElemeny.GetComponent<IncomeMoney>().Upgrade();

        if (ActiveElemeny.GetComponent<BoostDamage>())
            ActiveElemeny.GetComponent<BoostDamage>().Upgrade();

        if (ActiveElemeny.GetComponent<TapDamage>())
            ActiveElemeny.GetComponent<TapDamage>().Upgrade();

        if (ActiveElemeny.GetComponent<UpgradeCatapults>())
            ActiveElemeny.GetComponent<UpgradeCatapults>().Upgrade();
    }

    private void CheckProcess(int cheker)
    {
        if (cheker > 0)
        {
            activeObjects[0].SetActive(false);
            activeObjects[1].SetActive(true);
        }
        else
        {
            activeObjects[0].SetActive(true);
            activeObjects[1].SetActive(false);
        }
    }


    public void SaveTimer()
    {
        PlayerPrefs.SetFloat("timer" + Id, timer);
    }
}
