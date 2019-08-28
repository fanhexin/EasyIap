using System;
using System.Collections.Generic;
using System.Threading;
using UniRx.Async;
using UnityEngine.Purchasing;

namespace EasyIap
{
    public interface IIap
    {
//        event Action<string> onApprovedDeferPurchase;
        bool isReady { get; }
        IReadOnlyList<Product> products { get; }
        UniTask<string> InitAsync(params ProductDefine[] productDefines);
        UniTask<string> BuyAsync(string id, CancellationToken cancellationToken = default);
        UniTask<bool> RestoreAsync(CancellationToken cancellationToken = default);
        Product GetProduct(string id);
        
        /// <summary>
        /// 判断id对应商品是否已有对应收据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool HasReceipt(string id);

        /// <summary>
        /// 获取那些扣了钱但没有给与相应奖励的product id
        /// </summary>
        /// <returns></returns>
        UniTask<IReadOnlyCollection<string>> GetPendingPurchaseAsync(CancellationToken cancellationToken = default);
    }
}