using Microsoft.AspNetCore.Mvc;

using MySqlConnector;

using APULMS.Models;



namespace APULMS.Controllers

{

    public class AdminController : Controller

    {

        private readonly DatabaseHelper _db;



        public AdminController(DatabaseHelper db)

        {

            _db = db;

        }



        // =========================================================

        // DASHBOARD

        // =========================================================

        public IActionResult Dashboard()

        {

            if (!IsAdmin())

                return RedirectToAction("Login", "Account");



            SetUserInfo();



            ViewBag.DashboardTotalUsers = 0;

            ViewBag.DashboardTotalCourses = 0;

            ViewBag.DashboardTotalLecturers = 0;

            ViewBag.DashboardTotalStudents = 0;

            ViewBag.DashboardTotalAdmins = 0;



            try

            {

                using var connection = _db.GetConnection();

                connection.Open();



                const string sql = @"
                    SELECT
                        (SELECT COUNT(*) FROM users) AS total_users,
                        (SELECT COUNT(*) FROM courses) AS total_courses,
                        (SELECT COUNT(*) FROM users WHERE role = 'lecturer') AS total_lecturers,
                        (SELECT COUNT(*) FROM users WHERE role = 'student') AS total_students,
                        (SELECT COUNT(*) FROM users WHERE role = 'admin') AS total_admins";



                using var command = new MySqlCommand(sql, connection);

                using var reader = command.ExecuteReader();



                if (reader.Read())

                {

                    ViewBag.DashboardTotalUsers =
                        Convert.ToInt32(reader["total_users"]);



                    ViewBag.DashboardTotalCourses =
                        Convert.ToInt32(reader["total_courses"]);



                    ViewBag.DashboardTotalLecturers =
                        Convert.ToInt32(reader["total_lecturers"]);



                    ViewBag.DashboardTotalStudents =
                        Convert.ToInt32(reader["total_students"]);



                    ViewBag.DashboardTotalAdmins =
                        Convert.ToInt32(reader["total_admins"]);

                }

            }

            catch (MySqlException ex)

            {

                TempData["Error"] =
                    "Unable to load dashboard statistics: " + ex.Message;

            }



            return View();

        }



        // =========================================================

        // USERS

        // =========================================================

        public IActionResult Users()

        {

            if (!IsAdmin())

                return RedirectToAction("Login", "Account");



            SetUserInfo();



            var users = new List<User>();



            try

            {

                using var connection = _db.GetConnection();

                connection.Open();



                const string sql = @"

                    SELECT

                        id,

                        name,

                        email,

                        role,

                        created_at

                    FROM users

                    ORDER BY id DESC";



                using var command = new MySqlCommand(sql, connection);

                using var reader = command.ExecuteReader();



                while (reader.Read())

                {

                    users.Add(new User

                    {

                        Id = reader.GetInt32("id"),

                        Name = reader.IsDBNull(reader.GetOrdinal("name"))

                            ? string.Empty

                            : reader.GetString("name"),

                        Email = reader.IsDBNull(reader.GetOrdinal("email"))

                            ? string.Empty

                            : reader.GetString("email"),

                        Role = reader.IsDBNull(reader.GetOrdinal("role"))

                            ? string.Empty

                            : reader.GetString("role"),

                        CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at"))

                            ? DateTime.MinValue

                            : reader.GetDateTime("created_at")

                    });

                }

            }

            catch (MySqlException ex)

            {

                TempData["Error"] = "Unable to load users: " + ex.Message;

            }



            ViewBag.TotalUsers = users.Count;



            ViewBag.TotalStudents =

                users.Count(u =>

                    string.Equals(

                        u.Role,

                        "student",

                        StringComparison.OrdinalIgnoreCase));



            ViewBag.TotalLecturers =

                users.Count(u =>

                    string.Equals(

                        u.Role,

                        "lecturer",

                        StringComparison.OrdinalIgnoreCase));



            ViewBag.TotalAdmins =

                users.Count(u =>

                    string.Equals(

                        u.Role,

                        "admin",

                        StringComparison.OrdinalIgnoreCase));



            return View(users);

        }



        // =========================================================

        // EDIT USER - GET

        // =========================================================

        [HttpGet]

        public IActionResult EditUser(int id)

        {

            if (!IsAdmin())

                return RedirectToAction("Login", "Account");



            SetUserInfo();



            var user = GetUser(id);



            if (user == null)

            {

                TempData["Error"] = "The selected user could not be found.";

                return RedirectToAction(nameof(Users));

            }



            // Password is intentionally not loaded.

            // Editing a user must never overwrite the existing password.

            user.Password = string.Empty;

            user.ConfirmPassword = string.Empty;



            return View(user);

        }



        // =========================================================

        // EDIT USER - POST

        // =========================================================

        [HttpPost]

        [ValidateAntiForgeryToken]

        public IActionResult EditUser(User user)

        {

            if (!IsAdmin())

                return RedirectToAction("Login", "Account");



            SetUserInfo();



            user.Name = user.Name?.Trim() ?? string.Empty;

            user.Email = user.Email?.Trim() ?? string.Empty;

            user.Role = user.Role?.Trim().ToLowerInvariant() ?? string.Empty;



            // Password fields are not part of Admin user editing.

            // Remove any validation errors that the User model may define

            // for these registration-only fields.

            ModelState.Remove(nameof(APULMS.Models.User.Password));

            ModelState.Remove(nameof(APULMS.Models.User.ConfirmPassword));



            if (user.Id <= 0)

                ModelState.AddModelError(string.Empty, "Invalid user account.");



            if (string.IsNullOrWhiteSpace(user.Name))

                ModelState.AddModelError(nameof(APULMS.Models.User.Name), "Name is required.");



            if (string.IsNullOrWhiteSpace(user.Email))

                ModelState.AddModelError(nameof(APULMS.Models.User.Email), "Email is required.");



            if (!IsValidUserRole(user.Role))

                ModelState.AddModelError(

                    nameof(APULMS.Models.User.Role),

                    "Please select Student, Lecturer, or Administrator.");



            if (!ModelState.IsValid)

                return View(user);



            try

            {

                using var connection = _db.GetConnection();

                connection.Open();



                // Confirm that the account still exists.

                const string existsSql = @"

                    SELECT COUNT(*)

                    FROM users

                    WHERE id = @id";



                using (var existsCommand =

                       new MySqlCommand(existsSql, connection))

                {

                    existsCommand.Parameters.AddWithValue("@id", user.Id);



                    int exists = Convert.ToInt32(

                        existsCommand.ExecuteScalar());



                    if (exists == 0)

                    {

                        TempData["Error"] =

                            "The selected user could not be found.";



                        return RedirectToAction(nameof(Users));

                    }

                }



                // Email must remain unique, excluding the account being edited.

                const string duplicateEmailSql = @"

                    SELECT COUNT(*)

                    FROM users

                    WHERE LOWER(email) = LOWER(@email)

                      AND id <> @id";



                using (var duplicateCommand =

                       new MySqlCommand(

                           duplicateEmailSql,

                           connection))

                {

                    duplicateCommand.Parameters.AddWithValue(

                        "@email",

                        user.Email);



                    duplicateCommand.Parameters.AddWithValue(

                        "@id",

                        user.Id);



                    int duplicateCount = Convert.ToInt32(

                        duplicateCommand.ExecuteScalar());



                    if (duplicateCount > 0)

                    {

                        ModelState.AddModelError(

                            nameof(APULMS.Models.User.Email),

                            "Another account already uses this email address.");



                        return View(user);

                    }

                }



                // Protect the currently logged-in administrator from

                // accidentally changing their own role and losing access.

                int? currentAdminId = GetCurrentUserId();



                if (currentAdminId.HasValue &&

                    currentAdminId.Value == user.Id &&

                    !user.Role.Equals(

                        "admin",

                        StringComparison.OrdinalIgnoreCase))

                {

                    ModelState.AddModelError(

                        nameof(APULMS.Models.User.Role),

                        "You cannot remove the administrator role from your currently logged-in account.");



                    return View(user);

                }



                const string updateSql = @"

                    UPDATE users

                    SET

                        name = @name,

                        email = @email,

                        role = @role

                    WHERE id = @id";



                using var updateCommand =

                    new MySqlCommand(updateSql, connection);



                updateCommand.Parameters.AddWithValue(

                    "@name",

                    user.Name);



                updateCommand.Parameters.AddWithValue(

                    "@email",

                    user.Email);



                updateCommand.Parameters.AddWithValue(

                    "@role",

                    user.Role);



                updateCommand.Parameters.AddWithValue(

                    "@id",

                    user.Id);



                int affectedRows =

                    updateCommand.ExecuteNonQuery();



                if (affectedRows == 0)

                {

                    TempData["Error"] =

                        "No user changes were saved.";



                    return RedirectToAction(nameof(Users));

                }



                // If the administrator edited their own name/email,

                // keep the active session information synchronized.

                if (currentAdminId.HasValue &&

                    currentAdminId.Value == user.Id)

                {

                    HttpContext.Session.SetString(

                        "UserName",

                        user.Name);



                    HttpContext.Session.SetString(

                        "UserEmail",

                        user.Email);



                    HttpContext.Session.SetString(

                        "Role",

                        user.Role);

                }



                TempData["Success"] =

                    "User updated successfully.";



                return RedirectToAction(nameof(Users));

            }

            catch (MySqlException ex)

            {

                ModelState.AddModelError(

                    string.Empty,

                    "Unable to update user: " + ex.Message);



                return View(user);

            }

        }



        // =========================================================

        // DELETE USER

        // =========================================================

        [HttpPost]

        [ValidateAntiForgeryToken]

        public IActionResult DeleteUser(int id)

        {

            if (!IsAdmin())

                return RedirectToAction("Login", "Account");



            int? currentAdminId = GetCurrentUserId();



            if (currentAdminId.HasValue &&

                currentAdminId.Value == id)

            {

                TempData["Error"] =

                    "You cannot delete your currently logged-in administrator account.";



                return RedirectToAction(nameof(Users));

            }



            try

            {

                using var connection = _db.GetConnection();

                connection.Open();



                const string userSql = @"

                    SELECT

                        id,

                        name,

                        role

                    FROM users

                    WHERE id = @id

                    LIMIT 1";



                string userName;

                string userRole;



                using (var userCommand =

                       new MySqlCommand(userSql, connection))

                {

                    userCommand.Parameters.AddWithValue(

                        "@id",

                        id);



                    using var reader =

                        userCommand.ExecuteReader();



                    if (!reader.Read())

                    {

                        TempData["Error"] =

                            "The selected user could not be found.";



                        return RedirectToAction(nameof(Users));

                    }



                    userName =

                        reader.IsDBNull(reader.GetOrdinal("name"))

                            ? "User"

                            : reader.GetString("name");



                    userRole =

                        reader.IsDBNull(reader.GetOrdinal("role"))

                            ? string.Empty

                            : reader.GetString("role");

                }



                // A lecturer may own courses. The database relationship

                // courses.lecturer_id -> users.id uses ON DELETE SET NULL,

                // so courses remain safely in the system and become

                // unassigned when that lecturer is removed.

                if (userRole.Equals(

                    "lecturer",

                    StringComparison.OrdinalIgnoreCase))

                {

                    const string courseCountSql = @"

                        SELECT COUNT(*)

                        FROM courses

                        WHERE lecturer_id = @lecturerId";



                    using var courseCountCommand =

                        new MySqlCommand(

                            courseCountSql,

                            connection);



                    courseCountCommand.Parameters.AddWithValue(

                        "@lecturerId",

                        id);



                    int assignedCourseCount =

                        Convert.ToInt32(

                            courseCountCommand.ExecuteScalar());



                    if (assignedCourseCount > 0)

                    {

                        TempData["Warning"] =

                            $"{userName} was assigned to {assignedCourseCount} course(s). Those courses will remain in APULMS and become unassigned.";

                    }

                }



                const string deleteSql = @"

                    DELETE FROM users

                    WHERE id = @id";



                using var deleteCommand =

                    new MySqlCommand(deleteSql, connection);



                deleteCommand.Parameters.AddWithValue(

                    "@id",

                    id);



                int affectedRows =

                    deleteCommand.ExecuteNonQuery();



                if (affectedRows == 0)

                {

                    TempData["Error"] =

                        "The selected user could not be deleted.";



                    return RedirectToAction(nameof(Users));

                }



                TempData["Success"] =

                    $"{userName} was deleted successfully.";

            }

            catch (MySqlException ex)

            {

                TempData["Error"] =

                    "Unable to delete user: " + ex.Message;

            }



            return RedirectToAction(nameof(Users));

        }



        // =========================================================

        // COURSES

        // =========================================================

        public IActionResult Courses()

        {

            if (!IsAdmin())

                return RedirectToAction("Login", "Account");



            SetUserInfo();



            var courses = new List<Course>();



            using var connection = _db.GetConnection();

            connection.Open();



            const string sql = @"

                SELECT

                    c.id,

                    c.title,

                    c.description,

                    c.category,

                    c.thumbnail,

                    c.video_url,

                    c.lecturer_id,

                    c.created_at,

                    COALESCE(u.name, 'Unassigned') AS lecturer_name,

                    (

                        SELECT COUNT(*)

                        FROM enrollments e

                        WHERE e.course_id = c.id

                    ) AS student_count

                FROM courses c

                LEFT JOIN users u

                    ON c.lecturer_id = u.id

                ORDER BY c.id DESC";



            using var command = new MySqlCommand(sql, connection);

            using var reader = command.ExecuteReader();



            while (reader.Read())

            {

                courses.Add(new Course

                {

                    Id = reader.GetInt32("id"),



                    Title = reader.IsDBNull(reader.GetOrdinal("title"))

                        ? string.Empty

                        : reader.GetString("title"),



                    Description = reader.IsDBNull(reader.GetOrdinal("description"))

                        ? string.Empty

                        : reader.GetString("description"),



                    Category = reader.IsDBNull(reader.GetOrdinal("category"))

                        ? string.Empty

                        : reader.GetString("category"),



                    Thumbnail = reader.IsDBNull(reader.GetOrdinal("thumbnail"))

                        ? string.Empty

                        : reader.GetString("thumbnail"),



                    VideoUrl = reader.IsDBNull(reader.GetOrdinal("video_url"))

                        ? string.Empty

                        : reader.GetString("video_url"),



                    LecturerId = reader.IsDBNull(reader.GetOrdinal("lecturer_id"))

                        ? null

                        : reader.GetInt32("lecturer_id"),



                    LecturerName = reader.GetString("lecturer_name"),



                    StudentCount = Convert.ToInt32(

                        reader["student_count"]

                    ),



                    CreatedAt = reader.GetDateTime("created_at")

                });

            }



            ViewBag.TotalCourses = courses.Count;



            ViewBag.TotalStudents =

                GetTotalEnrollments();



            ViewBag.TotalLecturers =

                GetLecturerCount();



            ViewBag.ActiveCourses =

                courses.Count;



            return View(courses);

        }



        // =========================================================

        // CREATE COURSE - GET

        // =========================================================

        [HttpGet]

        public IActionResult CreateCourse()

        {

            if (!IsAdmin())

                return RedirectToAction("Login", "Account");



            SetUserInfo();



            ViewBag.Lecturers = GetLecturers();



            return View(new Course());

        }



        // =========================================================

        // CREATE COURSE - POST

        // =========================================================

        [HttpPost]

        [ValidateAntiForgeryToken]

        public IActionResult CreateCourse(Course course)

        {

            if (!IsAdmin())

                return RedirectToAction("Login", "Account");



            if (!ModelState.IsValid)

            {

                SetUserInfo();



                ViewBag.Lecturers =

                    GetLecturers();



                return View(course);

            }



            try

            {

                using var connection = _db.GetConnection();

                connection.Open();



                const string sql = @"

                    INSERT INTO courses

                    (

                        title,

                        description,

                        category,

                        thumbnail,

                        video_url,

                        lecturer_id

                    )

                    VALUES

                    (

                        @title,

                        @description,

                        @category,

                        @thumbnail,

                        @video_url,

                        @lecturer_id

                    )";



                using var command =

                    new MySqlCommand(sql, connection);



                command.Parameters.AddWithValue(

                    "@title",

                    course.Title

                );



                command.Parameters.AddWithValue(

                    "@description",

                    string.IsNullOrWhiteSpace(course.Description)

                        ? DBNull.Value

                        : course.Description

                );



                command.Parameters.AddWithValue(

                    "@category",

                    string.IsNullOrWhiteSpace(course.Category)

                        ? DBNull.Value

                        : course.Category

                );



                command.Parameters.AddWithValue(

                    "@thumbnail",

                    string.IsNullOrWhiteSpace(course.Thumbnail)

                        ? DBNull.Value

                        : course.Thumbnail

                );



                command.Parameters.AddWithValue(

                    "@video_url",

                    string.IsNullOrWhiteSpace(course.VideoUrl)

                        ? DBNull.Value

                        : course.VideoUrl

                );



                command.Parameters.AddWithValue(

                    "@lecturer_id",

                    course.LecturerId.HasValue

                        ? course.LecturerId.Value

                        : DBNull.Value

                );



                command.ExecuteNonQuery();



                TempData["Success"] =

                    "Course created successfully.";



                return RedirectToAction(nameof(Courses));

            }

            catch (MySqlException ex)

            {

                TempData["Error"] =

                    "Unable to create course: " + ex.Message;



                SetUserInfo();



                ViewBag.Lecturers =

                    GetLecturers();



                return View(course);

            }

        }



        // =========================================================

        // EDIT COURSE - GET

        // =========================================================

        [HttpGet]

        public IActionResult EditCourse(int id)

        {

            if (!IsAdmin())

                return RedirectToAction("Login", "Account");



            SetUserInfo();



            var course =

                GetCourse(id);



            if (course == null)

                return NotFound();



            ViewBag.Lecturers =

                GetLecturers();



            return View(course);

        }



        // =========================================================

        // EDIT COURSE - POST

        // =========================================================

        [HttpPost]

        [ValidateAntiForgeryToken]

        public IActionResult EditCourse(Course course)

        {

            if (!IsAdmin())

                return RedirectToAction("Login", "Account");



            if (!ModelState.IsValid)

            {

                SetUserInfo();



                ViewBag.Lecturers =

                    GetLecturers();



                return View(course);

            }



            try

            {

                using var connection = _db.GetConnection();

                connection.Open();



                const string sql = @"

                    UPDATE courses

                    SET

                        title = @title,

                        description = @description,

                        category = @category,

                        thumbnail = @thumbnail,

                        video_url = @video_url,

                        lecturer_id = @lecturer_id

                    WHERE id = @id";



                using var command =

                    new MySqlCommand(sql, connection);



                command.Parameters.AddWithValue(

                    "@id",

                    course.Id

                );



                command.Parameters.AddWithValue(

                    "@title",

                    course.Title

                );



                command.Parameters.AddWithValue(

                    "@description",

                    string.IsNullOrWhiteSpace(course.Description)

                        ? DBNull.Value

                        : course.Description

                );



                command.Parameters.AddWithValue(

                    "@category",

                    string.IsNullOrWhiteSpace(course.Category)

                        ? DBNull.Value

                        : course.Category

                );



                command.Parameters.AddWithValue(

                    "@thumbnail",

                    string.IsNullOrWhiteSpace(course.Thumbnail)

                        ? DBNull.Value

                        : course.Thumbnail

                );



                command.Parameters.AddWithValue(

                    "@video_url",

                    string.IsNullOrWhiteSpace(course.VideoUrl)

                        ? DBNull.Value

                        : course.VideoUrl

                );



                command.Parameters.AddWithValue(

                    "@lecturer_id",

                    course.LecturerId.HasValue

                        ? course.LecturerId.Value

                        : DBNull.Value

                );



                command.ExecuteNonQuery();



                TempData["Success"] =

                    "Course updated successfully.";



                return RedirectToAction(nameof(Courses));

            }

            catch (MySqlException ex)

            {

                TempData["Error"] =

                    "Unable to update course: " + ex.Message;



                SetUserInfo();



                ViewBag.Lecturers =

                    GetLecturers();



                return View(course);

            }

        }



        // =========================================================

        // DELETE COURSE

        // =========================================================

        [HttpPost]

        [ValidateAntiForgeryToken]

        public IActionResult DeleteCourse(int id)

        {

            if (!IsAdmin())

                return RedirectToAction("Login", "Account");



            try

            {

                using var connection = _db.GetConnection();

                connection.Open();



                const string sql =

                    "DELETE FROM courses WHERE id = @id";



                using var command =

                    new MySqlCommand(sql, connection);



                command.Parameters.AddWithValue(

                    "@id",

                    id

                );



                command.ExecuteNonQuery();



                TempData["Success"] =

                    "Course deleted successfully.";

            }

            catch (MySqlException ex)

            {

                TempData["Error"] =

                    "Unable to delete course: " + ex.Message;

            }



            return RedirectToAction(nameof(Courses));

        }

        // =========================================================
        // LECTURERS
        // =========================================================
        public IActionResult Lecturers()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            SetUserInfo();

            var lecturers = new List<AdminLecturerCourse>();

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string sql = @"
                    SELECT
                        u.id AS lecturer_id,
                        u.name AS lecturer_name,
                        u.email AS lecturer_email,
                        c.id AS course_id,
                        c.title AS course_title,
                        COUNT(DISTINCT e.user_id) AS student_count
                    FROM users u
                    LEFT JOIN courses c
                        ON c.lecturer_id = u.id
                    LEFT JOIN enrollments e
                        ON e.course_id = c.id
                    WHERE u.role = 'lecturer'
                    GROUP BY
                        u.id,
                        u.name,
                        u.email,
                        c.id,
                        c.title
                    ORDER BY
                        u.name,
                        c.title";

                using var command = new MySqlCommand(sql, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    lecturers.Add(new AdminLecturerCourse
                    {
                        LecturerId = reader.GetInt32("lecturer_id"),
                        LecturerName = reader.IsDBNull(reader.GetOrdinal("lecturer_name"))
                            ? string.Empty
                            : reader.GetString("lecturer_name"),
                        LecturerEmail = reader.IsDBNull(reader.GetOrdinal("lecturer_email"))
                            ? string.Empty
                            : reader.GetString("lecturer_email"),
                        CourseId = reader.IsDBNull(reader.GetOrdinal("course_id"))
                            ? null
                            : reader.GetInt32("course_id"),
                        CourseTitle = reader.IsDBNull(reader.GetOrdinal("course_title"))
                            ? "No assigned course"
                            : reader.GetString("course_title"),
                        StudentCount = reader.IsDBNull(reader.GetOrdinal("student_count"))
                            ? 0
                            : Convert.ToInt32(reader["student_count"])
                    });
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] =
                    "Unable to load lecturer information: " + ex.Message;
            }

            var assignedRows =
                lecturers.Where(l => l.CourseId.HasValue).ToList();

            ViewBag.TotalLecturers =
                lecturers.Select(l => l.LecturerId).Distinct().Count();

            ViewBag.AssignedCourses =
                assignedRows.Select(l => l.CourseId!.Value).Distinct().Count();

            ViewBag.StudentsTaught =
                assignedRows.Sum(l => l.StudentCount);

            ViewBag.UnassignedLecturers =
                lecturers
                    .Where(l => !l.CourseId.HasValue)
                    .Select(l => l.LecturerId)
                    .Distinct()
                    .Count();

            return View(lecturers);
        }


        // =========================================================
        // STUDENTS
        // =========================================================
        public IActionResult Students()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            SetUserInfo();

            var students = new List<AdminStudentEnrollment>();

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string sql = @"
                    SELECT
                        u.id AS student_id,
                        u.name AS student_name,
                        u.email AS student_email,
                        e.id AS enrollment_id,
                        e.progress,
                        e.enrolled_at,
                        c.id AS course_id,
                        c.title AS course_title
                    FROM users u
                    LEFT JOIN enrollments e
                        ON e.user_id = u.id
                    LEFT JOIN courses c
                        ON c.id = e.course_id
                    WHERE u.role = 'student'
                    ORDER BY u.name, c.title";

                using var command = new MySqlCommand(sql, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    students.Add(new AdminStudentEnrollment
                    {
                        StudentId = reader.GetInt32("student_id"),
                        StudentName = reader.IsDBNull(reader.GetOrdinal("student_name"))
                            ? string.Empty
                            : reader.GetString("student_name"),
                        StudentEmail = reader.IsDBNull(reader.GetOrdinal("student_email"))
                            ? string.Empty
                            : reader.GetString("student_email"),
                        EnrollmentId = reader.IsDBNull(reader.GetOrdinal("enrollment_id"))
                            ? null
                            : reader.GetInt32("enrollment_id"),
                        CourseId = reader.IsDBNull(reader.GetOrdinal("course_id"))
                            ? null
                            : reader.GetInt32("course_id"),
                        CourseTitle = reader.IsDBNull(reader.GetOrdinal("course_title"))
                            ? "Not enrolled"
                            : reader.GetString("course_title"),
                        Progress = reader.IsDBNull(reader.GetOrdinal("progress"))
                            ? 0
                            : reader.GetInt32("progress"),
                        EnrolledAt = reader.IsDBNull(reader.GetOrdinal("enrolled_at"))
                            ? null
                            : reader.GetDateTime("enrolled_at")
                    });
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] =
                    "Unable to load student information: " + ex.Message;
            }

            var enrollmentRows =
                students.Where(s => s.EnrollmentId.HasValue).ToList();

            ViewBag.TotalStudents =
                students.Select(s => s.StudentId).Distinct().Count();

            ViewBag.EnrolledStudents =
                enrollmentRows.Select(s => s.StudentId).Distinct().Count();

            ViewBag.TotalEnrollments =
                enrollmentRows.Count;

            ViewBag.AverageProgress =
                enrollmentRows.Count == 0
                    ? 0
                    : (int)Math.Round(enrollmentRows.Average(s => s.Progress));

            return View(students);
        }

        // =========================================================
        // REPORTS
        // =========================================================
        public IActionResult Reports()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            SetUserInfo();

            ViewBag.ReportTotalUsers = 0;
            ViewBag.ReportTotalCourses = 0;
            ViewBag.ReportTotalLecturers = 0;
            ViewBag.ReportTotalStudents = 0;
            ViewBag.ReportTotalEnrollments = 0;
            ViewBag.ReportQuizAttempts = 0;

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string sql = @"
                    SELECT
                        (SELECT COUNT(*) FROM users) AS total_users,
                        (SELECT COUNT(*) FROM courses) AS total_courses,
                        (SELECT COUNT(*) FROM users WHERE role = 'lecturer') AS total_lecturers,
                        (SELECT COUNT(*) FROM users WHERE role = 'student') AS total_students,
                        (SELECT COUNT(*) FROM enrollments) AS total_enrollments,
                        (SELECT COUNT(*) FROM quiz_results) AS quiz_attempts";

                using var command = new MySqlCommand(sql, connection);
                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    ViewBag.ReportTotalUsers =
                        Convert.ToInt32(reader["total_users"]);

                    ViewBag.ReportTotalCourses =
                        Convert.ToInt32(reader["total_courses"]);

                    ViewBag.ReportTotalLecturers =
                        Convert.ToInt32(reader["total_lecturers"]);

                    ViewBag.ReportTotalStudents =
                        Convert.ToInt32(reader["total_students"]);

                    ViewBag.ReportTotalEnrollments =
                        Convert.ToInt32(reader["total_enrollments"]);

                    ViewBag.ReportQuizAttempts =
                        Convert.ToInt32(reader["quiz_attempts"]);
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] =
                    "Unable to load report information: " + ex.Message;
            }

            return View();
        }

        // =========================================================
        // GUEST INQUIRIES
        // =========================================================

        public IActionResult Inquiries()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            SetUserInfo();

            var inquiries = new List<ContactInquiry>();

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string sql = @"
                    SELECT
                        id,
                        name,
                        email,
                        subject,
                        message,
                        status,
                        admin_reply,
                        created_at,
                        replied_at
                    FROM contact_messages
                    ORDER BY created_at DESC, id DESC";

                using var command =
                    new MySqlCommand(sql, connection);

                using var reader =
                    command.ExecuteReader();

                while (reader.Read())
                {
                    inquiries.Add(new ContactInquiry
                    {
                        Id = reader.GetInt32("id"),

                        Name = reader.IsDBNull(
                            reader.GetOrdinal("name"))
                            ? string.Empty
                            : reader.GetString("name"),

                        Email = reader.IsDBNull(
                            reader.GetOrdinal("email"))
                            ? string.Empty
                            : reader.GetString("email"),

                        Subject = reader.IsDBNull(
                            reader.GetOrdinal("subject"))
                            ? string.Empty
                            : reader.GetString("subject"),

                        Message = reader.IsDBNull(
                            reader.GetOrdinal("message"))
                            ? string.Empty
                            : reader.GetString("message"),

                        Status = reader.IsDBNull(
                            reader.GetOrdinal("status"))
                            ? "new"
                            : reader.GetString("status"),

                        AdminReply = reader.IsDBNull(
                            reader.GetOrdinal("admin_reply"))
                            ? null
                            : reader.GetString("admin_reply"),

                        CreatedAt = reader.GetDateTime(
                            "created_at"),

                        RepliedAt = reader.IsDBNull(
                            reader.GetOrdinal("replied_at"))
                            ? null
                            : reader.GetDateTime("replied_at")
                    });
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] =
                    "Unable to load guest inquiries: " +
                    ex.Message;
            }

            ViewBag.TotalInquiries =
                inquiries.Count;

            ViewBag.NewInquiries =
                inquiries.Count(x =>
                    x.Status.Equals(
                        "new",
                        StringComparison.OrdinalIgnoreCase));

            ViewBag.ReadInquiries =
                inquiries.Count(x =>
                    x.Status.Equals(
                        "read",
                        StringComparison.OrdinalIgnoreCase));

            ViewBag.RepliedInquiries =
                inquiries.Count(x =>
                    x.Status.Equals(
                        "replied",
                        StringComparison.OrdinalIgnoreCase));

            return View(inquiries);
        }


        // =========================================================
        // VIEW SINGLE INQUIRY
        // =========================================================

        [HttpGet]
        public IActionResult InquiryDetails(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            SetUserInfo();

            ContactInquiry? inquiry = null;

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string sql = @"
                    SELECT
                        id,
                        name,
                        email,
                        subject,
                        message,
                        status,
                        admin_reply,
                        created_at,
                        replied_at
                    FROM contact_messages
                    WHERE id = @id
                    LIMIT 1";

                using (var command =
                    new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue(
                        "@id",
                        id);

                    using var reader =
                        command.ExecuteReader();

                    if (reader.Read())
                    {
                        inquiry = new ContactInquiry
                        {
                            Id = reader.GetInt32("id"),

                            Name = reader.GetString("name"),

                            Email = reader.GetString("email"),

                            Subject = reader.GetString("subject"),

                            Message = reader.GetString("message"),

                            Status = reader.GetString("status"),

                            AdminReply = reader.IsDBNull(
                                reader.GetOrdinal("admin_reply"))
                                ? null
                                : reader.GetString("admin_reply"),

                            CreatedAt =
                                reader.GetDateTime("created_at"),

                            RepliedAt = reader.IsDBNull(
                                reader.GetOrdinal("replied_at"))
                                ? null
                                : reader.GetDateTime("replied_at")
                        };
                    }
                }

                if (inquiry == null)
                {
                    TempData["Error"] =
                        "The selected inquiry could not be found.";

                    return RedirectToAction(
                        nameof(Inquiries));
                }

                // Opening a NEW inquiry marks it as READ.
                if (inquiry.Status.Equals(
                    "new",
                    StringComparison.OrdinalIgnoreCase))
                {
                    const string updateSql = @"
                        UPDATE contact_messages
                        SET status = 'read'
                        WHERE id = @id
                          AND status = 'new'";

                    using var updateCommand =
                        new MySqlCommand(
                            updateSql,
                            connection);

                    updateCommand.Parameters.AddWithValue(
                        "@id",
                        id);

                    updateCommand.ExecuteNonQuery();

                    inquiry.Status = "read";
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] =
                    "Unable to load the inquiry: " +
                    ex.Message;

                return RedirectToAction(
                    nameof(Inquiries));
            }

            return View(inquiry);
        }


        // =========================================================
        // SAVE ADMIN REPLY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReplyInquiry(
            int id,
            string adminReply)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (id <= 0)
            {
                TempData["Error"] =
                    "Invalid inquiry.";

                return RedirectToAction(
                    nameof(Inquiries));
            }

            adminReply =
                adminReply?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(adminReply))
            {
                TempData["InquiryError"] =
                    "Please enter a response before saving.";

                return RedirectToAction(
                    nameof(InquiryDetails),
                    new { id });
            }

            if (adminReply.Length > 5000)
            {
                TempData["InquiryError"] =
                    "The response is too long.";

                return RedirectToAction(
                    nameof(InquiryDetails),
                    new { id });
            }

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string sql = @"
                    UPDATE contact_messages
                    SET
                        admin_reply = @adminReply,
                        status = 'replied',
                        replied_at = CURRENT_TIMESTAMP
                    WHERE id = @id";

                using var command =
                    new MySqlCommand(sql, connection);

                command.Parameters.AddWithValue(
                    "@adminReply",
                    adminReply);

                command.Parameters.AddWithValue(
                    "@id",
                    id);

                int affectedRows =
                    command.ExecuteNonQuery();

                if (affectedRows == 0)
                {
                    TempData["Error"] =
                        "The inquiry could not be found.";

                    return RedirectToAction(
                        nameof(Inquiries));
                }

                TempData["InquirySuccess"] =
                    "Response saved successfully.";
            }
            catch (MySqlException ex)
            {
                TempData["InquiryError"] =
                    "Unable to save the response: " +
                    ex.Message;
            }

            return RedirectToAction(
                nameof(InquiryDetails),
                new { id });
        }


        // =========================================================
        // PROFILE
        // =========================================================
        public IActionResult Profile()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            SetUserInfo();

            string? sessionUserId =
                HttpContext.Session.GetString("UserId");

            if (!int.TryParse(sessionUserId, out int userId))
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string sql = @"
                    SELECT profile_image
                    FROM users
                    WHERE id = @userId
                      AND role = 'admin'
                    LIMIT 1";

                using var command =
                    new MySqlCommand(sql, connection);

                command.Parameters.AddWithValue(
                    "@userId",
                    userId
                );

                object? result = command.ExecuteScalar();

                ViewBag.ProfileImage =
                    result == null || result == DBNull.Value
                        ? null
                        : result.ToString();
            }
            catch (MySqlException ex)
            {
                TempData["ProfileError"] =
                    "Unable to load profile picture: " + ex.Message;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile? profilePicture
        )
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            string? sessionUserId =
                HttpContext.Session.GetString("UserId");

            if (!int.TryParse(sessionUserId, out int userId))
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            if (profilePicture == null ||
                profilePicture.Length == 0)
            {
                TempData["ProfileError"] =
                    "Please choose a profile picture.";

                return RedirectToAction(nameof(Profile));
            }

            const long maxFileSize =
                5 * 1024 * 1024;

            if (profilePicture.Length > maxFileSize)
            {
                TempData["ProfileError"] =
                    "Profile picture must be 5 MB or smaller.";

                return RedirectToAction(nameof(Profile));
            }

            string extension =
                Path.GetExtension(
                    profilePicture.FileName
                ).ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                TempData["ProfileError"] =
                    "Only JPG, JPEG, PNG or WEBP images are allowed.";

                return RedirectToAction(nameof(Profile));
            }

            string[] allowedContentTypes =
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedContentTypes.Contains(
                    profilePicture.ContentType.ToLowerInvariant()
                ))
            {
                TempData["ProfileError"] =
                    "The selected file is not a supported image.";

                return RedirectToAction(nameof(Profile));
            }

            string uploadsFolder =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "profiles"
                );

            Directory.CreateDirectory(
                uploadsFolder
            );

            string fileName =
                $"user_{userId}_{Guid.NewGuid():N}{extension}";

            string physicalPath =
                Path.Combine(
                    uploadsFolder,
                    fileName
                );

            string publicPath =
                $"/uploads/profiles/{fileName}";

            try
            {
                await using (
                    var stream =
                        new FileStream(
                            physicalPath,
                            FileMode.CreateNew
                        )
                )
                {
                    await profilePicture.CopyToAsync(
                        stream
                    );
                }

                string? oldProfileImage = null;

                using (
                    var connection =
                        _db.GetConnection()
                )
                {
                    connection.Open();

                    const string getOldSql = @"
                        SELECT profile_image
                        FROM users
                        WHERE id = @userId
                          AND role = 'admin'
                        LIMIT 1";

                    using (
                        var getOldCommand =
                            new MySqlCommand(
                                getOldSql,
                                connection
                            )
                    )
                    {
                        getOldCommand.Parameters.AddWithValue(
                            "@userId",
                            userId
                        );

                        object? oldResult =
                            getOldCommand.ExecuteScalar();

                        if (oldResult != null &&
                            oldResult != DBNull.Value)
                        {
                            oldProfileImage =
                                oldResult.ToString();
                        }
                    }

                    const string updateSql = @"
                        UPDATE users
                        SET profile_image = @profileImage
                        WHERE id = @userId
                          AND role = 'admin'";

                    using var updateCommand =
                        new MySqlCommand(
                            updateSql,
                            connection
                        );

                    updateCommand.Parameters.AddWithValue(
                        "@profileImage",
                        publicPath
                    );

                    updateCommand.Parameters.AddWithValue(
                        "@userId",
                        userId
                    );

                    int rowsAffected =
                        updateCommand.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        if (System.IO.File.Exists(physicalPath))
                        {
                            System.IO.File.Delete(
                                physicalPath
                            );
                        }

                        TempData["ProfileError"] =
                            "Administrator account could not be updated.";

                        return RedirectToAction(
                            nameof(Profile)
                        );
                    }
                }

                if (!string.IsNullOrWhiteSpace(
                        oldProfileImage
                    ) &&
                    oldProfileImage.StartsWith(
                        "/uploads/profiles/",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    string oldFileName =
                        Path.GetFileName(
                            oldProfileImage
                        );

                    string oldPhysicalPath =
                        Path.Combine(
                            uploadsFolder,
                            oldFileName
                        );

                    if (System.IO.File.Exists(
                            oldPhysicalPath
                        ))
                    {
                        System.IO.File.Delete(
                            oldPhysicalPath
                        );
                    }
                }

                TempData["ProfileSuccess"] =
                    "Profile picture updated successfully.";
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(
                        physicalPath
                    ))
                {
                    System.IO.File.Delete(
                        physicalPath
                    );
                }

                TempData["ProfileError"] =
                    "Unable to upload profile picture: " +
                    ex.Message;
            }

            return RedirectToAction(
                nameof(Profile)
            );
        }




        // =========================================================

        // GET USER

        // =========================================================

        private User? GetUser(int id)

        {

            using var connection =

                _db.GetConnection();



            connection.Open();



            const string sql = @"

                SELECT

                    id,

                    name,

                    email,

                    role,

                    created_at

                FROM users

                WHERE id = @id

                LIMIT 1";



            using var command =

                new MySqlCommand(sql, connection);



            command.Parameters.AddWithValue(

                "@id",

                id

            );



            using var reader =

                command.ExecuteReader();



            if (!reader.Read())

                return null;



            return new User

            {

                Id = reader.GetInt32("id"),



                Name = reader.IsDBNull(reader.GetOrdinal("name"))

                    ? string.Empty

                    : reader.GetString("name"),



                Email = reader.IsDBNull(reader.GetOrdinal("email"))

                    ? string.Empty

                    : reader.GetString("email"),



                Role = reader.IsDBNull(reader.GetOrdinal("role"))

                    ? string.Empty

                    : reader.GetString("role"),



                CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at"))

                    ? DateTime.MinValue

                    : reader.GetDateTime("created_at")

            };

        }



        // =========================================================

        // VALID USER ROLE

        // =========================================================

        private static bool IsValidUserRole(string? role)

        {

            return string.Equals(

                       role,

                       "student",

                       StringComparison.OrdinalIgnoreCase)

                   ||

                   string.Equals(

                       role,

                       "lecturer",

                       StringComparison.OrdinalIgnoreCase)

                   ||

                   string.Equals(

                       role,

                       "admin",

                       StringComparison.OrdinalIgnoreCase);

        }



        // =========================================================

        // CURRENT USER ID

        // =========================================================

        private int? GetCurrentUserId()

        {

            string? userId =

                HttpContext.Session.GetString("UserId");



            if (int.TryParse(userId, out int id))

                return id;



            return null;

        }



        // =========================================================

        // GET COURSE

        // =========================================================

        private Course? GetCourse(int id)

        {

            using var connection =

                _db.GetConnection();



            connection.Open();



            const string sql = @"

                SELECT

                    id,

                    title,

                    description,

                    category,

                    thumbnail,

                    video_url,

                    lecturer_id,

                    created_at

                FROM courses

                WHERE id = @id";



            using var command =

                new MySqlCommand(sql, connection);



            command.Parameters.AddWithValue(

                "@id",

                id

            );



            using var reader =

                command.ExecuteReader();



            if (!reader.Read())

                return null;



            return new Course

            {

                Id = reader.GetInt32("id"),



                Title = reader.IsDBNull(reader.GetOrdinal("title"))

                    ? string.Empty

                    : reader.GetString("title"),



                Description = reader.IsDBNull(reader.GetOrdinal("description"))

                    ? string.Empty

                    : reader.GetString("description"),



                Category = reader.IsDBNull(reader.GetOrdinal("category"))

                    ? string.Empty

                    : reader.GetString("category"),



                Thumbnail = reader.IsDBNull(reader.GetOrdinal("thumbnail"))

                    ? string.Empty

                    : reader.GetString("thumbnail"),



                VideoUrl = reader.IsDBNull(reader.GetOrdinal("video_url"))

                    ? string.Empty

                    : reader.GetString("video_url"),



                LecturerId = reader.IsDBNull(reader.GetOrdinal("lecturer_id"))

                    ? null

                    : reader.GetInt32("lecturer_id"),



                CreatedAt = reader.GetDateTime("created_at")

            };

        }



        // =========================================================

        // GET LECTURERS

        // =========================================================

        private List<User> GetLecturers()

        {

            var lecturers =

                new List<User>();



            using var connection =

                _db.GetConnection();



            connection.Open();



            const string sql = @"

                SELECT

                    id,

                    name,

                    email

                FROM users

                WHERE role = 'lecturer'

                ORDER BY name";



            using var command =

                new MySqlCommand(sql, connection);



            using var reader =

                command.ExecuteReader();



            while (reader.Read())

            {

                lecturers.Add(new User

                {

                    Id = reader.GetInt32("id"),



                    Name = reader.IsDBNull(reader.GetOrdinal("name"))

                        ? string.Empty

                        : reader.GetString("name"),



                    Email = reader.IsDBNull(reader.GetOrdinal("email"))

                        ? string.Empty

                        : reader.GetString("email"),



                    Role = "lecturer"

                });

            }



            return lecturers;

        }



        // =========================================================

        // TOTAL ENROLLMENTS

        // =========================================================

        private int GetTotalEnrollments()

        {

            using var connection =

                _db.GetConnection();



            connection.Open();



            const string sql =

                "SELECT COUNT(*) FROM enrollments";



            using var command =

                new MySqlCommand(sql, connection);



            return Convert.ToInt32(

                command.ExecuteScalar()

            );

        }



        // =========================================================

        // TOTAL LECTURERS

        // =========================================================

        private int GetLecturerCount()

        {

            using var connection =

                _db.GetConnection();



            connection.Open();



            const string sql = @"

                SELECT COUNT(*)

                FROM users

                WHERE role = 'lecturer'";



            using var command =

                new MySqlCommand(sql, connection);



            return Convert.ToInt32(

                command.ExecuteScalar()

            );

        }



        // =========================================================

        // ADMIN CHECK

        // =========================================================

        private bool IsAdmin()

        {

            string? userId =

                HttpContext.Session.GetString("UserId");



            string? role =

                HttpContext.Session.GetString("Role");



            return userId != null &&

                   role == "admin";

        }

        // =========================================================
        // USER INFORMATION
        // =========================================================
        private void SetUserInfo()
        {
            ViewBag.Name =
                HttpContext.Session.GetString("UserName");

            ViewBag.Email =
                HttpContext.Session.GetString("UserEmail");

            ViewBag.Role =
                HttpContext.Session.GetString("Role");

            ViewBag.ProfileImage = null;

            string? sessionUserId =
                HttpContext.Session.GetString("UserId");

            if (!int.TryParse(sessionUserId, out int userId))
                return;

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string sql = @"
                    SELECT profile_image
                    FROM users
                    WHERE id = @userId
                      AND role = 'admin'
                    LIMIT 1";

                using var command =
                    new MySqlCommand(sql, connection);

                command.Parameters.AddWithValue(
                    "@userId",
                    userId
                );

                object? result =
                    command.ExecuteScalar();

                if (result != null &&
                    result != DBNull.Value)
                {
                    ViewBag.ProfileImage =
                        result.ToString();
                }
            }
            catch (MySqlException)
            {
                // Keep the portal usable even if the profile image
                // cannot be loaded. The layout will show the initial.
                ViewBag.ProfileImage = null;
            }
        }

    }


    public class AdminStudentEnrollment
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public int? EnrollmentId { get; set; }
        public int? CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public int Progress { get; set; }
        public DateTime? EnrolledAt { get; set; }
    }


    public class AdminLecturerCourse
    {
        public int LecturerId { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public string LecturerEmail { get; set; } = string.Empty;
        public int? CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }

}
