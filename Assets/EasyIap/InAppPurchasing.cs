using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Purchasing;

namespace EasyIap
{
    public class InAppPurchasing : IStoreListener, IIap
    {
        const string PENDING_PURCHASE_KEY = "pending_purchase";
        
        IStoreController _storeController;
        IExtensionProvider _extensionProvider;

        UniTaskCompletionSource<string> _initTcs;

        readonly Lazy<Dictionary<string, UniTaskCompletionSource<string>>> _buyTasks =
            new Lazy<Dictionary<string, UniTaskCompletionSource<string>>>(() =>
                new Dictionary<string, UniTaskCompletionSource<string>>());

        public event Action<string> onPendingPurchase;
        public bool isReady => _storeController != null && _extensionProvider != null;
        public IReadOnlyList<Product> products
        {
            get
            {
                CheckReady();
                return _storeController.products.all;
            }
        }

        public UniTask<string> InitAsync(params ProductDefine[] productDefines)
        {
            if (productDefines == null || productDefines.Length == 0)
            {
                throw new Exception("Please init with products!");
            }
            
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
//            Debug.Log($"can make payments: {builder.Configure<IAppleConfiguration>().canMakePayments}-----");
            
            foreach (ProductDefine define in productDefines)
            {
                builder.AddProduct(define.id, define.productType);
            }
            
            _initTcs = new UniTaskCompletionSource<string>();
            UnityPurchasing.Initialize(this, builder);
            return _initTcs.Task;
        }

        public async UniTask<string> BuyAsync(string id, CancellationToken cancellationToken = default)
        {
            CheckReady();

            Product product = _storeController.products.WithID(id);
            if (product == null || !product.availableToPurchase)
            {
                return $"Product {id} not available!";
            }
            
            var buyTcs = new UniTaskCompletionSource<string>();
            cancellationToken.Register(() => buyTcs.TrySetCanceled());
            _buyTasks.Value[id] = buyTcs;
            _storeController.InitiatePurchase(product);
            
            PlayerPrefs.SetString(PENDING_PURCHASE_KEY, id);
            PlayerPrefs.Save();
            return await buyTcs.Task;
        }

        public async UniTask<bool> RestoreAsync(CancellationToken cancellationToken = default)
        {
#if UNITY_IOS
            CheckReady();
            var restoreTcs = new UniTaskCompletionSource<bool>();
            cancellationToken.Register(() => restoreTcs.TrySetCanceled());
            var apple = _extensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions(ret => restoreTcs.TrySetResult(ret));
            return await restoreTcs.Task;
#else
            return true;            
#endif
        }

        public Product GetProduct(string id)
        {
            CheckReady();
            return _storeController.products.WithID(id);
        }

        public bool HasReceipt(string id)
        {
            CheckReady();
            return products.Any(p => p.definition.id == id && p.hasReceipt);
        }

        void CheckReady()
        {
            if (isReady)
            {
                return;
            }
            
            throw new Exception("Iap not ready!");
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error)
        {
            _initTcs?.TrySetResult(error.ToString());
            _initTcs = null;
        }

        void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _extensionProvider = extensions;

            _initTcs?.TrySetResult(null);
            _initTcs = null;
        }

        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs e)
        {
            string id = e.purchasedProduct.definition.id;
//            Debug.Log($"ProcessPurchase id {id}-----");
            
            if (!ProcessPendingPurchase(id))
            {
                return PurchaseProcessingResult.Pending;
            }
            return PurchaseResponse(id)?PurchaseProcessingResult.Complete:PurchaseProcessingResult.Pending;
        }

        bool ProcessPendingPurchase(string id)
        {
            string pendingId = PlayerPrefs.GetString(PENDING_PURCHASE_KEY, string.Empty);
            if (pendingId != id)
            {
                return true;
            }

            // 如果不注册回调对pending purchase进行处理，则让purchase继续处于pending状态
            if (onPendingPurchase == null)
            {
                return false;
            }

            onPendingPurchase(id);

            PlayerPrefs.DeleteKey(PENDING_PURCHASE_KEY);
            PlayerPrefs.Save();
            return true;
        }

        void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
//            Debug.Log($"OnPurchaseFailed id {product.definition.id}-----");
            PurchaseResponse(product.definition.id, failureReason.ToString());
        }
        
        bool PurchaseResponse(string id, string errorCode = null)
        {
            if (!_buyTasks.Value.TryGetValue(id, out UniTaskCompletionSource<string> tcs)) return true;
            
            bool ret = tcs.TrySetResult(errorCode);
            if (!ret)
            {
                return ret;
            }
            
            _buyTasks.Value.Remove(id);
            
            string pendingId = PlayerPrefs.GetString(PENDING_PURCHASE_KEY, string.Empty);
            if (pendingId == id)
            {
                PlayerPrefs.DeleteKey(PENDING_PURCHASE_KEY);
                PlayerPrefs.Save();
            }
            return ret;
        }
    }
}