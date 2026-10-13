using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CoursesController(AppDbContext db) => _db = db;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /api/courses?search=&categoryId=&page=&pageSize=
    [HttpGet]
    public async Task<ActionResult<PagedResult<CourseListItemDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = _db.Courses
            .AsNoTracking()
            .Where(c => c.Status == CourseStatus.Active && (c.EndDate == null || c.EndDate >= today));

        if (categoryId.HasValue)
            query = query.Where(c => c.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Title.Contains(term)
                                  || c.Description.Contains(term)
                                  || c.Tags.Contains(term)
                                  || c.Category.Name.Contains(term)
                                  || _db.Users.Any(u => u.Id == c.TeacherId && u.FullName.Contains(term)));
        }

        var total = await query.CountAsync();

        var items = await (
            from c in query
            join u in _db.Users on c.TeacherId equals u.Id
            orderby c.CreatedAt descending
            select new CourseListItemDto
            {
                Id = c.Id,
                Title = c.Title,
                CategoryId = c.CategoryId,
                CategoryName = c.Category.Name,
                CoverImageUrl = c.CoverImageUrl,
                Level = c.Level,
                PricePerSession = c.PricePerSession,
                DurationMinutes = c.DurationMinutes,
                TotalSessions = c.TotalSessions,
                TeacherId = c.TeacherId,
                TeacherName = u.FullName,
                TeacherAvatarUrl = u.AvatarUrl
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<CourseListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    // GET /api/courses/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseDetailsDto>> GetById(int id)
    {
        var row = await (
            from c in _db.Courses.AsNoTracking()
            join u in _db.Users on c.TeacherId equals u.Id
            where c.Id == id && c.Status == CourseStatus.Active
            select new
            {
                Dto = new CourseDetailsDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    CategoryId = c.CategoryId,
                    CategoryName = c.Category.Name,
                CoverImageUrl = c.CoverImageUrl,
                    Level = c.Level,
                    PricePerSession = c.PricePerSession,
                    DurationMinutes = c.DurationMinutes,
                    TotalSessions = c.TotalSessions,
                    TeacherId = c.TeacherId,
                    TeacherName = u.FullName,
                    TeacherAvatarUrl = u.AvatarUrl,
                    TeacherJobTitle = u.JobTitle,
                    CreatedAt = c.CreatedAt,
                    MaxStudentsPerSession = c.MaxStudentsPerSession,
                    AllowsOnline = c.AllowsOnline,
                    AllowsOffline = c.AllowsOffline,
                    OfflineLocation = c.OfflineLocation,
                    EndDate = c.EndDate
                },
                c.Tags,
                c.LearningOutcomes
            })
            .FirstOrDefaultAsync();

        if (row is null)
            return NotFound(new ApiError("Course not found.", "COURSE_NOT_FOUND"));

        var dto = row.Dto;

        dto.Tags = row.Tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        dto.LearningOutcomes = row.LearningOutcomes
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        dto.StudentsCount = await _db.Bookings
            .Where(b => b.CourseId == id && b.Status != BookingStatus.Cancelled)
            .Select(b => b.StudentId)
            .Distinct()
            .CountAsync();

        dto.IsSaved = await _db.SavedCourses
            .AnyAsync(s => s.CourseId == id && s.UserId == CurrentUserId);

        return Ok(dto);
    }
}