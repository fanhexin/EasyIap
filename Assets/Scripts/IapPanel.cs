using System.Collections.Generic;
using DataSheet;
using EasyIap;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class IapPanel : MonoBehaviour
{
    [SerializeField] ProductDefineProvider _productDefineProvider;
    [SerializeField] Text _title;
    [SerializeField] Transform _content;
    [SerializeField] Transform _productViewTmpl;
    [SerializeField] Button _restoreBtn;
    
    IIap _iap;

    async void Start()
    {
        _title.text = "Init....";
        _restoreBtn.onClick.AddListener(OnRestoreBtnClick);
        
        _iap = new InAppPurchasing();
        _iap.onPendingPurchase += OnPendingPurchase;
        
        string initResult = await _iap.InitAsync(_productDefineProvider.productDefines);
        _title.text = string.IsNullOrEmpty(initResult) ? "Init success!" : initResult;
        
        if (!string.IsNullOrEmpty(initResult))
        {
            return;
        }

        _restoreBtn.interactable = true;
        InitProducts(_iap.products);
    }

    void OnPendingPurchase(string id)
    {
        _title.text = $"Process pending product {id}!";
    }

    async void OnRestoreBtnClick()
    {
        bool result = await _iap.RestoreAsync();
        _title.text = $"Restore result: {result}!";
        RefreshProduct();
    }

    void InitProducts(IReadOnlyList<Product> iapProducts)
    {
        foreach (Product product in iapProducts)
        {
            var view = Instantiate(_productViewTmpl, _content);
            view.gameObject.SetActive(true);
            string desc = $"id:{product.definition.id}, type:{product.definition.type}, hasReceipt:{product.hasReceipt}, price:{product.metadata.localizedPriceString}";
            view.Find("Desc").GetComponent<Text>().text = desc;
            
            view.Find("BuyBtn")
                .GetComponent<Button>()
                .onClick
                .AddListener(async () =>
                {
                    string result = await _iap.BuyAsync(product.definition.id);
                    if (string.IsNullOrEmpty(result))
                    {
                        _title.text = $"Buy product {product.definition.id} success!";
                        if (product.definition.type == ProductType.NonConsumable)
                        {
                            Destroy(view.gameObject);
                        }
                    }
                    else
                    {
                        _title.text = $"Buy product {product.definition.id} result: {result}!";
                    }
                });
        }
    }

    void RefreshProduct()
    {
        for (int i = _content.childCount - 1; i >= 1; i--)
        {
            var go = _content.GetChild(i).gameObject;
            Destroy(go);
        }
        
        InitProducts(_iap.products);
    }
}
