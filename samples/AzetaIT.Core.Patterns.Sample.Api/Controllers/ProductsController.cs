using AzetaIT.Core.Patterns.Sample.Products;
using Microsoft.AspNetCore.Mvc;

namespace AzetaIT.Core.Patterns.Sample.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await svc.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var product = await svc.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductRequest request, CancellationToken ct)
    {
        var created = await svc.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductUpdateRequest request, CancellationToken ct)
    {
        try
        {
            await svc.UpdateAsync(id, request, ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await svc.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
