

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using WebApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class CompletedCourseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompletedCourseController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Display completed courses for a specific student
        public async Task<IActionResult> Index(
            int studentId,
            string? searchString,
            string? sortOrder)
        {
            var student = await _context.Students
                .FindAsync(studentId);

            if (student == null)
                return NotFound();

            // Sorting options for the view
            ViewBag.SemesterSortParm =
                sortOrder == "semester_desc" ? "semester" : "semester_desc";

            ViewBag.NameSortParm =
                sortOrder == "name" ? "name_desc" : "name";

            ViewBag.IdSortParm =
                sortOrder == "id" ? "id_desc" : "id";

            ViewBag.CurrentFilter = searchString;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Student = student;

            // Get this student's completed courses
            var completedCourses = _context.CompletedCourses
                .Include(cc => cc.Course)
                    .ThenInclude(c => c.Semester)
                .Where(cc => cc.StudentID == studentId);

            // Search by course name, course number, or CourseID
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                if (int.TryParse(searchString, out int courseId))
                {
                    completedCourses = completedCourses.Where(cc =>
                        cc.Course.CourseName.Contains(searchString) ||
                        cc.Course.CourseNumber.Contains(searchString) ||
                        cc.Course.CourseID == courseId);
                }
                else
                {
                    completedCourses = completedCourses.Where(cc =>
                        cc.Course.CourseName.Contains(searchString) ||
                        cc.Course.CourseNumber.Contains(searchString));
                }
            }

            // Sort completed courses
            switch (sortOrder)
            {
                case "semester_desc":
                    completedCourses = completedCourses
                        .OrderByDescending(cc => cc.Course.Semester.Year)
                        .ThenByDescending(cc =>
                            cc.Course.Semester.SemesterName == "Spring" ? 1 :
                            cc.Course.Semester.SemesterName == "Summer" ? 2 :
                            cc.Course.Semester.SemesterName == "Fall" ? 3 : 4)
                        .ThenBy(cc => cc.Course.CourseName)
                        .ThenBy(cc => cc.Course.CourseID);
                    break;

                case "semester":
                    completedCourses = completedCourses
                        .OrderBy(cc => cc.Course.Semester.Year)
                        .ThenBy(cc =>
                            cc.Course.Semester.SemesterName == "Spring" ? 1 :
                            cc.Course.Semester.SemesterName == "Summer" ? 2 :
                            cc.Course.Semester.SemesterName == "Fall" ? 3 : 4)
                        .ThenBy(cc => cc.Course.CourseName)
                        .ThenBy(cc => cc.Course.CourseID);
                    break;

                case "name":
                    completedCourses = completedCourses
                        .OrderBy(cc => cc.Course.CourseName)
                        .ThenBy(cc => cc.Course.Semester.Year)
                        .ThenBy(cc => cc.Course.CourseID);
                    break;

                case "name_desc":
                    completedCourses = completedCourses
                        .OrderByDescending(cc => cc.Course.CourseName)
                        .ThenBy(cc => cc.Course.Semester.Year)
                        .ThenBy(cc => cc.Course.CourseID);
                    break;

                case "id":
                    completedCourses = completedCourses
                        .OrderBy(cc => cc.Course.CourseID);
                    break;

                case "id_desc":
                    completedCourses = completedCourses
                        .OrderByDescending(cc => cc.Course.CourseID);
                    break;

                default:
                    // Default: semester ascending
                    completedCourses = completedCourses
                        .OrderBy(cc => cc.Course.Semester.Year)
                        .ThenBy(cc =>
                            cc.Course.Semester.SemesterName == "Spring" ? 1 :
                            cc.Course.Semester.SemesterName == "Summer" ? 2 :
                            cc.Course.Semester.SemesterName == "Fall" ? 3 : 4)
                        .ThenBy(cc => cc.Course.CourseName)
                        .ThenBy(cc => cc.Course.CourseID);
                    break;
            }

            return View(await completedCourses.ToListAsync());
        }

        // Display completed course form
        public async Task<IActionResult> Create(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);

            if (student == null)
                return NotFound();

            ViewBag.Student = student;

            // Get only approved courses the student has not completed
            var courses = await _context.Enrollments
                .Where(e => e.StudentID == studentId &&
                            e.Status == "Approved" &&
                            !_context.CompletedCourses.Any(cc =>
                                cc.StudentID == studentId &&
                                cc.CourseID == e.CourseID))
                .Select(e => e.Course)
                .ToListAsync();

            ViewBag.CourseID = new SelectList(
                courses,
                "CourseID",
                "CourseNumber"
            );

            return View();
        }

        // Save completed course
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int studentId, int courseId)
        {
            var student = await _context.Students.FindAsync(studentId);

            if (student == null)
                return NotFound();

            // Check if the student already completed this course
            bool alreadyCompleted = await _context.CompletedCourses.AnyAsync(cc =>
                cc.StudentID == studentId &&
                cc.CourseID == courseId);

            if (alreadyCompleted)
            {
                TempData["ErrorMessage"] =
                    "This student has already completed this course.";

                return RedirectToAction(nameof(Index), new { studentId });
            }

            // Verify that the student has an approved enrollment
            bool isApprovedEnrollment = await _context.Enrollments.AnyAsync(e =>
                e.StudentID == studentId &&
                e.CourseID == courseId &&
                e.Status == "Approved");

            if (!isApprovedEnrollment)
            {
                TempData["ErrorMessage"] =
                    "This student is not approved for this course.";

                return RedirectToAction(nameof(Index), new { studentId });
            }

            var completedCourse = new CompletedCourse
            {
                StudentID = studentId,
                CourseID = courseId
            };

            _context.CompletedCourses.Add(completedCourse);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Course successfully marked as completed.";

            return RedirectToAction(nameof(Index), new { studentId });
        }

        // Remove Completed Class
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int studentId)
        {
            var completedCourse = await _context.CompletedCourses
                .FirstOrDefaultAsync(cc =>
                    cc.CompletedCourseID == id &&
                    cc.StudentID == studentId);

            if (completedCourse == null)
                return NotFound();

            _context.CompletedCourses.Remove(completedCourse);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new { studentId }
            );
        }

        // Display completed courses for the logged-in student
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyCourses()
        {
            var userId = _userManager.GetUserId(User);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (student == null)
                return NotFound();

            List<CompletedCourse> completedCourses =
                await _context.CompletedCourses
                    .Include(cc => cc.Course)
                        .ThenInclude(c => c.Semester)
                    .Where(cc => cc.StudentID == student.StudentID)
                    .ToListAsync();

            ViewBag.Student = student;

            return View(completedCourses);
        }
    }
}

