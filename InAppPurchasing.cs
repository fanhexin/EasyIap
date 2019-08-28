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
//        const string DEFERRED_PURCHASE_KEY = "deferred_purchase";
        
//        public event Action<string> onApprovedDeferPurchase;
        
        IStoreController _storeController;
        IExtensionProvider _extensionProvider;

        UniTaskCompletionSource<string> _initTcs;
        UniTaskCompletionSource<IReadOnlyCollection<string>> _pendingPurchaseTcs;

        readonly Lazy<Dictionary<string, UniTaskCompletionSource<string>>> _buyTasks =
            new Lazy<Dictionary<string, UniTaskCompletionSource<string>>>(() =>
                new Dictionary<string, UniTaskCompletionSource<string>>());

        string[] _pendingIds;
        List<string> _processPendingIds;

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
            UpdatePendingPurchase();
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

        public async UniTask<IReadOnlyCollection<string>> GetPendingPurchaseAsync(CancellationToken cancellationToken = default)
        {
            string value = PlayerPrefs.GetString(PENDING_PURCHASE_KEY);
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            
            _pendingIds = value.Split(',');
            if (_pendingIds == null || _pendingIds.Length == 0)
            {
                return null;
            }
            
            _pendingPurchaseTcs = new UniTaskCompletionSource<IReadOnlyCollection<string>>();
            cancellationToken.Register(() => _pendingPurchaseTcs.TrySetCanceled());
            return await _pendingPurchaseTcs.Task;
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
            
//#if UNITY_IOS
//            var apple = extensions.GetExtension<IAppleExtensions>();
//            apple.simulateAskToBuy = true;
//            apple.RegisterPurchaseDeferredListener(OnDeferred);
//#endif
            
            _initTcs?.TrySetResult(null);
            _initTcs = null;
        }

//        void OnDeferred(Product product)
//        {
//            Debug.Log($"{nameof(OnDeferred)} {product.definition.id}-----");
//            
//            string id = product.definition.id;
//            if (!_buyTasks.Value.TryGetValue(id, out var tcs))
//            {
//                return;
//            }
//
//            tcs.TrySetResult("Deferred!");
//            _buyTasks.Value.Remove(product.definition.id);
//            UpdatePendingPurchase();
//            
//            PlayerPrefs.SetString(DEFERRED_PURCHASE_KEY, id);
//            PlayerPrefs.Save();
//        }

        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs e)
        {
            string id = e.purchasedProduct.definition.id;
//            Debug.Log($"ProcessPurchase id {id}-----");
//            if (ProcessDeferredPurchase(id))
//            {
//                return PurchaseProcessingResult.Complete;
//            }
            
            if (!ProcessPendingPurchase(id))
            {
                return PurchaseProcessingResult.Pending;
            }
            return PurchaseResponse(id)?PurchaseProcessingResult.Complete:PurchaseProcessingResult.Pending;
        }

//        bool ProcessDeferredPurchase(string id)
//        {
//            string curDeferredId = PlayerPrefs.GetString(DEFERRED_PURCHASE_KEY, string.Empty);
//            if (string.IsNullOrEmpty(curDeferredId) || curDeferredId == id)
//            {
//                return false;
//            }
//            
//            onApprovedDeferPurchase?.Invoke(id);
//            
//            PlayerPrefs.DeleteKey(DEFERRED_PURCHASE_KEY);
//            PlayerPrefs.Save();
//            return true;
//        }

        bool ProcessPendingPurchase(string id)
        {
            if (_pendingPurchaseTcs == null) return true;
            
            if (_processPendingIds == null)
            {
                _processPendingIds = new List<string>();
            }

//            Debug.Log($"{nameof(ProcessPendingPurchase)} add id {id} -----");
            
            _processPendingIds.Add(id);

            if (_processPendingIds.Count != _pendingIds.Length ||
                _processPendingIds.Intersect(_pendingIds).Count() != _pendingIds.Length) return true;
            
//            Debug.Log($"{nameof(ProcessPendingPurchase)} try set result-----");
            
            bool ret = _pendingPurchaseTcs.TrySetResult(_pendingIds);
            _pendingPurchaseTcs = null;
            if (!ret)
            {
                return ret;
            }

            _processPendingIds.Clear();
            _processPendingIds = null;
            
            _pendingIds = null;

            PlayerPrefs.DeleteKey(PENDING_PURCHASE_KEY);
            PlayerPrefs.Save();
            return ret;
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
            UpdatePendingPurchase();
            return ret;
        }

        void UpdatePendingPurchase()
        {
            string ids = string.Join(",", _buyTasks.Value.Keys);
            PlayerPrefs.SetString(PENDING_PURCHASE_KEY, ids);    
            PlayerPrefs.Save();
        }
    }
}