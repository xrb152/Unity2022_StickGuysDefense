  

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WarriorsCoreUpgrade : MonoBehaviour
{
    public int id;

    [Header("INFORMATION UI")]
    //[SerializeField] private Image borderColor;
    [SerializeField] private GameObject adsBlockImage, priceWrapper;
    [SerializeField] private TextMeshProUGUI[] priceDisplayText;
    [SerializeField] private TextMeshProUGUI levelDisplayText, levelColorText;
    [SerializeField] private Image researchProgressDisplay;
    [SerializeField] private Sprite[] _avatarCycleImages;
    [SerializeField] private ButtonInfo _buttonInfo;

    [SerializeField] private GameObject[] progressObjects;
    [HideInInspector] public int currentLevel;
    [SerializeField] private int counter = 1;
    [SerializeField] private int counterWarriorEnable = 1;
    private int cycleCounter;
    [Header("DATA")]
    [SerializeField] private warriorLevel[] _warriorLevel;
    //public W_UpgradesDetail[] details;
    [SerializeField] private WarriorAttack_1[] _warriors;
    [SerializeField] private Sprite[] buttonSprite;
    [SerializeField] private Image button;
    private float _timer;
    private int _isprocess;
    private float tempTimer;
    private Wallet _wallet;
    private int _warriorLenght;
    private WindowsUI _windowsUI;
    [SerializeField] private AudioSource _clickAudios;
    private bool isActive;
    private AddWarriors _addWarriors;

    private int GetCurrentPrice()
    {
        return 100 * (currentLevel + 1);
    }

    /// <summary>最高等级索引（_warriorLevel 的最后一格）。</summary>
    private int MaxLevel => (_warriorLevel != null && _warriorLevel.Length > 0) ? _warriorLevel.Length - 1 : 0;

    /// <summary>是否已经满级。</summary>
    public bool IsMaxLevel => currentLevel >= MaxLevel;
    private void Awake()
    {
        currentLevel = PlayerPrefs.GetInt("currentLevel" + id);
        cycleCounter = PlayerPrefs.GetInt("cycleCounter" + id);
    }

    public void Start()
    {
        // 满级后继续点击会把存档写成越界值，这里先夹紧，顺便自愈已经写坏的旧存档
        if (currentLevel < 0)
            currentLevel = 0;

        if (currentLevel > MaxLevel)
        {
            currentLevel = MaxLevel;
            PlayerPrefs.SetInt("currentLevel" + id, currentLevel);
        }

        _warriorLenght = _warriors.Length;
        _wallet = FindObjectOfType<Wallet>();
        _windowsUI = FindObjectOfType<WindowsUI>();
        _addWarriors = FindObjectOfType<AddWarriors>();

        if (PlayerPrefs.HasKey("counter" + id))
            counter = PlayerPrefs.GetInt("counter" + id);

        if (PlayerPrefs.HasKey("counterWarriorEnable" + id))
            counterWarriorEnable = PlayerPrefs.GetInt("counterWarriorEnable" + id);

        _isprocess = PlayerPrefs.GetInt("_isprocessWarrior" + id);

        CheckAdsImage();

        _wallet.MoneyChange.AddListener((level) =>
        {
            CheckAdsImage();
        });

        CheckWarriors(1);
        CheckProcess(_isprocess);
        UpdateText();

    }

    private void UpdateButtonInfo()
    {
        int price = GetCurrentPrice();
        bool isMax = IsMaxLevel;

        if (_buttonInfo != null)
        {
            _buttonInfo.price = price;
            _buttonInfo.isMaxLevel = isMax;
            _buttonInfo.CheckStatus();
        }

        // _buttonInfo 可能指向另一行（隐藏行）的同款卡片，
        // 这里再按 adsBlockImage 找到本卡片上的 ButtonInfo，把真实价格同步过去。
        if (adsBlockImage != null)
            ButtonInfo.SyncPrice(adsBlockImage.transform, price, isMax);
    }


    private void UpdateLevel()
    {
        currentLevel = Mathf.Min(currentLevel + 1, MaxLevel);
        PlayerPrefs.SetInt("currentLevel" + id, currentLevel);

        for (int i = 0; i < _warriors.Length; i++)
        {
            if (_warriors[i].groupId == counter)
                _warriors[i].UpgradeWarriors();

            CheckWarriors(1);
        }

        counter++;

        if (counter > 5)
        {
            cycleCounter++;
            PlayerPrefs.SetInt("cycleCounter" + id, cycleCounter);
            counter = 0;
        }

        PlayerPrefs.SetInt("counter" + id, counter);
    }


    //public void AddUnits()
    //{
    //    if (counterWarriorEnable < 6)
    //    {
    //        for (int i = 0; i < _warriors.Length; i++)
    //        {
    //            if (_warriors[i].groupId == counterWarriorEnable)
    //                _warriors[i].AddUnit();

    //            CheckWarriors(1);
    //        }

    //        counterWarriorEnable++;
    //        PlayerPrefs.SetInt("counterWarriorEnable" + id, counterWarriorEnable);
    //    }
    //}

    public void CheckWarriors(int type)
    {
        int level = Mathf.Clamp(currentLevel, 0, MaxLevel);

        for (int i = 0; i < _warriorLenght; i++)
        {
            if (_warriors[i].warriorEnable > 0)
                _warriors[i].CheckStats(_warriorLevel[level].Health, _warriorLevel[level].Attack, _warriorLevel[level].AttackTimer, type);
        }
    }
    public void HealSingleWarrior(bool check)
    {
        int level = Mathf.Clamp(currentLevel, 0, MaxLevel);

        for (int i = 0; i < _warriorLenght; i++)
        {
            if (check)
            {
                if (_warriors[i].warriorLevel > 0 && _warriors[i].isdead)
                {
                    _warriors[i].CheckStats(_warriorLevel[level].Health, _warriorLevel[level].Attack, _warriorLevel[level].AttackTimer, 1);
                    check = false;
                }
            }
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
            CheckAdsImage();
        }
        else
        {
            _windowsUI.GemShopOpen(0);
        }
    }

    public void UseUpgrade()
    {
        if (IsMaxLevel)
        {
            // 已满级：不再升级、不再加兵，只把界面刷成「满级」
            UpdateText();
            CheckAdsImage();
            return;
        }

        _addWarriors.AddUnit(id);
        UpdateLevel();
        UpdateText();
        _clickAudios.Play();
    }

    private void CheckProcess(int cheker)
    {
        if (cheker > 0)
        {
            progressObjects[0].SetActive(false);
            progressObjects[1].SetActive(true);
        }
        else
        {
            progressObjects[0].SetActive(true);
            progressObjects[1].SetActive(false);
        }
    }

    public void UpdateText()
    {
        string priceText = IsMaxLevel ? "满级" : GetCurrentPrice().ToString();

        priceDisplayText[0].text = priceText;
        priceDisplayText[1].text = priceText;
        levelDisplayText.text = (currentLevel + 1).ToString() + "级";

        UpdateButtonInfo();
    }

    public void UseRampage(float cycle)
    {
        //_audiosKlich.Play();

        for (int i = 0; i < _warriorLenght; i++)
        {
            if (_warriors[i].warriorLevel > 0)
                _warriors[i].Rampage(cycle);
        }

        _clickAudios.Play();
    }

    public void StopRampage()
    {
        CheckWarriors(0);
    }

    private void CheckAdsImage()
    {
        if (IsMaxLevel || _wallet.GetMoney() >= GetCurrentPrice())
        {
            adsBlockImage.SetActive(false);
            priceWrapper.SetActive(true);
            button.sprite = buttonSprite[0];
        }
        else
        {
            adsBlockImage.SetActive(true);
            priceWrapper.SetActive(false);
            button.sprite = buttonSprite[1];
        }
    }
}
