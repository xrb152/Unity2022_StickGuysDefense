  

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonInfo : MonoBehaviour
{
    public int price;
    /// <summary>满级：隐藏广告层，价格位置显示「满级」，不再提供任何升级入口。</summary>
    [HideInInspector] public bool isMaxLevel;
    [SerializeField] private GameObject _blockImage;
   // [SerializeField] private GameObject _priceWrapper;
    [SerializeField] private TextMeshProUGUI _priceDisplayText;
    [SerializeField] private Wallet _wallet;
    [HideInInspector] public spellButton _spellButton;
        

    public void AdsButtonChecker(bool value)
    {
        _blockImage.GetComponent<Button>().enabled = value;
    }

    /// <summary>
    /// 把这张卡片上 ButtonInfo 的价格同步成卡片真实价格。
    /// anchor 传卡片内部任意子物体（比如 adsBlockImage），会向上找到卡片根上的 ButtonInfo。
    /// 不同步的话，ButtonInfo 会用预制体里的旧价格判断“买不买得起”，
    /// 结果是有钱时点卡片走不到金币逻辑、没钱时又不弹广告层。
    /// </summary>
    public static void SyncPrice(Component anchor, int newPrice, bool isMaxLevel = false)
    {
        if (anchor == null)
            return;

        ButtonInfo info = anchor.GetComponentInParent<ButtonInfo>();
        if (info == null)
            return;

        info.price = newPrice;
        info.isMaxLevel = isMaxLevel;
        info.CheckStatus();
    }

    private void Start()
    {
        _wallet = FindObjectOfType<Wallet>();

        if (_priceDisplayText)
            _priceDisplayText.text = price.ToString();

        _wallet.MoneyChange.AddListener((level) =>
        {
            CheckStatus();
        });
    }

    // 卡片价格通常由同一张卡上的 WarriorsCoreUpgrade / IncomeMoney 在各自 Start 里写入，
    // 两个 Start 的先后顺序不确定，所以第一次判断推迟到下一帧，避免用预制体里的旧价格误判。
    private bool _firstCheckDone;

    private void Update()
    {
        if (_firstCheckDone)
            return;

        _firstCheckDone = true;
        CheckStatus();
    }

    public void CheckStatus()
    {
        if (isMaxLevel)
        {
            // 满级卡：藏掉广告层，价格位置显示「满级」
            if (_blockImage != null)
                _blockImage.SetActive(false);

            ChangeText(2);
            return;
        }

        if (_spellButton)
        {
            if (_spellButton.CheckQty() > 0)
            {
                ChangeText(1);
            }
            else
            {
                CheckInner();
            }
        }
        else
        {
            CheckInner();
        }
    }

    private void CheckInner()
    {
        if (_wallet == null)
            _wallet = FindObjectOfType<Wallet>();

        if (_wallet == null)
            return;

        if (_wallet.GetMoney() >= price)
        {
            _blockImage.SetActive(false);

       //     if (_priceWrapper)
       //         _priceWrapper.SetActive(true);
        }
        else
        {
            _blockImage.SetActive(true);
       //     if (_priceWrapper)
         //       _priceWrapper.SetActive(false);
        }

        ChangeText(0);
    }

    public void ChangeText(int value)
    {
        if (!_priceDisplayText) return;

        if (value == 2)
            _priceDisplayText.text = "满级";
        else if (value > 0)
            _priceDisplayText.text = "免费";
        else
            _priceDisplayText.text = price.ToString();
    }
}
