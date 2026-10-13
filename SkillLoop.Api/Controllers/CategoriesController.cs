using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;

namespace SkillLoop.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Icon))
            .ToListAsync();

        return Ok(categories);
    }
}