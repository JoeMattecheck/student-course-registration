using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Linq;

namespace WebApp.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Student/Index
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .Where(s => s.ApplicationUserId != null)
                .ToListAsync();

            var filteredStudents = new List<Student>();

            foreach (var student in students)
            {
                var user = await _userManager.FindByIdAsync(student.ApplicationUserId!);

                if (user != null && await _userManager.IsInRoleAsync(user, "Student"))
                {
                    filteredStudents.Add(student);
                }
            }

            return View(filteredStudents);
        }

        // GET: /Student/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentID == id);

            if (student == null)
                return NotFound();

            return View(student);
        }

        // GET: /Student/CurrentCourses/5
        public async Task<IActionResult> CurrentCourses(
            int id,
            string? searchString,
            string sortBy = "semester",
            bool descending = false)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentID == id);

            if (student == null)
                return NotFound();

            var courses = _context.Enrollments
                .Where(e => e.StudentID == id
                && e.Status == "Approved"
                && !_context.CompletedCourses.Any(cc =>
                cc.StudentID == e.StudentID &&
                cc.CourseID == e.CourseID))
                .Include(e => e.Course)
                .ThenInclude(c => c.Semester)
                .AsQueryable();

            // Search by course name, course number, or course ID
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                courses = courses.Where(e =>
                    e.Course.CourseName.Contains(searchString) ||
                    e.Course.CourseNumber.Contains(searchString));

                if (int.TryParse(searchString, out int courseId))
                {
                    courses = courses.Where(e =>
                        e.Course.CourseName.Contains(searchString) ||
                        e.Course.CourseNumber.Contains(searchString) ||
                        e.Course.CourseID == courseId);
                }
            }

            // Sorting
            switch (sortBy.ToLower())
            {
                case "name":
                    courses = descending
                        ? courses.OrderByDescending(e => e.Course.CourseName)
                        : courses.OrderBy(e => e.Course.CourseName);
                    break;

                case "id":
                    courses = descending
                        ? courses.OrderByDescending(e => e.Course.CourseID)
                        : courses.OrderBy(e => e.Course.CourseID);
                    break;

                default:
                    if (descending)
                    {
                        courses = courses
                            .OrderByDescending(e => e.Course.Semester.Year)
                            .ThenByDescending(e =>
                                e.Course.Semester.SemesterName == "Spring" ? 1 :
                                e.Course.Semester.SemesterName == "Summer" ? 2 :
                                e.Course.Semester.SemesterName == "Fall" ? 3 : 4);
                    }
                    else
                    {
                        courses = courses
                            .OrderBy(e => e.Course.Semester.Year)
                            .ThenBy(e =>
                                e.Course.Semester.SemesterName == "Spring" ? 1 :
                                e.Course.Semester.SemesterName == "Summer" ? 2 :
                                e.Course.Semester.SemesterName == "Fall" ? 3 : 4);
                    }
                    break;
            }

            ViewBag.Student = student;
            ViewBag.SearchString = searchString;
            ViewBag.SortBy = sortBy;
            ViewBag.Descending = descending;

            return View(await courses.ToListAsync());
        }


        // GET: /Student/CompletedCourses/5
        public async Task<IActionResult> CompletedCourses(
            int id,
            string? searchString,
            string sortBy = "semester",
            bool descending = false)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentID == id);

            if (student == null)
                return NotFound();

            var courses = _context.Enrollments
                .Where(e => e.StudentID == id
                && e.Status == "Approved"
                && !_context.CompletedCourses.Any(cc =>
                cc.StudentID == e.StudentID &&
                cc.CourseID == e.CourseID))
                .Include(e => e.Course)
                .ThenInclude(c => c.Semester)
            .AsQueryable();

            // Search by course name, course number, or course ID
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                courses = courses.Where(c =>
                    c.Course.CourseName.Contains(searchString) ||
                    c.Course.CourseNumber.Contains(searchString));

                if (int.TryParse(searchString, out int courseId))
                {
                    courses = courses.Where(c =>
                        c.Course.CourseName.Contains(searchString) ||
                        c.Course.CourseNumber.Contains(searchString) ||
                        c.Course.CourseID == courseId);
                }
            }

            // Sorting
            switch (sortBy.ToLower())
            {
                case "name":
                    courses = descending
                        ? courses.OrderByDescending(c => c.Course.CourseName)
                        : courses.OrderBy(c => c.Course.CourseName);
                    break;

                case "id":
                    courses = descending
                        ? courses.OrderByDescending(c => c.Course.CourseID)
                        : courses.OrderBy(c => c.Course.CourseID);
                    break;

                default:
                    if (descending)
                    {
                        courses = courses
                            .OrderByDescending(c => c.Course.Semester.Year)
                            .ThenByDescending(c =>
                                c.Course.Semester.SemesterName == "Spring" ? 1 :
                                c.Course.Semester.SemesterName == "Summer" ? 2 :
                                c.Course.Semester.SemesterName == "Fall" ? 3 : 4);
                    }
                    else
                    {
                        courses = courses
                            .OrderBy(c => c.Course.Semester.Year)
                            .ThenBy(c =>
                                c.Course.Semester.SemesterName == "Spring" ? 1 :
                                c.Course.Semester.SemesterName == "Summer" ? 2 :
                                c.Course.Semester.SemesterName == "Fall" ? 3 : 4);
                    }
                    break;
            }

            ViewBag.Student = student;
            ViewBag.SearchString = searchString;
            ViewBag.SortBy = sortBy;
            ViewBag.Descending = descending;

            return View(await courses.ToListAsync());
        }


        // GET: /Student/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Student/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            if (ModelState.IsValid)
            {
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(student);
        }
        // GET: /Student/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var student = await _context.Students.FindAsync(id);

            if (student == null)
                return NotFound();

            return View(student);
        }

        // POST: /Student/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.StudentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingStudent = await _context.Students
                    .FindAsync(id);

                if (existingStudent == null)
                {
                    return NotFound();
                }

                existingStudent.FirstName = student.FirstName;
                existingStudent.LastName = student.LastName;
                existingStudent.Email = student.Email;
                existingStudent.UserName = student.UserName;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(student);
        }
        // GET: Student/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentID == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }
        // POST: Student/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student != null)
            {
                _context.Students.Remove(student);
                
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
