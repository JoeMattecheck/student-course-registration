namespace WebApp.Models
{
    public class Semester
    {
        public int SemesterID { get; set; }

        public required string SemesterName { get; set; }

        public int Year { get; set; }

        public ICollection<Course> Courses { get; set; }
            = new List<Course>();
    }
}
