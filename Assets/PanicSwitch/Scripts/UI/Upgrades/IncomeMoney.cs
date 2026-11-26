  

using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class IncomeMoney : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _moneyForKillDisplayText;
    [SerializeField] private TextMeshProUGUI[] priceDisplayText;
    [SerializeField] private GameObject adsBlockImage, priceWrapper;
    [SerializeField] private Sprite[] buttonSprite;
    [SerializeField] private Image button; 
    public static float MoneyForKill;
    private Wallet _wallet;
    private int _level;
    /// <summary>同一张卡上的升级数据：真实价目表、当前等级、满级判定都在它身上。</summary>
    private UpgradeDetails _details;

    private void Awake()
    {
        if (PlayerPrefs.HasKey("MoneyForKill"))
        {
            MoneyForKill = PlayerPrefs.GetFloat("MoneyForKill");
        }
        else
        {
            MoneyForKill = .4f;
        }
        _level = PlayerPrefs.GetInt("level_goldPerKill");
        _wallet = FindObjectOfType<Wallet>();
        _details = GetComponent<UpgradeDetails>();


        CheckAdsImage();

        if (_wallet != null)
        {
            _wallet.MoneyChange.AddListener((level) =>
            {
                CheckAdsImage();
            });
        }

        UpdateText();
    }

    // UpgradeDetails 的 Level 是在它自己的 Awake 里读存档的，两个 Awake 的先后顺序不确定，
    // 所以价格和满级状态在 Start 里再刷一次（此时所有 Awake 都已经执行完）。
    private void Start()
    {
        UpdateText();
    }

    /// <summary>这张卡是否已经满级，上限由 UpgradeDetails 的价目表长度决定。</summary>
    private bool IsMaxLevel
    {
        get { return _details != null && _details.IsMaxLevel; }
    }

    public void Upgrade()
    {
        MoneyForKill += .3f;
        PlayerPrefs.SetFloat("MoneyForKill", MoneyForKill);
        _level++;
        PlayerPrefs.SetInt("level_goldPerKill", _level);
        UpdateText();
    }

    private void UpdateText()
    {
        int price = GetCurrentPrice();
        bool max = IsMaxLevel;

        _moneyForKillDisplayText.text = MoneyForKill.ToString("f1") + "金币/击杀";

        // 满级后价格位置显示「满级」，跟剑 / 弓两张卡保持一致
        string priceText = max ? "满级" : price.ToString();
        if (priceDisplayText.Length > 0)
            priceDisplayText[0].text = priceText;
        if (priceDisplayText.Length > 1)
            priceDisplayText[1].text = priceText;

        CheckAdsImage();

        // 同步卡片上的 ButtonInfo，让“有钱花金币 / 没钱看广告”跟卡面价格用同一个阈值；
        // 满级时把 isMaxLevel 一起传过去，由它收掉广告层、价格位改成「满级」。
        if (adsBlockImage != null)
            ButtonInfo.SyncPrice(adsBlockImage.transform, price, max);
    }

    private void CheckAdsImage()
    {
        if (IsMaxLevel)
        {
            // 满级：没有可卖的东西了，别再显示「看广告免费升级」的遮罩
            adsBlockImage.SetActive(false);
            priceWrapper.SetActive(true);
            if (button != null && buttonSprite.Length > 0)
                button.sprite = buttonSprite[0];
            return;
        }

        if (_wallet == null)
            _wallet = FindObjectOfType<Wallet>();

        if (_wallet != null && _wallet.GetMoney() >= GetCurrentPrice())
        {
            adsBlockImage.SetActive(false);
            priceWrapper.SetActive(true);
            if (button != null && buttonSprite.Length > 0)
                button.sprite = buttonSprite[0];
        }
        else
        {
            adsBlockImage.SetActive(true);
            priceWrapper.SetActive(false);
            if (button != null && buttonSprite.Length > 1)
                button.sprite = buttonSprite[1];
        }
    }

    private int GetCurrentPrice()
    {
        // 有 UpgradeDetails 时直接用它那套价格：卡片本身没有独立的价目表，
        // 显示的价格必须是点下去真正会扣的钱，两边用同一个公式才不会对不上。
        if (_details != null)
            return _details.GetCurrentPrice();

        return 250 * (_level + 1);
    }
}
