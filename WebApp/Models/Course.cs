namespace WebApp.Models
{
        public class Course
        {
            public int CourseID { get; set; }

            public required string CourseNumber { get; set; }
            public required string CourseName { get; set; }

            // Foreign key
            public int SemesterID { get; set; }

            // Relationships
            public Semester Semester { get; set; } = null!;

            public ICollection<Enrollment> Enrollments { get; set; }
                = new List<Enrollment>();

            public ICollection<CompletedCourse> CompletedCourses { get; set; }
                = new List<CompletedCourse>();

            // Courses that require this course
            public ICollection<CoursePrerequisite> Prerequisites { get; set; }
                = new List<CoursePrerequisite>();

            // Courses for which this course is a prerequisite
            public ICollection<CoursePrerequisite> RequiredForCourses { get; set; }
                = new List<CoursePrerequisite>();
        }
    }
