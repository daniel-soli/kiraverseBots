namespace KiraVerse.Bots.Models.IMX;

public class ImxSellOrderDto
{
    public int order_id { get; set; }
    public string status { get; set; }
    public string user { get; set; }
    public Sell1 sell { get; set; }
    public Buy1 buy { get; set; }
    public string amount_sold { get; set; }
    public DateTime expiration_timestamp { get; set; }
    public DateTime timestamp { get; set; }
    public DateTime updated_timestamp { get; set; }
}

public class ImxBuyOrderDto
{
    public int order_id { get; set; }
    public string status { get; set; }
    public string user { get; set; }
    public Sell2 sell { get; set; }
    public Buy2 buy { get; set; }
    public string amount_sold { get; set; }
    public DateTime expiration_timestamp { get; set; }
    public DateTime timestamp { get; set; }
    public DateTime updated_timestamp { get; set; }
}


public class RootobjectImxSellOrder
{
    public ImxSellOrderDto[] result { get; set; }
}

public class RootobjectImxBuyOrder
{
    public ImxBuyOrderDto[] result { get; set; }
}

public class Sell1
{
    public string type { get; set; }
    public SellData1 data { get; set; }
}

public class Sell2
{
    public string type { get; set; }
    public SellData2 data { get; set; }
}

public class SellData1
{
    public string token_id { get; set; }
    public string id { get; set; }
    public string token_address { get; set; }
    public string quantity { get; set; }
    public string quantity_with_fees { get; set; }
    public Properties properties { get; set; }
}

public class SellData2
{
    public string token_address { get; set; }
    public int decimals { get; set; }
    public string quantity { get; set; }
    public string quantity_with_fees { get; set; }
}

public class Properties
{
    public string name { get; set; }
    public string image_url { get; set; }
    public Collection collection { get; set; }
}

public class Collection
{
    public string name { get; set; }
    public string icon_url { get; set; }
}

public class Buy1
{
    public string type { get; set; }
    public BuyData1 data { get; set; }
}

public class Buy2
{
    public string type { get; set; }
    public BuyData2 data { get; set; }
}

public class BuyData1
{
    public string token_address { get; set; }
    public int decimals { get; set; }
    public string quantity { get; set; }
    public string quantity_with_fees { get; set; }
}

public class BuyData2
{
    public string token_id { get; set; }
    public string id { get; set; }
    public string token_address { get; set; }
    public string quantity { get; set; }
    public string quantity_with_fees { get; set; }
    public Properties properties { get; set; }
}
