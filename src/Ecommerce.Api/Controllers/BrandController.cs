using Ecommerce.Core.Enums;
using Ecommerce.Core.Entities;
using Ecommerce.Application.Data;
using Ecommerce.Contracts.Brands;

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AutoMapper.QueryableExtensions;
using Ecommerce.Contracts.Endpoints;

namespace Ecommerce.Api.Controllers;

// public class BrandController : ApiControllerBase
[Authorize]
[ApiController]
[Route("api/")]
public class BrandController : ControllerBase
{
    private readonly IEcommerceDbContext _db;
    private readonly IMapper _mapper;

    public BrandController(
        IMapper mapper,
        IEcommerceDbContext db)
    {
        _mapper = mapper;
        _db = db;
    }

    [HttpGet]
    [Route(BrandEndpoints.GetAllBrands)]
    [ProducesResponseType(typeof(List<BrandResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BrandResponse>>> Get()
    {
        List<BrandResponse> brands = await _db.Brands.ProjectTo<BrandResponse>(_mapper.ConfigurationProvider).ToListAsync();

        return Ok(brands);
    }

    [HttpGet]
    [Route(BrandEndpoints.GetBrandById + "{id}")]
    [ProducesResponseType(typeof(BrandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandResponse>> Get(int id)
    {
        if (id < 1) return BadRequest("Invalid id");

        Brand? brand = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id);

        if (brand is null) return NotFound($"Brand with the id::{id} not found");

        return Ok(_mapper.Map<BrandResponse>(brand));
    }

    [HttpPost]
    [Route(BrandEndpoints.CreateBrand)]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(BrandResponse), StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBrandRequest brandRequest)
    {
        Brand brand = _mapper.Map<Brand>(brandRequest);

        await _db.Brands.AddAsync(brand);

        await _db.SaveChangesAsync();

        if (brand.Id < 1) return BadRequest("Could not create the brand");

        return RedirectToAction("Get", new { id = brand.Id });
    }

    [HttpPut]
    [Route(BrandEndpoints.EditBrand + "{id}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Edit(int id, [FromBody] EditBrandRequest brandRequest)
    {
        if (id < 1) return BadRequest("Invalid id");

        if (!await _db.Brands.AnyAsync(b => b.Id == id)) return NotFound($"Brand with the id::{id} not found");

        Brand brandToUpdate = _mapper.Map<Brand>(brandRequest);
        brandToUpdate.Id = id;

        _db.Brands.Update(brandToUpdate);

        if (await _db.SaveChangesAsync() < 1) return BadRequest("Could not edit the brand");

        return NoContent();
    }

    [HttpDelete]
    [Route(BrandEndpoints.DeleteBrand + "{id}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        if (id < 1) return BadRequest("Invalid id");

        Brand? brandToDelete = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id);

        if (brandToDelete is null) return NotFound($"Brand with the id::{id} not found");

        _db.Brands.Remove(brandToDelete);

        if (await _db.SaveChangesAsync() < 1) return BadRequest("Could not delete the brand");

        return NoContent();
    }
}