using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Data;
using Ecommerce.Application.Extensions;
using Ecommerce.Core.Entities;

using Francisvac.Result;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Stores;

public class StoreService : IStoreService
{
    private readonly IEcommerceDbContext _db;

    public StoreService(
        IEcommerceDbContext db)
    {
        _db = db;
    }

    public async Task<Result> AddProductAsync(int productId, int storeId)
    {
        if (await _db.ProductStores.AnyAsync(ps => ps.ProductId == productId && ps.StoreId == storeId)) return Result.Error("The product is already in the store");

        await _db.ProductStores.AddAsync(new ProductStore(productId, storeId));

        if (await _db.SaveChangesAsync() < 1) return Result.Error("Could not add the product");

        return Result.Success("The product was added successfully");
    }

    public async Task<Result> DecreaseProductAsync(int productId, int storeId)
    {
        ProductStore? productStore = await _db.ProductStores.FirstOrDefaultAsync(ps => ps.StoreId == storeId && ps.ProductId == productId);

        if (productStore is null) return Result.NotFound("The product is not found in the store");

        if (productStore.Quantity < 1)
        {
            return Result.Error("Stock sold out");
        }

        productStore.DecreaseQuantity();

        if (await _db.SaveChangesAsync() < 1) return Result.Error("Could not decrease the product");

        return Result.Success("The product was decrease successfully");
    }

    public async Task<Result> DeleteProductStoreRelation(int storeId)
    {
        var productStores = await _db.ProductStores.Where(ps => ps.StoreId.Equals(storeId)).ToListAsync();

        if (productStores.Count < 1) return Result.NotFound("The product is not found in the store");

        _db.ProductStores.RemoveRange(productStores);

        if (await _db.SaveChangesAsync() < 1) return Result.Error("Could not remove the product");

        return Result.Success("The product was remove successfully");
    }

    public async Task<Result> DeleteStore(int storeId)
    {
        Store? store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId);
    
        if (store is null) return Result.NotFound($"The store is not found");

        _db.Stores.Remove(store);

        await DeleteProductStoreRelation(storeId);

        return Result.Success("The store was remove successfully");
    }

    public async Task<Result> IncreaseProductAsync(int productId, int storeId)
    {
        var storeProduct = await _db.ProductStores.FirstOrDefaultAsync(ps => ps.StoreId == storeId && ps.ProductId == productId);

        if (storeProduct is null) return Result.NotFound("The product is not found in the store");

        storeProduct.IncreaseQuantity();

        if (await _db.SaveChangesAsync() < 1) return Result.Error("Could not increase the product");

        return Result.Success("The product was increase successfully");
    }

    public async Task<Result<PaginatedList<Product>>> ProductsFiltered(Pagination pagination, string? nameFilter, string? categoryFilter)
    {
        var productFilterQuery = _db.ProductStores
                                    .AsNoTracking()
                                    .Include(s => s.Store)
                                    .Include(ps => ps.Product).ThenInclude(p => p.Brand)
                                    .Include(ps => ps.Product).ThenInclude(p => p.Category)
                                    .Select(ps => ps.Product);

        if (!string.IsNullOrEmpty(nameFilter))
        {
            productFilterQuery = productFilterQuery.Where(p => p.Name.Contains(nameFilter!));
        }

        if (!string.IsNullOrEmpty(categoryFilter))
        {
            productFilterQuery = productFilterQuery.Where(p => p.Category.Name.Contains(categoryFilter));
        }

        var productPaginated = await productFilterQuery.PaginatedListAsync(pagination);

        return productPaginated.Items.Any()
                ? productPaginated
                : Result.NotFound("Products not found");
    }
}