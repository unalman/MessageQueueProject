using FluentAssertions;

namespace StockWorker.Tests
{
    public class InMemoryStockStoreTests()
    {
        [Fact]
        public void TryDecrease_Should_Decrease_Stock_When_Sufficient_Stock()
        {
            InMemoryStockStore store = new InMemoryStockStore();
            var sku = "SKU-1";
            var result = store.TryDecrease(sku, 9, out int remaining);

            result.Should().BeTrue();
            store.GetStock(sku).Should().Be(1);
        }

        [Fact]
        public void TryDecrease_Should_Not_Decrease_Stock_When_Insufficient_Stock()
        {
            InMemoryStockStore store = new InMemoryStockStore();

            var result = store.TryDecrease("SKU-2", 6, out int remaining);

            result.Should().BeFalse();
            store.GetStock("SKU-2").Should().Be(5);
        }

        [Fact]
        public void TryDecrease_Should_Not_Decrease_When_Quantity_Zero()
        {
            InMemoryStockStore store = new InMemoryStockStore();

            var result = store.TryDecrease("SKU-2", 0, out int remaining);

            result.Should().BeFalse();
        }

        [Fact]
        public void TryDecrease_Should_Not_Decrease_When_Quantity_Negative()
        {
            InMemoryStockStore store = new InMemoryStockStore();

            var result = store.TryDecrease("SKU-2", -1, out int remaining);

            result.Should().BeFalse();
        }

        [Fact]
        public void TryDecrease_Should_Not_Decrease_When_Sku_Not_Exist()
        {
            InMemoryStockStore store = new InMemoryStockStore();

            var result = store.TryDecrease("SKU-X", 1, out int remaining);

            result.Should().BeFalse();
        }

        [Fact]
        public void TryDecrease_Should_Return_Current_Stock_As_Remaining_When_Insufficient()
        {
            InMemoryStockStore store = new InMemoryStockStore();

            var result = store.TryDecrease("SKU-2", 6, out int remaining);

            result.Should().BeFalse();
            remaining.Should().Be(5);
        }

        [Fact]
        public void TryDecrease_Should_Be_Case_Insensitive()
        {
            InMemoryStockStore store = new InMemoryStockStore();

            var result = store.TryDecrease("sku-1", 2, out int remaining);

            result.Should().BeTrue();
            remaining.Should().Be(8);
        }
    }
}
