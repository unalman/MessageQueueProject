using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace StockWorker.Tests
{
    public class InMemoryStockStoreTests()
    {
        [Fact]
        public async Task TryReserve_Should_Reserve_Stock()
        {
            InMemoryStockStore store = new InMemoryStockStore();
            var sku = "SKU-1";
            var result = store.TryReserve(sku, 9);

            result.Should().BeTrue();

            var stock = store.GetStock(sku);
            stock.Should().NotBeNull();
            stock.Available.Should().Be(1);
            stock.Reseverved.Should().Be(9);
        }

        [Fact]
        public async Task TryReserve_Should_ReturnFalse_When_Insufficient_Stock()
        {
            InMemoryStockStore store = new InMemoryStockStore();
            var sku = "SKU-1";
            var result = store.TryReserve(sku, 11);

            result.Should().BeFalse();

            var stock = store.GetStock(sku);
            stock.Should().NotBeNull();
            stock.Available.Should().Be(10);
            stock.Reseverved.Should().Be(0);
        }

        [Fact]
        public async Task TryReserve_Should_ReturnFalse_When_Quantity_Zero()
        {
            InMemoryStockStore store = new InMemoryStockStore();

            var sku = "SKU-1";
            var result = store.TryReserve(sku, 0);

            result.Should().BeFalse();

            var stock = store.GetStock(sku);
            stock.Should().NotBeNull();
            stock.Available.Should().Be(10);
            stock.Reseverved.Should().Be(0);
        }

        [Fact]
        public async Task TryReserve_Should_ReturnFalse_When_Quantity_Negative()
        {
            InMemoryStockStore store = new InMemoryStockStore();

            var sku = "SKU-1";
            var result = store.TryReserve(sku, -1);

            result.Should().BeFalse();

            var stock = store.GetStock(sku);
            stock.Should().NotBeNull();
            stock.Available.Should().Be(10);
            stock.Reseverved.Should().Be(0);
        }

        [Fact]
        public async Task TryReserve_Should_ReturnFalse_When_Sku_Not_Exist()
        {
            InMemoryStockStore store = new InMemoryStockStore();

            var result = store.TryReserve("SKU-X", 1);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task TryReserve_Should_Be_Case_Insensitive()
        {
            InMemoryStockStore store = new InMemoryStockStore();

            var result = store.TryReserve("sku-1", 2);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task Release_Should_Release_Stock()
        {
            InMemoryStockStore store = new InMemoryStockStore();
            var sku = "SKU-1";

            var result = store.TryReserve(sku, 5);
            result.Should().BeTrue();

            var resultRelease = store.Release(sku, 5);
            resultRelease.Should().BeTrue();

            var stock = store.GetStock(sku);

            stock.Should().NotBeNull();
            stock.Available.Should().Be(10);
            stock.Reseverved.Should().Be(0);
        }

        [Fact]
        public async Task Release_Should_ReturnFalse_When_Insufficient_Reserved()
        {
            var store = new InMemoryStockStore();
            var sku = "SKU-1";
            var result = store.TryReserve(sku, 5);
            result.Should().BeTrue();

            var resultRelease = store.Release(sku, 6);
            resultRelease.Should().BeFalse();

            var stock = store.GetStock(sku);
            stock.Should().NotBeNull();
            stock.Available.Should().Be(5);
            stock.Reseverved.Should().Be(5);
        }

        [Fact]
        public async Task Release_Should_ReturnFalse_When_Sku_Not_Exists()
        {
            var store = new InMemoryStockStore();
            var sku = "SKU-X";
            var result = store.Release(sku, 1);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task Commit_Should_Commit_Stock()
        {
            var store = new InMemoryStockStore();
            var sku = "SKU-1";
            var result = store.TryReserve(sku, 1);
            result.Should().BeTrue();

            var resultCommit = store.Commit(sku, 1);
            resultCommit.Should().BeTrue();

            var stock = store.GetStock(sku);
            stock.Should().NotBeNull();
            stock.Available.Should().Be(9);
            stock.Reseverved.Should().Be(0);
        }

        [Fact]
        public async Task Commit_Should_ReturnFalse_When_Insufficient_Reserved()
        {
            var store = new InMemoryStockStore();
            var sku = "SKU-1";
            var result = store.TryReserve(sku, 2);
            result.Should().BeTrue();

            var resultCommit = store.Commit(sku, 3);
            resultCommit.Should().BeFalse();

            var stock = store.GetStock(sku);
            stock.Should().NotBeNull();
            stock.Available.Should().Be(8);
            stock.Reseverved.Should().Be(2);
        }

        [Fact]
        public async Task Commit_Should_ReturnFalse_When_Sku_Not_Exists()
        {
            var store = new InMemoryStockStore();
            var sku = "SKU-X";
            var result = store.Commit(sku, 1);
            result.Should().BeFalse();
        }
    }
}
