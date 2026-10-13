namespace SkillLoop.Api.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }          // display order in "Explore by Category"

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}