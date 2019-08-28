using Newtonsoft.Json;

namespace DataSheet
{
    public partial class Iap    
    {
        readonly public int coin; 
        readonly public int discount; 
        readonly public int hint; 
        readonly public bool if_no_ads; 
        readonly public bool if_one_time; 
        readonly public string ios; 
        readonly public float price; 
        readonly public int rank; 
        readonly public int touch_hint; 

        [JsonConstructor]
        public Iap(
            int coin,
            int discount,
            int hint,
            bool if_no_ads,
            bool if_one_time,
            string ios,
            float price,
            int rank,
            int touch_hint
        )
        {
            this.coin = coin;
            this.discount = discount;
            this.hint = hint;
            this.if_no_ads = if_no_ads;
            this.if_one_time = if_one_time;
            this.ios = ios;
            this.price = price;
            this.rank = rank;
            this.touch_hint = touch_hint;
        }
    }
}