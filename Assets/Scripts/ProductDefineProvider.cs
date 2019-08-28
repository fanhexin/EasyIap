using EasyIap;
using UnityEngine;

namespace DataSheet
{
    public abstract class ProductDefineProvider : ScriptableObject
    {
        public abstract ProductDefine[] productDefines { get; }
    }
}