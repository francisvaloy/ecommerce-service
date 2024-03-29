using Ecommerce.Application.Data;
using Ecommerce.Application.Stores;
using Ecommerce.Core.Entities;

using Microsoft.EntityFrameworkCore;
using Francisvac.Result;

namespace Ecommerce.Application.Baskets;

public class BasketService : IBasketService
{
    private readonly IStoreService _storeService;
    private readonly IEcommerceDbContext _db;

    public BasketService(
        IStoreService storeService,
        IEcommerceDbContext db)
    {
        _storeService = storeService;
        _db = db;
    }

    public async Task<Result> RestoreTheQuantityIntoStore(Basket basket)
    {
        Store store = await _db.Stores.FirstAsync();

        ProductStore? productStore = await _db.ProductStores.FirstOrDefaultAsync(s => s.ProductId == basket.ProductId && s.StoreId == store.Id);

        if (productStore is null) return Result.NotFound($"No product with id {basket.ProductId} is related to the store.");

        productStore.IncreaseQuantity(basket.Quantity);

        if (await _db.SaveChangesAsync() < 1) return Result.Error("Changes were not saved to the database.");

        return Result.Success("the product was restored to the store successfully.");
    }

    public async Task<Result> AddProductAsync(int productId, string userId)
    {
        Store store = await _db.Stores.FirstAsync();

        ProductStore? productStore = await _db.ProductStores
                                            .Include(ps => ps.Product)
                                            .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.StoreId == store.Id);
   
        if (productStore is null || productStore.Quantity == 0) return Result.Error("No products in stock.");

        Basket? userBasketExist = await _db.Baskets
                                            .FirstOrDefaultAsync(b => b.ProductId == productId && b.ApplicationUserId == userId);
        
        if (userBasketExist is not null) return Result.Error("The basket already contains the product.");

        Basket userBasket = new(productId, productStore.Product, userId);

        await _db.Baskets.AddAsync(userBasket);

        Result decreaseProductFromStoreResult = await _storeService.DecreaseProductAsync(productId, store.Id);

        if (!decreaseProductFromStoreResult.IsSuccess) return decreaseProductFromStoreResult;

        return Result.Success("Product added to basket satisfactorily.");
    }

    public async Task<Result> IncreaseProduct(int productId, string userId)
    {
        Store store = await _db.Stores.FirstAsync();

        Basket? userBasket = await _db.Baskets.Include(b => b.Product).FirstOrDefaultAsync(b => b.ProductId == productId && b.ApplicationUserId == userId);

        if (userBasket is null) return Result.NotFound($"The product with id {productId} is not found in the basket.");

        Result decreaseProductFromStoreResult = await _storeService.DecreaseProductAsync(productId: productId, storeId: store.Id);

        if (!decreaseProductFromStoreResult.IsSuccess) return decreaseProductFromStoreResult;

        userBasket.IncreaseProductQuantity();

        if (await _db.SaveChangesAsync() < 1) return Result.Error("Changes were not saved to the database.");

        return Result.Success("The product was successfully increased.");
    }

    public async Task<Result> DecreaseProduct(int productId, string userId)
    {
        Store store = await _db.Stores.FirstAsync();

        Basket? userBasket = await _db.Baskets
                                    .Include(b => b.Product)                            
                                    .FirstOrDefaultAsync(b => b.ProductId == productId && b.ApplicationUserId == userId);

        if (userBasket is null) return Result.NotFound($"The basket does not contain the product with id {productId}.");

        Result increaseProductFromStoreResult = await _storeService.IncreaseProductAsync(productId: productId, storeId: store.Id);

        if (!increaseProductFromStoreResult.IsSuccess) return increaseProductFromStoreResult;
        
        int amountDecreased = userBasket.DecreaseProductQuantity();

        if (amountDecreased == 0 || userBasket.Quantity == 0)
        {
            _db.Baskets.Remove(userBasket);
            await _db.SaveChangesAsync();
            return Result.Success("The product was remove from the basket");
        }

        if (await _db.SaveChangesAsync() < 1) return Result.Error("Changes were not saved to the database.");

        return Result.Success("The product was successfully increased.");
    }

    public async Task<Result<(IEnumerable<Product>, float)>> GetAllProducts(string userId)
    {
        List<Basket> userBasket = await _db.Baskets
                                                .Include(b => b.Product)
                                                .ThenInclude(p => p.Brand)
                                                .Include(b => b.Product)
                                                .ThenInclude(p => p.Category)
                                                .Where(b => b.ApplicationUserId == userId)
                                                .ToListAsync();

        if (userBasket is null || userBasket.Count < 1) return Result.NotFound("No product was found in the basket.");

        List<Product> userBasketProducts = userBasket.Select(sb => sb.Product).ToList();

        float total = userBasket.Select(ub => ub.Total).Sum();

        return (userBasketProducts, total);
    }

    public async Task<Result> RemoveProduct(int productId, string UserId)
    {
        Basket? userBasket = await _db.Baskets
                                        .Include(b => b.Product)
                                        .FirstOrDefaultAsync(b => b.ApplicationUserId == UserId && b.ProductId == productId);

        if (userBasket is null) return Result.NotFound("The product was not found in the basket.");

        Result amountRestoredInStoreResult = await RestoreTheQuantityIntoStore(userBasket);

        if (!amountRestoredInStoreResult.IsSuccess) return amountRestoredInStoreResult;

        _db.Baskets.Remove(userBasket);

        if (await _db.SaveChangesAsync() < 1) return Result.Error("Changes were not saved to the database.");

        return Result.Success("The product was removed from the basket successfully.");
    }

    public async Task<Result<int[]>> GetProductIds(string userId)
    {
        var userBasket = await _db.Baskets.Where(b => b.ApplicationUserId == userId)
                                    .Select(b => b.Product.Id)
                                    .ToArrayAsync();

        if (userBasket.Length < 1)
            return Result.NotFound("The user doesn't has product in cart");

        return userBasket;
    }
}
