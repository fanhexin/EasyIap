using UnityEngine.Purchasing;

namespace EasyIap
{
    public struct ProductDefine 
    {
        public readonly string id;
        public readonly ProductType productType;

        public ProductDefine(string id, ProductType productType)
        {
            this.id = id;
            this.productType = productType;
        }
    }
}