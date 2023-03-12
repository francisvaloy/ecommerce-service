using Ecommerce.Contracts.Baskets;

namespace Ecommerce.Api.IntegrationTests.Controllers.Basket;

[Collection("BaseIntegrationTestCollection")]
public class GetAllProductTests
{
    readonly BaseIntegrationTest _baseIntegrationTest;

    readonly string endPointPath = "api/basket/GetAllProductInBasket";
    const string AddProductToBasketPath = "api/basket/AddProductToBasket?productId=";
    public GetAllProductTests(BaseIntegrationTest baseIntegrationTest)
    {
        _baseIntegrationTest = baseIntegrationTest;
    }

    [Fact]
    public async Task ShouldReturnOk_WhenUserHasProductsInBasket()
    {
        // Arrange
        using var db = _baseIntegrationTest.EcommerceProgram.CreateApplicationDbContext();

        var product = db.Products.First();

        _ = await _baseIntegrationTest.DefaultUserHttpClient.PostAsync(AddProductToBasketPath + product.Id, null);

        _ = await _baseIntegrationTest.DefaultUserHttpClient.PostAsync(AddProductToBasketPath + (product.Id + 1), null);

        // Act
        var response = await _baseIntegrationTest.DefaultUserHttpClient.GetAsync(endPointPath);

        string responseReadedAsString = await response.Content.ReadAsStringAsync();

        BasketResponse basketProducts = JsonConvert.DeserializeObject<BasketResponse>(responseReadedAsString);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        basketProducts.Should().NotBeNull();

        basketProducts.Products.Count().Should().BeGreaterThanOrEqualTo(2);

        basketProducts.Total.Should().BeGreaterThan(0);
    }
}