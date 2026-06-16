namespace StockWorker.Models
{
    public sealed class StockItem
    {
        public StockItem(int available, int reserved = 0)
        {
            Available = available;
            Reseverved = reserved;
        }

        public int Available { get; set; }
        public int Reseverved { get; set; }
    }
}
