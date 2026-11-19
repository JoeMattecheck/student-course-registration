
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Course/Index

        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentFilter = searchString;

            ViewBag.CourseNumberSortParm =
                sortOrder == "CourseNumber" ? "course_number_desc" : "CourseNumber";

            ViewBag.CourseNameSortParm =
                sortOrder == "CourseName" ? "course_name_desc" : "CourseName";

            ViewBag.SemesterSortParm =
                sortOrder == "Semester" ? "semester_desc" : "Semester";

            var courses = _context.Courses
                .Include(c => c.Semester)
                .AsQueryable();

            // Search by course number or course name
            if (!string.IsNullOrEmpty(searchString))
            {
                courses = courses.Where(c =>
                    c.CourseNumber.Contains(searchString) ||
                    c.CourseName.Contains(searchString));
            }

            // Sort courses
            courses = sortOrder switch
            {
                "CourseNumber" => courses.OrderBy(c => c.CourseNumber),
                "course_number_desc" => courses.OrderByDescending(c => c.CourseNumber),

                "CourseName" => courses.OrderBy(c => c.CourseName),
                "course_name_desc" => courses.OrderByDescending(c => c.CourseName),

                "Semester" => courses.OrderBy(c => c.Semester.Year)
                                      .ThenBy(c => c.Semester.SemesterName),

                "semester_desc" => courses.OrderByDescending(c => c.Semester.Year)
                                           .ThenByDescending(c => c.Semester.SemesterName),

                _ => courses.OrderBy(c => c.CourseNumber)
            };

            return View(await courses.ToListAsync());
        }


        // GET: Course/Create
        public async Task<IActionResult> Create()
        {
            await LoadSemesters();

            return View();
        }

        // POST: Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course)
        {
            ModelState.Remove("Semester");

            if (ModelState.IsValid)
            {
                _context.Courses.Add(course);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadSemesters();

            return View(course);
        }

        // Loads available semesters into the dropdown
        private async Task LoadSemesters()
        {
            var semesters = await _context.Semesters.ToListAsync();

            ViewBag.SemesterID = new SelectList(
                semesters,
                "SemesterID",
                "SemesterName"
            );
        }
        // GET: Course/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var course = await _context.Courses
                .Include(c => c.Semester)
                .Include(c => c.Prerequisites)
                    .ThenInclude(cp => cp.PrerequisiteCourse)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
                return NotFound();

            return View(course);
        }
        // GET: Course/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            await LoadSemesters();

            return View(course);
        }
        // POST: Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course course)
        {
            if (id != course.CourseID)
            {
                return NotFound();
            }

            ModelState.Remove("Semester");

            if (ModelState.IsValid)
            {
                var existingCourse = await _context.Courses.FindAsync(id);

                if (existingCourse == null)
                {
                    return NotFound();
                }

                existingCourse.CourseNumber = course.CourseNumber;
                existingCourse.CourseName = course.CourseName;
                existingCourse.SemesterID = course.SemesterID;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadSemesters();

            return View(course);
        }
        // GET: Course/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }
        // POST: Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.CompletedCourses)
                .Include(c => c.Prerequisites)
                .Include(c => c.RequiredForCourses)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                return NotFound();
            }

            if (course.Enrollments.Any() ||
                course.CompletedCourses.Any() ||
                course.Prerequisites.Any() ||
                course.RequiredForCourses.Any())
            {
                TempData["ErrorMessage"] =
                    "Cannot delete this course because it has associated records.";

                return RedirectToAction(nameof(Index));
            }

            _context.Courses.Remove(course);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        //Manage Prerequisites
        public async Task<IActionResult> ManagePrerequisites(int? id)
        {
            if (id == null)
                return NotFound();

            var course = await _context.Courses
                .Include(c => c.Prerequisites)
                    .ThenInclude(cp => cp.PrerequisiteCourse)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
                return NotFound();

            var availableCourses = await _context.Courses
                .Where(c => c.CourseID != id)
                .ToListAsync();

            ViewBag.AvailableCourses = new SelectList(
                availableCourses,
                "CourseID",
                "CourseNumber"
            );

            return View(course);
        }

        //Add Prerequisite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPrerequisite(
            int courseId,
            int prerequisiteCourseId)
        {
            // Prevent a course from being its own prerequisite
            if (courseId == prerequisiteCourseId)
            {
                TempData["ErrorMessage"] =
                    "A course cannot be its own prerequisite.";

                return RedirectToAction(
                    nameof(ManagePrerequisites), new { id = courseId });
            }

            // Verify both courses exist
            var course = await _context.Courses.FindAsync(courseId);
            var prerequisite = await _context.Courses.FindAsync(prerequisiteCourseId);

            if (course == null || prerequisite == null)
                return NotFound();

            // Check for an existing relationship
            bool alreadyExists = await _context.CoursePrerequisites
                .AnyAsync(cp =>
                    cp.CourseID == courseId &&
                    cp.PrerequisiteCourseID == prerequisiteCourseId);

            if (alreadyExists)
            {
                TempData["ErrorMessage"] =
                    "This prerequisite already exists.";

                return RedirectToAction(
                    nameof(ManagePrerequisites), new { id = courseId });
            }

            // Check whether this would create a direct circular prerequisite
            bool createsCircularPrerequisite =
                await _context.CoursePrerequisites.AnyAsync(cp =>
                    cp.CourseID == prerequisiteCourseId &&
                    cp.PrerequisiteCourseID == courseId);

            if (createsCircularPrerequisite)
            {
                TempData["ErrorMessage"] =
                    "Cannot add this prerequisite because it would create a circular relationship.";

                return RedirectToAction(
                    nameof(ManagePrerequisites), new { id = courseId });
            }

            // Add the prerequisite relationship
            var coursePrerequisite = new CoursePrerequisite
            {
                CourseID = courseId,
                PrerequisiteCourseID = prerequisiteCourseId
            };

            _context.CoursePrerequisites.Add(coursePrerequisite);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Prerequisite added successfully.";

            return RedirectToAction(
                nameof(ManagePrerequisites), new { id = courseId });
        }

        //Remove Prequisite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePrerequisite(
            int courseId,
            int prerequisiteCourseId)
        {
            var prerequisite = await _context.CoursePrerequisites
                .FindAsync(courseId, prerequisiteCourseId);

            if (prerequisite == null)
                return NotFound();

            _context.CoursePrerequisites.Remove(prerequisite);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(ManagePrerequisites),
                new { id = courseId });
        }
    }
}
