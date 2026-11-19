namespace WebApp.Models
{

    public class CompletedCourse
    {
        public int CompletedCourseID { get; set; }

        public int StudentID { get; set; }
        public int CourseID { get; set; }

        public Student Student { get; set; } = null!;
        public Course Course { get; set; } = null!;
    }

}
