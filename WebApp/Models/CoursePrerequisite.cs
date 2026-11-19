namespace WebApp.Models;
public class CoursePrerequisite
{
    public int CourseID { get; set; }

    public int PrerequisiteCourseID { get; set; }

    public Course Course { get; set; } = null!;

    public Course PrerequisiteCourse { get; set; } = null!;
}
