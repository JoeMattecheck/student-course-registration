
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.Data;

namespace WebApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tables
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<CompletedCourse> CompletedCourses { get; set; }
        public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }
        public DbSet<Semester> Semesters { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Student -> ApplicationUser (one-to-one)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.ApplicationUser)
                .WithOne()
                .HasForeignKey<Student>(s => s.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Semester -> Course
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Semester)
                .WithMany(s => s.Courses)
                .HasForeignKey(c => c.SemesterID)
                .OnDelete(DeleteBehavior.Restrict);

            // Student -> Enrollment
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentID)
                .OnDelete(DeleteBehavior.Restrict);

            // Course -> Enrollment
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseID)
                .OnDelete(DeleteBehavior.Restrict);

            // Student -> CompletedCourse
            modelBuilder.Entity<CompletedCourse>()
                .HasOne(cc => cc.Student)
                .WithMany(s => s.CompletedCourses)
                .HasForeignKey(cc => cc.StudentID)
                .OnDelete(DeleteBehavior.Restrict);

            // Course -> CompletedCourse
            modelBuilder.Entity<CompletedCourse>()
                .HasOne(cc => cc.Course)
                .WithMany(c => c.CompletedCourses)
                .HasForeignKey(cc => cc.CourseID)
                .OnDelete(DeleteBehavior.Restrict);

            // Course prerequisites
            modelBuilder.Entity<CoursePrerequisite>()
                .HasKey(cp => new
                {
                    cp.CourseID,
                    cp.PrerequisiteCourseID
                });

            modelBuilder.Entity<CoursePrerequisite>()
                .HasOne(cp => cp.Course)
                .WithMany(c => c.Prerequisites)
                .HasForeignKey(cp => cp.CourseID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CoursePrerequisite>()
                .HasOne(cp => cp.PrerequisiteCourse)
                .WithMany(c => c.RequiredForCourses)
                .HasForeignKey(cp => cp.PrerequisiteCourseID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
