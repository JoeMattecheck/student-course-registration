

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace WebApp.Controllers
{
    [Authorize]
    public class EnrollmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EnrollmentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Display enrollments for a specific student
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (student == null)
                return NotFound();

            var enrollments = await _context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentID == student.StudentID
            && !_context.CompletedCourses.Any(cc =>
            cc.StudentID == e.StudentID &&
            cc.CourseID == e.CourseID))
            .ToListAsync();

            ViewBag.Student = student;

            return View(enrollments);
        }

        // Display available courses with search and sorting
        public async Task<IActionResult> Create(
            string searchString,
            string sortOrder)
        {
            var userId = _userManager.GetUserId(User);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (student == null)
                return NotFound();

            ViewBag.Student = student;
            ViewBag.CurrentFilter = searchString;
            ViewBag.SortOrder = sortOrder;

            var courses = _context.Courses
                .Include(c => c.Semester)
                .AsQueryable();

            // Search by course number or course name
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                courses = courses.Where(c =>
                    c.CourseNumber.Contains(searchString) ||
                    c.CourseName.Contains(searchString));
            }

            // Sort courses
            courses = sortOrder switch
            {
                "name" => courses
                    .OrderBy(c => c.Semester.Year)
                    .ThenBy(c => c.Semester.SemesterName)
                    .ThenBy(c => c.CourseName),

                "name_desc" => courses
                    .OrderBy(c => c.Semester.Year)
                    .ThenBy(c => c.Semester.SemesterName)
                    .ThenByDescending(c => c.CourseName),

                "number" => courses
                    .OrderBy(c => c.Semester.Year)
                    .ThenBy(c => c.Semester.SemesterName)
                    .ThenBy(c => c.CourseNumber),

                "number_desc" => courses
                    .OrderBy(c => c.Semester.Year)
                    .ThenBy(c => c.Semester.SemesterName)
                    .ThenByDescending(c => c.CourseNumber),

                "semester_desc" => courses
                    .OrderByDescending(c => c.Semester.Year)
                    .ThenByDescending(c => c.Semester.SemesterName)
                    .ThenBy(c => c.CourseNumber),

                _ => courses
                    .OrderBy(c => c.Semester.Year)
                    .ThenBy(c => c.Semester.SemesterName)
                    .ThenBy(c => c.CourseNumber)
            };

            return View(await courses.ToListAsync());
        }


        // Save enrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int courseId)
        {
            // Validate course selection
            if (courseId <= 0)
            {
                TempData["ErrorMessage"] = "Please select a course.";
                return RedirectToAction(nameof(Create));
            }

            var userId = _userManager.GetUserId(User);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (student == null)
                return NotFound();

            int studentId = student.StudentID;

            var course = await _context.Courses.FindAsync(courseId);

            if (course == null)
                return NotFound();

            // Check for duplicate enrollment
            bool alreadyEnrolled = await _context.Enrollments.AnyAsync(e =>
                e.StudentID == studentId &&
                e.CourseID == courseId);

            if (alreadyEnrolled)
            {
                TempData["ErrorMessage"] =
                    "Student is already enrolled in this course.";

                return RedirectToAction(nameof(Index));
            }

            // Find prerequisites the student has not completed
            var missingPrerequisites = await _context.CoursePrerequisites
                .Where(cp =>
                    cp.CourseID == courseId &&
                    !_context.CompletedCourses.Any(cc =>
                        cc.StudentID == studentId &&
                        cc.CourseID == cp.PrerequisiteCourseID))
                .Select(cp => cp.PrerequisiteCourse.CourseNumber)
                .ToListAsync();

            // Prevent enrollment if prerequisites are missing
            if (missingPrerequisites.Any())
            {
                TempData["ErrorMessage"] =
                    "Cannot enroll. The student must complete these prerequisites first: "
                    + string.Join(", ", missingPrerequisites);

                return RedirectToAction(nameof(Index));
            }

            // Create enrollment
            var enrollment = new Enrollment
            {
                StudentID = studentId,
                CourseID = courseId,
                Status = "Pending"
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // Drop a course
        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Drop(int id)
        {
            var userId = _userManager.GetUserId(User);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (student == null)
                return NotFound();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.EnrollmentID == id &&
                    e.StudentID == student.StudentID);

            if (enrollment == null)
                return NotFound();

            _context.Enrollments.Remove(enrollment);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Display all enrollment requests for Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var enrollments = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Where(e => e.Status == "Pending")
            .ToListAsync();

            return View(enrollments);
        }

        // Update enrollment status
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            int studentId,
            string status)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.EnrollmentID == id &&
                    e.StudentID == studentId);

            if (enrollment == null)
                return NotFound();

            // Only allow these statuses
            if (status != "Approved" && status != "Rejected")
            {
                return BadRequest();
            }

            enrollment.Status = status;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(AdminIndex));
        }

        // Delete enrollment
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int studentId)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.EnrollmentID == id &&
                    e.StudentID == studentId);

            if (enrollment == null)
                return NotFound();

            _context.Enrollments.Remove(enrollment);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(AdminIndex));
        }
    }
}
