
using WebApp.Data;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class Student
    {
        public int StudentID { get; set; }

        [Display(Name = "First Name")]
        [Required]
        public required string FirstName { get; set; }

        [Display(Name = "Last Name")]
        [Required]
        public required string LastName { get; set; }

        [Display(Name = "Email Address")]
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Display(Name = "Username")]
        [Required]
        public required string UserName { get; set; }

        public string? ApplicationUserId { get; set; }

        public ApplicationUser? ApplicationUser { get; set; }

        // Relationships
        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();

        public ICollection<CompletedCourse> CompletedCourses { get; set; }
            = new List<CompletedCourse>();
    }
}
