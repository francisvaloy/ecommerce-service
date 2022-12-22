using System.Linq.Expressions;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Stores;
using Ecommerce.Core.Entities;

namespace Ecommerce.Application.UnitTests.Stores;

public class ProductServiceTest
{
    private readonly Product productMock = new("product", 200.00f, 1, 1, "https://url.com"){Id = 1};
    private readonly Store storeMock = new("store"){Id = 1};
    private readonly ProductStore productStoreMock = new(1, 1, 1);
    private readonly Mock<IEfRepository<Product>> productRepoMock = new();
    private readonly Mock<IEfRepository<Store>> storeRepoMock = new();
    private readonly Mock<IEfRepository<ProductStore>> productStoreRepoMock = new();

    private ProductService CreateProductService()
    {
        return new ProductService(
            productStoreRepoMock.Object
        );
    }

    [Fact]
    public void Should_Implement_IProductService()
    {
        typeof(ProductService).Should().BeAssignableTo<IProductService>();
    }

    [Fact]
    public void DeleteProductStoreRelation_WithNoSpecificProductInStore_ShouldThrowInvalidOperationException()
    {
        productStoreRepoMock.Setup(psr => psr.GetAll(It.IsAny<Expression<Func<ProductStore, bool>>>(), null!)).Returns<ProductStore>(null);

        var productServiceMock = CreateProductService();

        Action act = () => productServiceMock.DeleteProductStoreRelation(productMock.Id);

        act.Should().Throw<InvalidOperationException>().WithMessage($"Store doesnt have a product with Id: ${productMock.Id}");
    }

    [Fact]
    public void DeleteProductStoreRelation_WithSpecificProductInStore_ShouldRemoveTheStoreProductRelatedWithSpecificProduct()
    {
        IEnumerable<ProductStore> productStores = new List<ProductStore>()
        {
            productStoreMock
        };

        int removeStoreCall = 0;

        productStoreRepoMock.Setup(psr => psr.GetAll(It.IsAny<Expression<Func<ProductStore, bool>>>(), null!)).Returns(productStores);
        productStoreRepoMock.Setup(psr => psr.RemoveRange(It.IsAny<IEnumerable<ProductStore>>())).Callback(() => ++removeStoreCall);

        var productServiceMock = CreateProductService();

        productServiceMock.DeleteProductStoreRelation(productMock.Id);

        removeStoreCall.Should().Be(1);
    }

    [Fact]
    public async Task RelatedToStoreAsync_WithProductAlreadyRelatedToTheStore_ShouldThrowInvalidOperationException()
    {
        productStoreRepoMock.Setup(psr => psr.GetFirst(It.IsAny<Expression<Func<ProductStore, bool>>>(), null!)).Returns(productStoreMock);

        var productServiceMock = CreateProductService();

        Func<Task> act = () => productServiceMock.RelatedToStoreAsync(It.IsAny<int>(), It.IsAny<int>());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The product is already related to the store.");
    }

    [Fact]
    public async Task RelateToStoreAsync_WithProductNoRelatedAlready_ShouldCreateTheRelation()
    {
        productStoreRepoMock.Setup(psr => psr.GetFirst(It.IsAny<Expression<Func<ProductStore, bool>>>(), null!)).Returns<ProductStore>(null);       
        productStoreRepoMock.Setup(psr => psr.AddAsync(It.IsAny<ProductStore>()).Result).Returns(productStoreMock);

        var productServiceMock = CreateProductService();

        var result = await productServiceMock.RelatedToStoreAsync(productMock.Id, storeMock.Id);

        result.Should().Be(true);
    }
}