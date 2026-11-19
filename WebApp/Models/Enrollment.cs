
namespace WebApp.Models;

public class Enrollment
{
    public int EnrollmentID { get; set; }

    // Foreign keys
    public int StudentID { get; set; }
    public int CourseID { get; set; }

    public string Status { get; set; } = "Pending";

    // Navigation properties
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
