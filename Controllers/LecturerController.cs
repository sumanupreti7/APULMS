using Microsoft.AspNetCore.Mvc;

using MySqlConnector;

using APULMS.Models;

namespace APULMS.Controllers

{

    public class LecturerController : Controller

    {

        private readonly DatabaseHelper _db;

        public LecturerController(DatabaseHelper db)

        {

            _db = db;

        }

        // =========================================================

        // CHECK LECTURER SESSION

        // =========================================================

        private bool IsLecturer()

        {

            return HttpContext.Session.GetString("UserId") != null &&

                   HttpContext.Session.GetString("Role") == "lecturer";

        }

        // =========================================================

        // REDIRECT TO LOGIN

        // =========================================================

        private IActionResult LoginRedirect()

        {

            return RedirectToAction("Login", "Account");

        }

        // =========================================================

        // LOAD LOGGED-IN LECTURER INFORMATION

        // =========================================================

        private void LoadUser()
        {
            ViewBag.Name =
                HttpContext.Session.GetString("UserName") ?? "Lecturer";

            ViewBag.Email =
                HttpContext.Session.GetString("UserEmail") ?? "";

            ViewBag.Role =
                HttpContext.Session.GetString("Role") ?? "lecturer";

            ViewBag.ProfileImage = null;

            int lecturerId = GetLecturerId();

            if (lecturerId <= 0)
                return;

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string sql = @"
                    SELECT profile_image
                    FROM users
                    WHERE id = @lecturerId
                      AND role = 'lecturer'
                    LIMIT 1";

                using var command =
                    new MySqlCommand(sql, connection);

                command.Parameters.AddWithValue(
                    "@lecturerId",
                    lecturerId
                );

                object? result =
                    command.ExecuteScalar();

                if (result != null &&
                    result != DBNull.Value)
                {
                    var profileImage =
                        result.ToString() ?? "";

                    ViewBag.ProfileImage =
                        profileImage;

                    HttpContext.Session.SetString(
                        "ProfileImage",
                        profileImage
                    );
                }
                else
                {
                    ViewBag.ProfileImage = null;
                    HttpContext.Session.Remove("ProfileImage");
                }
            }
            catch (MySqlException)
            {
                // Keep the Lecturer portal usable if the image cannot be loaded.
                ViewBag.ProfileImage = null;
            }
        }

        // =========================================================

        // GET CURRENT LECTURER ID

        // =========================================================

        private int GetLecturerId()

        {

            string? userId =

                HttpContext.Session.GetString("UserId");

            if (int.TryParse(userId, out int lecturerId))

            {

                return lecturerId;

            }

            return 0;

        }

        // =========================================================

        // DASHBOARD

        // =========================================================

        public IActionResult Dashboard()

        {

            if (!IsLecturer())

            {

                return LoginRedirect();

            }

            LoadUser();

            int lecturerId = GetLecturerId();

            if (lecturerId <= 0)

            {

                HttpContext.Session.Clear();

                return LoginRedirect();

            }

            try

            {

                using var connection = _db.GetConnection();

                connection.Open();

                // =================================================

                // TOTAL COURSES

                // =================================================

                const string courseCountSql = @"

                    SELECT COUNT(*)

                    FROM courses

                    WHERE lecturer_id = @lecturerId";

                using (var courseCommand =

                    new MySqlCommand(courseCountSql, connection))

                {

                    courseCommand.Parameters.AddWithValue(

                        "@lecturerId",

                        lecturerId

                    );

                    ViewBag.TotalCourses =

                        Convert.ToInt32(

                            courseCommand.ExecuteScalar()

                        );

                }

                // =================================================

                // TOTAL UNIQUE STUDENTS

                // =================================================

                const string studentCountSql = @"

                    SELECT COUNT(DISTINCT e.user_id)

                    FROM enrollments e

                    INNER JOIN courses c

                        ON e.course_id = c.id

                    WHERE c.lecturer_id = @lecturerId";

                using (var studentCommand =

                    new MySqlCommand(studentCountSql, connection))

                {

                    studentCommand.Parameters.AddWithValue(

                        "@lecturerId",

                        lecturerId

                    );

                    ViewBag.TotalStudents =

                        Convert.ToInt32(

                            studentCommand.ExecuteScalar()

                        );

                }

                // =================================================

                // LOAD LECTURER COURSES

                // =================================================

                var courses = new List<Course>();

                const string coursesSql = @"

                    SELECT

                        c.id,

                        c.title,

                        c.description,

                        c.category,

                        c.thumbnail,

                        c.video_url,

                        c.lecturer_id,

                        c.created_at,

                        COUNT(DISTINCT e.user_id) AS student_count

                    FROM courses c

                    LEFT JOIN enrollments e

                        ON c.id = e.course_id

                    WHERE c.lecturer_id = @lecturerId

                    GROUP BY

                        c.id,

                        c.title,

                        c.description,

                        c.category,

                        c.thumbnail,

                        c.video_url,

                        c.lecturer_id,

                        c.created_at

                    ORDER BY c.created_at DESC";

                using (var courseListCommand =

                    new MySqlCommand(coursesSql, connection))

                {

                    courseListCommand.Parameters.AddWithValue(

                        "@lecturerId",

                        lecturerId

                    );

                    using var reader =

                        courseListCommand.ExecuteReader();

                    while (reader.Read())

                    {

                        courses.Add(

                            new Course

                            {

                                Id =

                                    reader.GetInt32("id"),

                                Title =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("title")

                                    )

                                        ? ""

                                        : reader.GetString("title"),

                                Description =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("description")

                                    )

                                        ? ""

                                        : reader.GetString("description"),

                                Category =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("category")

                                    )

                                        ? ""

                                        : reader.GetString("category"),

                                Thumbnail =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("thumbnail")

                                    )

                                        ? ""

                                        : reader.GetString("thumbnail"),

                                VideoUrl =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("video_url")

                                    )

                                        ? ""

                                        : reader.GetString("video_url"),

                                LecturerId =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("lecturer_id")

                                    )

                                        ? null

                                        : reader.GetInt32("lecturer_id"),

                                StudentCount =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("student_count")

                                    )

                                        ? 0

                                        : Convert.ToInt32(

                                            reader["student_count"]

                                        ),

                                CreatedAt =

                                    reader.GetDateTime("created_at")

                            }

                        );

                    }

                }

                ViewBag.LecturerCourses = courses;

            }

            catch (MySqlException ex)

            {

                Console.WriteLine(

                    $"LECTURER DASHBOARD DATABASE ERROR: {ex.Message}"

                );

                ViewBag.TotalCourses = 0;

                ViewBag.TotalStudents = 0;

                ViewBag.LecturerCourses =

                    new List<Course>();

                ViewBag.DatabaseError =

                    "Unable to load lecturer dashboard data.";

            }

            catch (Exception ex)

            {

                Console.WriteLine(

                    $"LECTURER DASHBOARD ERROR: {ex.Message}"

                );

                ViewBag.TotalCourses = 0;

                ViewBag.TotalStudents = 0;

                ViewBag.LecturerCourses =

                    new List<Course>();

                ViewBag.DatabaseError =

                    "Unable to load lecturer dashboard data.";

            }

            return View();

        }

        // =========================================================

        // COURSES

        // =========================================================

        public IActionResult Courses()

        {

            if (!IsLecturer())

            {

                return LoginRedirect();

            }

            LoadUser();

            int lecturerId = GetLecturerId();

            if (lecturerId <= 0)

            {

                HttpContext.Session.Clear();

                return LoginRedirect();

            }

            var courses = new List<Course>();

            try

            {

                using var connection = _db.GetConnection();

                connection.Open();

                // =================================================

                // LOAD COURSES ASSIGNED TO LOGGED-IN LECTURER

                // =================================================

                const string coursesSql = @"

                    SELECT

                        c.id,

                        c.title,

                        c.description,

                        c.category,

                        c.thumbnail,

                        c.video_url,

                        c.lecturer_id,

                        c.created_at,

                        COUNT(DISTINCT e.user_id) AS student_count

                    FROM courses c

                    LEFT JOIN enrollments e

                        ON c.id = e.course_id

                    WHERE c.lecturer_id = @lecturerId

                    GROUP BY

                        c.id,

                        c.title,

                        c.description,

                        c.category,

                        c.thumbnail,

                        c.video_url,

                        c.lecturer_id,

                        c.created_at

                    ORDER BY c.created_at DESC";

                using var command =

                    new MySqlCommand(coursesSql, connection);

                command.Parameters.AddWithValue(

                    "@lecturerId",

                    lecturerId

                );

                using var reader = command.ExecuteReader();

                while (reader.Read())

                {

                    courses.Add(

                        new Course

                        {

                            Id =

                                reader.GetInt32("id"),

                            Title =

                                reader.IsDBNull(

                                    reader.GetOrdinal("title")

                                )

                                    ? ""

                                    : reader.GetString("title"),

                            Description =

                                reader.IsDBNull(

                                    reader.GetOrdinal("description")

                                )

                                    ? ""

                                    : reader.GetString("description"),

                            Category =

                                reader.IsDBNull(

                                    reader.GetOrdinal("category")

                                )

                                    ? ""

                                    : reader.GetString("category"),

                            Thumbnail =

                                reader.IsDBNull(

                                    reader.GetOrdinal("thumbnail")

                                )

                                    ? ""

                                    : reader.GetString("thumbnail"),

                            VideoUrl =

                                reader.IsDBNull(

                                    reader.GetOrdinal("video_url")

                                )

                                    ? ""

                                    : reader.GetString("video_url"),

                            LecturerId =

                                reader.IsDBNull(

                                    reader.GetOrdinal("lecturer_id")

                                )

                                    ? null

                                    : reader.GetInt32("lecturer_id"),

                            StudentCount =

                                reader.IsDBNull(

                                    reader.GetOrdinal("student_count")

                                )

                                    ? 0

                                    : Convert.ToInt32(

                                        reader["student_count"]

                                    ),

                            CreatedAt =

                                reader.GetDateTime("created_at")

                        }

                    );

                }

                // =================================================

                // STATISTICS

                // =================================================

                ViewBag.TotalCourses = courses.Count;

                ViewBag.TotalStudents =

                    courses.Sum(c => c.StudentCount);

                ViewBag.LecturerCourses = courses;

            }

            catch (MySqlException ex)

            {

                Console.WriteLine(

                    $"LECTURER COURSES DATABASE ERROR: {ex.Message}"

                );

                ViewBag.TotalCourses = 0;

                ViewBag.TotalStudents = 0;

                ViewBag.LecturerCourses = courses;

                ViewBag.DatabaseError =

                    "Unable to load your courses.";

            }

            catch (Exception ex)

            {

                Console.WriteLine(

                    $"LECTURER COURSES ERROR: {ex.Message}"

                );

                ViewBag.TotalCourses = 0;

                ViewBag.TotalStudents = 0;

                ViewBag.LecturerCourses = courses;

                ViewBag.DatabaseError =

                    "Unable to load your courses.";

            }

            return View();

        }

        // =========================================================

        // STUDENTS

        // =========================================================

        public IActionResult Students(int? courseId)

        {

            if (!IsLecturer())

            {

                return LoginRedirect();

            }

            LoadUser();

            int lecturerId = GetLecturerId();

            if (lecturerId <= 0)

            {

                HttpContext.Session.Clear();

                return LoginRedirect();

            }

            // Dynamic rows are passed to the Razor view.

            var students = new List<dynamic>();

            // Default values so the page remains safe even

            // if the database query fails.

            ViewBag.TotalStudents = 0;

            ViewBag.AverageProgress = 0;

            ViewBag.TotalEnrollments = 0;

            ViewBag.SelectedCourseId = courseId;

            ViewBag.SelectedCourseTitle = "";

            ViewBag.SelectedCourseCategory = "";

            ViewBag.LecturerStudents = students;

            try

            {

                using var connection = _db.GetConnection();

                connection.Open();

                // =================================================

                // IF A COURSE WAS SELECTED, VERIFY THAT THE COURSE

                // BELONGS TO THE CURRENT LOGGED-IN LECTURER.

                // =================================================

                if (courseId.HasValue)

                {

                    const string verifyCourseSql = @"

                        SELECT

                            id,

                            title,

                            category

                        FROM courses

                        WHERE id = @courseId

                          AND lecturer_id = @lecturerId

                        LIMIT 1";

                    using var verifyCommand =

                        new MySqlCommand(

                            verifyCourseSql,

                            connection

                        );

                    verifyCommand.Parameters.AddWithValue(

                        "@courseId",

                        courseId.Value

                    );

                    verifyCommand.Parameters.AddWithValue(

                        "@lecturerId",

                        lecturerId

                    );

                    using var courseReader =

                        verifyCommand.ExecuteReader();

                    if (!courseReader.Read())

                    {

                        ViewBag.InvalidCourse = true;

                        ViewBag.DatabaseError =

                            "The selected course was not found or is not assigned to your lecturer account.";

                        ViewBag.SelectedCourseId = null;

                        return View();

                    }

                    ViewBag.SelectedCourseTitle =

                        courseReader.IsDBNull(

                            courseReader.GetOrdinal("title")

                        )

                            ? ""

                            : courseReader.GetString("title");

                    ViewBag.SelectedCourseCategory =

                        courseReader.IsDBNull(

                            courseReader.GetOrdinal("category")

                        )

                            ? ""

                            : courseReader.GetString("category");

                }

                // =================================================

                // LOAD REAL ENROLLED STUDENTS

                //

                // Each row represents one enrollment:

                // Student + Course + Progress.

                //

                // When courseId is supplied, only that course is

                // returned.

                // =================================================

                const string studentsSql = @"

                    SELECT

                        u.id AS student_id,

                        u.name AS student_name,

                        u.email AS student_email,

                        c.id AS course_id,

                        c.title AS course_title,

                        c.category AS course_category,

                        e.progress,

                        e.enrolled_at

                    FROM enrollments e

                    INNER JOIN users u

                        ON e.user_id = u.id

                    INNER JOIN courses c

                        ON e.course_id = c.id

                    WHERE c.lecturer_id = @lecturerId

                      AND u.role = 'student'

                      AND (

                            @courseId IS NULL

                            OR c.id = @courseId

                          )

                    ORDER BY

                        u.name ASC,

                        c.title ASC";

                using (var studentsCommand =

                    new MySqlCommand(

                        studentsSql,

                        connection

                    ))

                {

                    studentsCommand.Parameters.AddWithValue(

                        "@lecturerId",

                        lecturerId

                    );

                    studentsCommand.Parameters.AddWithValue(

                        "@courseId",

                        courseId.HasValue

                            ? courseId.Value

                            : DBNull.Value

                    );

                    using var reader =

                        studentsCommand.ExecuteReader();

                    while (reader.Read())

                    {

                        int progress =

                            reader.IsDBNull(

                                reader.GetOrdinal("progress")

                            )

                                ? 0

                                : reader.GetInt32("progress");

                        // Keep progress safe for the UI/progress bar.

                        progress =

                            Math.Clamp(progress, 0, 100);

                        students.Add(

                            new

                            {

                                StudentId =

                                    reader.GetInt32(

                                        "student_id"

                                    ),

                                StudentName =

                                    reader.IsDBNull(

                                        reader.GetOrdinal(

                                            "student_name"

                                        )

                                    )

                                        ? "Student"

                                        : reader.GetString(

                                            "student_name"

                                        ),

                                StudentEmail =

                                    reader.IsDBNull(

                                        reader.GetOrdinal(

                                            "student_email"

                                        )

                                    )

                                        ? ""

                                        : reader.GetString(

                                            "student_email"

                                        ),

                                CourseId =

                                    reader.GetInt32(

                                        "course_id"

                                    ),

                                CourseTitle =

                                    reader.IsDBNull(

                                        reader.GetOrdinal(

                                            "course_title"

                                        )

                                    )

                                        ? "Course"

                                        : reader.GetString(

                                            "course_title"

                                        ),

                                CourseCategory =

                                    reader.IsDBNull(

                                        reader.GetOrdinal(

                                            "course_category"

                                        )

                                    )

                                        ? ""

                                        : reader.GetString(

                                            "course_category"

                                        ),

                                Progress =

                                    progress,

                                EnrolledAt =

                                    reader.IsDBNull(

                                        reader.GetOrdinal(

                                            "enrolled_at"

                                        )

                                    )

                                        ? DateTime.MinValue

                                        : reader.GetDateTime(

                                            "enrolled_at"

                                        )

                            }

                        );

                    }

                }

                // =================================================

                // CALCULATE REAL PAGE STATISTICS

                // =================================================

                // Unique students.

                ViewBag.TotalStudents =

                    students

                        .Select(s => (int)s.StudentId)

                        .Distinct()

                        .Count();

                // One row represents one enrollment.

                ViewBag.TotalEnrollments =

                    students.Count;

                // Average enrollment progress.

                ViewBag.AverageProgress =

                    students.Count > 0

                        ? Convert.ToInt32(

                            Math.Round(

                                students.Average(

                                    s => (double)s.Progress

                                )

                            )

                        )

                        : 0;

                ViewBag.LecturerStudents = students;

            }

            catch (MySqlException ex)

            {

                Console.WriteLine(

                    $"LECTURER STUDENTS DATABASE ERROR: {ex.Message}"

                );

                ViewBag.TotalStudents = 0;

                ViewBag.AverageProgress = 0;

                ViewBag.TotalEnrollments = 0;

                ViewBag.LecturerStudents =

                    new List<dynamic>();

                ViewBag.DatabaseError =

                    "Unable to load enrolled students.";

            }

            catch (Exception ex)

            {

                Console.WriteLine(

                    $"LECTURER STUDENTS ERROR: {ex.Message}"

                );

                ViewBag.TotalStudents = 0;

                ViewBag.AverageProgress = 0;

                ViewBag.TotalEnrollments = 0;

                ViewBag.LecturerStudents =

                    new List<dynamic>();

                ViewBag.DatabaseError =

                    "Unable to load enrolled students.";

            }

            return View();

        }

        // =========================================================

        // QUIZZES

        // =========================================================

        public IActionResult Quizzes(int? courseId)

        {

            if (!IsLecturer())

            {

                return LoginRedirect();

            }

            LoadUser();

            int lecturerId = GetLecturerId();

            if (lecturerId <= 0)

            {

                HttpContext.Session.Clear();

                return LoginRedirect();

            }

            var courses = new List<Course>();

            var quizzes = new List<dynamic>();

            ViewBag.LecturerCourses = courses;

            ViewBag.LecturerQuizzes = quizzes;

            ViewBag.SelectedCourseId = courseId;

            ViewBag.TotalQuestions = 0;

            try

            {

                using var connection = _db.GetConnection();

                connection.Open();

                const string coursesSql = @"

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

                    WHERE lecturer_id = @lecturerId

                    ORDER BY title ASC";

                using (var courseCommand =

                    new MySqlCommand(coursesSql, connection))

                {

                    courseCommand.Parameters.AddWithValue(

                        "@lecturerId",

                        lecturerId

                    );

                    using var courseReader =

                        courseCommand.ExecuteReader();

                    while (courseReader.Read())

                    {

                        courses.Add(

                            new Course

                            {

                                Id = courseReader.GetInt32("id"),

                                Title = courseReader.IsDBNull(

                                    courseReader.GetOrdinal("title")

                                ) ? "" : courseReader.GetString("title"),

                                Description = courseReader.IsDBNull(

                                    courseReader.GetOrdinal("description")

                                ) ? "" : courseReader.GetString("description"),

                                Category = courseReader.IsDBNull(

                                    courseReader.GetOrdinal("category")

                                ) ? "" : courseReader.GetString("category"),

                                Thumbnail = courseReader.IsDBNull(

                                    courseReader.GetOrdinal("thumbnail")

                                ) ? "" : courseReader.GetString("thumbnail"),

                                VideoUrl = courseReader.IsDBNull(

                                    courseReader.GetOrdinal("video_url")

                                ) ? "" : courseReader.GetString("video_url"),

                                LecturerId = courseReader.IsDBNull(

                                    courseReader.GetOrdinal("lecturer_id")

                                ) ? null : courseReader.GetInt32("lecturer_id"),

                                CreatedAt = courseReader.IsDBNull(

                                    courseReader.GetOrdinal("created_at")

                                ) ? DateTime.MinValue : courseReader.GetDateTime("created_at")

                            }

                        );

                    }

                }

                if (courseId.HasValue &&

                    !courses.Any(c => c.Id == courseId.Value))

                {

                    TempData["Error"] =

                        "The selected course is not assigned to your lecturer account.";

                    return RedirectToAction(nameof(Quizzes));

                }

                const string quizzesSql = @"

                    SELECT

                        q.id,

                        q.course_id,

                        c.title AS course_title,

                        q.question,

                        q.option_a,

                        q.option_b,

                        q.option_c,

                        q.option_d,

                        q.correct_answer

                    FROM quizzes q

                    INNER JOIN courses c

                        ON q.course_id = c.id

                    WHERE c.lecturer_id = @lecturerId

                      AND (

                            @courseId IS NULL

                            OR q.course_id = @courseId

                          )

                    ORDER BY c.title ASC, q.id DESC";

                using var quizCommand =

                    new MySqlCommand(quizzesSql, connection);

                quizCommand.Parameters.AddWithValue(

                    "@lecturerId",

                    lecturerId

                );

                quizCommand.Parameters.AddWithValue(

                    "@courseId",

                    courseId.HasValue

                        ? courseId.Value

                        : DBNull.Value

                );

                using var quizReader =

                    quizCommand.ExecuteReader();

                while (quizReader.Read())

                {

                    quizzes.Add(

                        new

                        {

                            Id = quizReader.GetInt32("id"),

                            CourseId = quizReader.GetInt32("course_id"),

                            CourseTitle = quizReader.IsDBNull(

                                quizReader.GetOrdinal("course_title")

                            ) ? "Course" : quizReader.GetString("course_title"),

                            Question = quizReader.IsDBNull(

                                quizReader.GetOrdinal("question")

                            ) ? "" : quizReader.GetString("question"),

                            OptionA = quizReader.IsDBNull(

                                quizReader.GetOrdinal("option_a")

                            ) ? "" : quizReader.GetString("option_a"),

                            OptionB = quizReader.IsDBNull(

                                quizReader.GetOrdinal("option_b")

                            ) ? "" : quizReader.GetString("option_b"),

                            OptionC = quizReader.IsDBNull(

                                quizReader.GetOrdinal("option_c")

                            ) ? "" : quizReader.GetString("option_c"),

                            OptionD = quizReader.IsDBNull(

                                quizReader.GetOrdinal("option_d")

                            ) ? "" : quizReader.GetString("option_d"),

                            CorrectAnswer = quizReader.IsDBNull(

                                quizReader.GetOrdinal("correct_answer")

                            ) ? "" : quizReader.GetString("correct_answer")

                        }

                    );

                }

                ViewBag.LecturerCourses = courses;

                ViewBag.LecturerQuizzes = quizzes;

                ViewBag.TotalQuestions = quizzes.Count;

            }

            catch (MySqlException ex)

            {

                Console.WriteLine(

                    $"LECTURER QUIZZES DATABASE ERROR: {ex.Message}"

                );

                ViewBag.DatabaseError =

                    "Unable to load quiz information.";

            }

            catch (Exception ex)

            {

                Console.WriteLine(

                    $"LECTURER QUIZZES ERROR: {ex.Message}"

                );

                ViewBag.DatabaseError =

                    "Unable to load quiz information.";

            }

            return View();

        }

        // =========================================================

        // CREATE QUIZ QUESTION

        // =========================================================

        [HttpPost]

        [ValidateAntiForgeryToken]

        public IActionResult CreateQuiz(

            int courseId,

            string question,

            string optionA,

            string optionB,

            string optionC,

            string optionD,

            string correctAnswer

        )

        {

            if (!IsLecturer())

            {

                return LoginRedirect();

            }

            int lecturerId = GetLecturerId();

            if (lecturerId <= 0)

            {

                HttpContext.Session.Clear();

                return LoginRedirect();

            }

            question = (question ?? "").Trim();

            optionA = (optionA ?? "").Trim();

            optionB = (optionB ?? "").Trim();

            optionC = (optionC ?? "").Trim();

            optionD = (optionD ?? "").Trim();

            correctAnswer = (correctAnswer ?? "")

                .Trim()

                .ToUpperInvariant();

            if (courseId <= 0 ||

                string.IsNullOrWhiteSpace(question) ||

                string.IsNullOrWhiteSpace(optionA) ||

                string.IsNullOrWhiteSpace(optionB) ||

                string.IsNullOrWhiteSpace(optionC) ||

                string.IsNullOrWhiteSpace(optionD) ||

                !new[] { "A", "B", "C", "D" }.Contains(correctAnswer))

            {

                TempData["Error"] =

                    "Please complete the question, all four options, and select a valid correct answer.";

                return RedirectToAction(

                    nameof(Quizzes),

                    new { courseId }

                );

            }

            try

            {

                using var connection = _db.GetConnection();

                connection.Open();

                const string verifySql = @"

                    SELECT COUNT(*)

                    FROM courses

                    WHERE id = @courseId

                      AND lecturer_id = @lecturerId";

                using (var verifyCommand =

                    new MySqlCommand(verifySql, connection))

                {

                    verifyCommand.Parameters.AddWithValue(

                        "@courseId",

                        courseId

                    );

                    verifyCommand.Parameters.AddWithValue(

                        "@lecturerId",

                        lecturerId

                    );

                    int ownsCourse =

                        Convert.ToInt32(

                            verifyCommand.ExecuteScalar()

                        );

                    if (ownsCourse == 0)

                    {

                        TempData["Error"] =

                            "You can only create quiz questions for courses assigned to your account.";

                        return RedirectToAction(nameof(Quizzes));

                    }

                }

                const string insertSql = @"

                    INSERT INTO quizzes

                    (

                        course_id,

                        question,

                        option_a,

                        option_b,

                        option_c,

                        option_d,

                        correct_answer

                    )

                    VALUES

                    (

                        @courseId,

                        @question,

                        @optionA,

                        @optionB,

                        @optionC,

                        @optionD,

                        @correctAnswer

                    )";

                using var insertCommand =

                    new MySqlCommand(insertSql, connection);

                insertCommand.Parameters.AddWithValue("@courseId", courseId);

                insertCommand.Parameters.AddWithValue("@question", question);

                insertCommand.Parameters.AddWithValue("@optionA", optionA);

                insertCommand.Parameters.AddWithValue("@optionB", optionB);

                insertCommand.Parameters.AddWithValue("@optionC", optionC);

                insertCommand.Parameters.AddWithValue("@optionD", optionD);

                insertCommand.Parameters.AddWithValue(

                    "@correctAnswer",

                    correctAnswer

                );

                insertCommand.ExecuteNonQuery();

                TempData["Success"] =

                    "Quiz question created successfully.";

            }

            catch (Exception ex)

            {

                Console.WriteLine(

                    $"CREATE QUIZ ERROR: {ex.Message}"

                );

                TempData["Error"] =

                    "Unable to create the quiz question.";

            }

            return RedirectToAction(

                nameof(Quizzes),

                new { courseId }

            );

        }

        // =========================================================

        // DELETE QUIZ QUESTION

        // =========================================================

        [HttpPost]

        [ValidateAntiForgeryToken]

        public IActionResult DeleteQuiz(int id, int? courseId)

        {

            if (!IsLecturer())

            {

                return LoginRedirect();

            }

            int lecturerId = GetLecturerId();

            if (lecturerId <= 0)

            {

                HttpContext.Session.Clear();

                return LoginRedirect();

            }

            if (id <= 0)

            {

                TempData["Error"] =

                    "Invalid quiz question selected.";

                return RedirectToAction(

                    nameof(Quizzes),

                    new { courseId }

                );

            }

            try

            {

                using var connection = _db.GetConnection();

                connection.Open();

                const string deleteSql = @"

                    DELETE q

                    FROM quizzes q

                    INNER JOIN courses c

                        ON q.course_id = c.id

                    WHERE q.id = @id

                      AND c.lecturer_id = @lecturerId";

                using var deleteCommand =

                    new MySqlCommand(deleteSql, connection);

                deleteCommand.Parameters.AddWithValue("@id", id);

                deleteCommand.Parameters.AddWithValue(

                    "@lecturerId",

                    lecturerId

                );

                int affected =

                    deleteCommand.ExecuteNonQuery();

                TempData[affected > 0 ? "Success" : "Error"] =

                    affected > 0

                        ? "Quiz question deleted successfully."

                        : "Quiz question was not found or does not belong to your course.";

            }

            catch (Exception ex)

            {

                Console.WriteLine(

                    $"DELETE QUIZ ERROR: {ex.Message}"

                );

                TempData["Error"] =

                    "Unable to delete the quiz question.";

            }

            return RedirectToAction(

                nameof(Quizzes),

                new { courseId }

            );

        }

        // =========================================================

        // ASSIGNMENTS

        // =========================================================

        public IActionResult Assignments()

        {

            if (!IsLecturer())

            {

                return LoginRedirect();

            }

            LoadUser();

            return View();

        }

       // =========================================================

       // GRADES / REAL QUIZ RESULTS

       // =========================================================

       public IActionResult Grades()

       {

           if (!IsLecturer())

           {

               return LoginRedirect();

           }

           LoadUser();

           int lecturerId = GetLecturerId();

           if (lecturerId <= 0)

           {

               HttpContext.Session.Clear();

               return LoginRedirect();

           }

           var results = new List<dynamic>();

           ViewBag.LecturerResults = results;

           ViewBag.TotalAttempts = 0;

           ViewBag.TotalStudents = 0;

           ViewBag.AverageScore = 0;

           ViewBag.PassRate = 0;

           try

           {

               using var connection = _db.GetConnection();

               connection.Open();

               const string resultsSql = @"

                   SELECT

                       qr.id AS result_id,

                       qr.user_id AS student_id,

                       u.name AS student_name,

                       u.email AS student_email,

                       qr.course_id,

                       c.title AS course_title,

                       c.category AS course_category,

                       qr.score,

                       qr.taken_at

                   FROM quiz_results qr

                   INNER JOIN users u

                       ON qr.user_id = u.id

                   INNER JOIN courses c

                       ON qr.course_id = c.id

                   WHERE c.lecturer_id = @lecturerId

                     AND u.role = 'student'

                   ORDER BY qr.taken_at DESC, qr.id DESC";

               using var command = new MySqlCommand(resultsSql, connection);

               command.Parameters.AddWithValue("@lecturerId", lecturerId);

               using var reader = command.ExecuteReader();

               while (reader.Read())

               {

                   int score = reader.IsDBNull(reader.GetOrdinal("score"))

                       ? 0

                       : reader.GetInt32("score");

                   score = Math.Clamp(score, 0, 100);

                   results.Add(

                       new

                       {

                           ResultId = reader.GetInt32("result_id"),

                           StudentId = reader.GetInt32("student_id"),

                           StudentName = reader.IsDBNull(reader.GetOrdinal("student_name"))

                               ? "Student"

                               : reader.GetString("student_name"),

                           StudentEmail = reader.IsDBNull(reader.GetOrdinal("student_email"))

                               ? ""

                               : reader.GetString("student_email"),

                           CourseId = reader.GetInt32("course_id"),

                           CourseTitle = reader.IsDBNull(reader.GetOrdinal("course_title"))

                               ? "Course"

                               : reader.GetString("course_title"),

                           CourseCategory = reader.IsDBNull(reader.GetOrdinal("course_category"))

                               ? ""

                               : reader.GetString("course_category"),

                           Score = score,

                           TakenAt = reader.IsDBNull(reader.GetOrdinal("taken_at"))

                               ? DateTime.MinValue

                               : reader.GetDateTime("taken_at")

                       }

                   );

               }

               ViewBag.LecturerResults = results;

               ViewBag.TotalAttempts = results.Count;

               ViewBag.TotalStudents = results

                   .Select(r => (int)r.StudentId)

                   .Distinct()

                   .Count();

               ViewBag.AverageScore = results.Count > 0

                   ? Convert.ToInt32(

                       Math.Round(

                           results.Average(r => (double)r.Score)

                       )

                   )

                   : 0;

               ViewBag.PassRate = results.Count > 0

                   ? Convert.ToInt32(

                       Math.Round(

                           results.Count(r => (int)r.Score >= 50)

                           * 100.0 / results.Count

                       )

                   )

                   : 0;

           }

           catch (MySqlException ex)

           {

               Console.WriteLine(

                   $"LECTURER RESULTS DATABASE ERROR: {ex.Message}"

               );

               ViewBag.LecturerResults = new List<dynamic>();

               ViewBag.TotalAttempts = 0;

               ViewBag.TotalStudents = 0;

               ViewBag.AverageScore = 0;

               ViewBag.PassRate = 0;

               ViewBag.DatabaseError =

                   "Unable to load student quiz results.";

           }

           catch (Exception ex)

           {

               Console.WriteLine(

                   $"LECTURER RESULTS ERROR: {ex.Message}"

               );

               ViewBag.LecturerResults = new List<dynamic>();

               ViewBag.TotalAttempts = 0;

               ViewBag.TotalStudents = 0;

               ViewBag.AverageScore = 0;

               ViewBag.PassRate = 0;

               ViewBag.DatabaseError =

                   "Unable to load student quiz results.";

           }

           return View();

       }

       // =========================================================

       // PROFILE

       // =========================================================

       public IActionResult Profile()

       {

           if (!IsLecturer())

           {

               return LoginRedirect();

           }

           LoadUser();

           int lecturerId = GetLecturerId();

           if (lecturerId <= 0)

           {

               HttpContext.Session.Clear();

               return LoginRedirect();

           }

           ViewBag.ProfileCourseCount = 0;

           ViewBag.ProfileStudentCount = 0;

           ViewBag.ProfileQuizAttemptCount = 0;

           try

           {

               using var connection = _db.GetConnection();

               connection.Open();

               const string profileStatsSql = @"

                   SELECT

                       (

                           SELECT COUNT(*)

                           FROM courses c

                           WHERE c.lecturer_id = @lecturerId

                       ) AS course_count,

                       (

                           SELECT COUNT(DISTINCT e.user_id)

                           FROM enrollments e

                           INNER JOIN courses c

                               ON e.course_id = c.id

                           INNER JOIN users u

                               ON e.user_id = u.id

                           WHERE c.lecturer_id = @lecturerId

                             AND u.role = 'student'

                       ) AS student_count,

                       (

                           SELECT COUNT(*)

                           FROM quiz_results qr

                           INNER JOIN courses c

                               ON qr.course_id = c.id

                           WHERE c.lecturer_id = @lecturerId

                       ) AS quiz_attempt_count";

               using var command =

                   new MySqlCommand(profileStatsSql, connection);

               command.Parameters.AddWithValue(

                   "@lecturerId",

                   lecturerId

               );

               using var reader = command.ExecuteReader();

               if (reader.Read())

               {

                   ViewBag.ProfileCourseCount =

                       reader.IsDBNull(

                           reader.GetOrdinal("course_count")

                       )

                           ? 0

                           : Convert.ToInt32(

                               reader["course_count"]

                           );

                   ViewBag.ProfileStudentCount =

                       reader.IsDBNull(

                           reader.GetOrdinal("student_count")

                       )

                           ? 0

                           : Convert.ToInt32(

                               reader["student_count"]

                           );

                   ViewBag.ProfileQuizAttemptCount =

                       reader.IsDBNull(

                           reader.GetOrdinal("quiz_attempt_count")

                       )

                           ? 0

                           : Convert.ToInt32(

                               reader["quiz_attempt_count"]

                           );

               }

           }

           catch (MySqlException ex)

           {

               Console.WriteLine(

                   $"LECTURER PROFILE DATABASE ERROR: {ex.Message}"

               );

               ViewBag.DatabaseError =

                   "Unable to load teaching overview.";

           }

           catch (Exception ex)

           {

               Console.WriteLine(

                   $"LECTURER PROFILE ERROR: {ex.Message}"

               );

               ViewBag.DatabaseError =

                   "Unable to load teaching overview.";

           }

           return View();

       }

// =========================================================
        // UPLOAD PROFILE PICTURE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile? profilePicture
        )
        {
            if (!IsLecturer())
                return LoginRedirect();

            int lecturerId = GetLecturerId();

            if (lecturerId <= 0)
            {
                HttpContext.Session.Clear();
                return LoginRedirect();
            }

            if (profilePicture == null || profilePicture.Length == 0)
            {
                TempData["ProfileError"] =
                    "Please choose a profile picture.";

                return RedirectToAction(nameof(Profile));
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (profilePicture.Length > maxFileSize)
            {
                TempData["ProfileError"] =
                    "Profile picture must be 5 MB or smaller.";

                return RedirectToAction(nameof(Profile));
            }

            string extension =
                Path.GetExtension(profilePicture.FileName)
                    .ToLowerInvariant();

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

            Directory.CreateDirectory(uploadsFolder);

            string fileName =
                $"user_{lecturerId}_{Guid.NewGuid():N}{extension}";

            string physicalPath =
                Path.Combine(uploadsFolder, fileName);

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
                    await profilePicture.CopyToAsync(stream);
                }

                string? oldProfileImage = null;

                using (var connection = _db.GetConnection())
                {
                    connection.Open();

                    const string getOldSql = @"
                        SELECT profile_image
                        FROM users
                        WHERE id = @lecturerId
                          AND role = 'lecturer'
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
                            "@lecturerId",
                            lecturerId
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
                        WHERE id = @lecturerId
                          AND role = 'lecturer'";

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
                        "@lecturerId",
                        lecturerId
                    );

                    int rowsAffected =
                        updateCommand.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        if (System.IO.File.Exists(physicalPath))
                            System.IO.File.Delete(physicalPath);

                        TempData["ProfileError"] =
                            "Lecturer account could not be updated.";

                        return RedirectToAction(nameof(Profile));
                    }
                }

                if (!string.IsNullOrWhiteSpace(oldProfileImage) &&
                    oldProfileImage.StartsWith(
                        "/uploads/profiles/",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    string oldFileName =
                        Path.GetFileName(oldProfileImage);

                    string oldPhysicalPath =
                        Path.Combine(
                            uploadsFolder,
                            oldFileName
                        );

                    if (System.IO.File.Exists(oldPhysicalPath))
                        System.IO.File.Delete(oldPhysicalPath);
                }

                TempData["ProfileSuccess"] =
                    "Profile picture updated successfully.";
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(physicalPath))
                    System.IO.File.Delete(physicalPath);

                TempData["ProfileError"] =
                    "Unable to upload profile picture: " +
                    ex.Message;
            }

            return RedirectToAction(nameof(Profile));
        }

}

}
