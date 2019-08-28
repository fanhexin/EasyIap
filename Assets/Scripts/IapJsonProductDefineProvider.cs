using System.Collections.Generic;
using System.Linq;
using EasyIap;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DataSheet
{
    [CreateAssetMenu(fileName = "IapJsonProductDefineProvider")]
    public class IapJsonProductDefineProvider : ProductDefineProvider
    {
        [SerializeField] TextAsset _iapJson;

        ProductDefine[] _productDefines;
        
        public override ProductDefine[] productDefines
        {
            get
            {
                if (_productDefines == null)
                {
                    var iaps = JsonConvert.DeserializeObject<Dictionary<string, Iap>>(_iapJson.text);
                    _productDefines = iaps.Select(x => new ProductDefine($"{Application.identifier}.{x.Value.ios}",
                        x.Value.if_one_time ? ProductType.NonConsumable : ProductType.Consumable))
                        .ToArray();
                }

                return _productDefines;
            }
        }
    }
}