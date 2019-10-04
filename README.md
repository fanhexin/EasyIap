# EasyIap

## 安装

首先确保`PackageManager`中已安装`InAppPurchasing` 最低`2.0.6`版本的包，
然后在`Unity Service`窗口中开启`Analytics`和`In-App Purchasing`两个服务。
在开启`In-App Purchasing`服务时，需要点界面上的`Import`按钮，之后会自动导入几个`UnityPackage`
（*但在2018.4.4版本中，点`Import`按钮会报错，需要手动再导入`Plugin`目录下导入进来的几个`UnityPackage`*）。

最后通过修改`manifest.json`，在其中加入如下条目安装：

```json
{
    "com.github.unitask": "https://github.com/fanhexin/UniTask.git#upm",
    "com.github.fanhexin.easyiap": "https://github.com/fanhexin/EasyIap.git#upm"
}
```

## 使用

只需创建`InAppPurchasing`类实例，即可开始使用内购功能，流程见如下代码：

```cs
var iap = new InAppPurchasing();

// 需要在Init之前注册
iap.onPendingPurchase += id => 
{
    // 未完成内购处理代码
};

// 在使用其他方法前需要先初始化
// ProductDefine 存储id和ProductType，组成数组初始化多个内购商品
// 初始化成功 result 为 null，否则为具体错误原因
string result = await iap.InitAsync(new ProductDefine[n] {...});

// 购买成功 result 为 null，否则为具体错误原因
string result = await iap.BuyAsync("product id");

// 恢复内购成功 result 为 true 否则为 false
bool result = await iap.RestoreAsync();

// 通过id查询具体的product
Product product = iap.GetProduct("product id");

// 根据id判断是否有收据，常用来查询去广告等NonComsumable类型内购是否已购买
bool b = iap.HasReceipt("product id");

// Init成功后该property返回true
bool iapReady = iap.isReady;

// products字段能取到所有InAppPurchasing内部的Product
foreach (var p in iap.products)
{
    // do something
}
```

`onPendingPurchase` 事件在发生未完成的内购时(如点击购买并确认后应用崩溃或者玩家手动杀死应用)，
重新启动应用并且`InAppPurchasing`初始化完成后触发。
参数为未完成的内购id，需要根据id给玩家补充漏掉的内购内容。

`BuyAsync`和`RestoreAsync`两方法均可传入`CancellationToken`从外部对操作进行取消，取消后对应的`purchase`会处于
`Pending`状态，再次启动应用会走`onPendingPurchase`流程。
