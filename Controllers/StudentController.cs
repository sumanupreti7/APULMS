using Microsoft.AspNetCore.Mvc;

using MySqlConnector;

using APULMS.Models;



namespace APULMS.Controllers

{

    public class StudentController : Controller

    {

        private readonly DatabaseHelper _db;



        public StudentController(DatabaseHelper db)

        {

            _db = db;

        }



        public IActionResult Dashboard()

        {

            // =====================================================

            // CHECK LOGIN SESSION

            // =====================================================

            string? userIdString =

                HttpContext.Session.GetString("UserId");



            string? role =

                HttpContext.Session.GetString("Role");



            if (string.IsNullOrEmpty(userIdString))

            {

                return RedirectToAction(

                    "Login",

                    "Account"

                );

            }



            if (role != "student")

            {

                return RedirectToAction(

                    "Login",

                    "Account"

                );

            }



            if (!int.TryParse(userIdString, out int userId))

            {

                HttpContext.Session.Clear();



                return RedirectToAction(

                    "Login",

                    "Account"

                );

            }



            // =====================================================

            // SESSION USER INFORMATION

            // =====================================================

            ViewBag.Name =

                HttpContext.Session.GetString("UserName");



            ViewBag.Email =

                HttpContext.Session.GetString("UserEmail");



            ViewBag.Role =

                HttpContext.Session.GetString("Role");


            // Load shared student information including profile image.
            SetStudentViewBag();



            // =====================================================

            // DEFAULT VALUES

            // =====================================================

            ViewBag.EnrolledCourseCount = 0;

            ViewBag.AverageProgress = 0;

            ViewBag.QuizCount = 0;

            ViewBag.AverageQuizScore = 0;



            var enrolledCourses =

                new List<Course>();



            var quizResults =

                new List<StudentQuizResult>();



            try

            {

                using var connection =

                    _db.GetConnection();



                connection.Open();



                // =================================================

                // GET STUDENT'S ENROLLED COURSES

                // =================================================

                const string courseSql = @"

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

                        COALESCE(e.progress, 0) AS progress

                    FROM enrollments e

                    INNER JOIN courses c

                        ON e.course_id = c.id

                    LEFT JOIN users u

                        ON c.lecturer_id = u.id

                    WHERE e.user_id = @userId

                    ORDER BY e.enrolled_at DESC";



                using (

                    var courseCommand =

                        new MySqlCommand(

                            courseSql,

                            connection

                        )

                )

                {

                    courseCommand.Parameters.AddWithValue(

                        "@userId",

                        userId

                    );



                    using var reader =

                        courseCommand.ExecuteReader();



                    int totalProgress = 0;



                    while (reader.Read())

                    {

                        int progress =

                            reader.IsDBNull(

                                reader.GetOrdinal("progress")

                            )

                                ? 0

                                : reader.GetInt32("progress");



                        enrolledCourses.Add(

                            new Course

                            {

                                Id =

                                    reader.GetInt32("id"),



                                Title =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("title")

                                    )

                                        ? string.Empty

                                        : reader.GetString("title"),



                                Description =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("description")

                                    )

                                        ? string.Empty

                                        : reader.GetString("description"),



                                Category =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("category")

                                    )

                                        ? string.Empty

                                        : reader.GetString("category"),



                                Thumbnail =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("thumbnail")

                                    )

                                        ? string.Empty

                                        : reader.GetString("thumbnail"),



                                VideoUrl =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("video_url")

                                    )

                                        ? string.Empty

                                        : reader.GetString("video_url"),



                                LecturerId =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("lecturer_id")

                                    )

                                        ? null

                                        : reader.GetInt32("lecturer_id"),



                                LecturerName =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("lecturer_name")

                                    )

                                        ? "Unassigned"

                                        : reader.GetString("lecturer_name"),



                                StudentCount = progress,



                                CreatedAt =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("created_at")

                                    )

                                        ? DateTime.MinValue

                                        : reader.GetDateTime("created_at")

                            }

                        );



                        totalProgress += progress;

                    }



                    ViewBag.EnrolledCourseCount =

                        enrolledCourses.Count;



                    if (enrolledCourses.Count > 0)

                    {

                        ViewBag.AverageProgress =

                            totalProgress /

                            enrolledCourses.Count;

                    }

                }



                // =================================================

                // GET STUDENT QUIZ RESULTS

                // =================================================

                const string resultSql = @"

                    SELECT

                        qr.id,

                        qr.course_id,

                        qr.score,

                        qr.taken_at,

                        c.title AS course_title

                    FROM quiz_results qr

                    INNER JOIN courses c

                        ON qr.course_id = c.id

                    WHERE qr.user_id = @userId

                    ORDER BY qr.taken_at DESC";



                using (

                    var resultCommand =

                        new MySqlCommand(

                            resultSql,

                            connection

                        )

                )

                {

                    resultCommand.Parameters.AddWithValue(

                        "@userId",

                        userId

                    );



                    using var reader =

                        resultCommand.ExecuteReader();



                    int totalScore = 0;



                    while (reader.Read())

                    {

                        int score =

                            reader.IsDBNull(

                                reader.GetOrdinal("score")

                            )

                                ? 0

                                : reader.GetInt32("score");



                        quizResults.Add(

                            new StudentQuizResult

                            {

                                Id =

                                    reader.GetInt32("id"),



                                CourseId =

                                    reader.GetInt32("course_id"),



                                CourseTitle =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("course_title")

                                    )

                                        ? string.Empty

                                        : reader.GetString("course_title"),



                                Score = score,



                                TakenAt =

                                    reader.IsDBNull(

                                        reader.GetOrdinal("taken_at")

                                    )

                                        ? DateTime.MinValue

                                        : reader.GetDateTime("taken_at")

                            }

                        );



                        totalScore += score;

                    }



                    ViewBag.QuizCount =

                        quizResults.Count;



                    if (quizResults.Count > 0)

                    {

                        ViewBag.AverageQuizScore =

                            totalScore /

                            quizResults.Count;

                    }

                }



                // =================================================

                // SEND DATABASE DATA TO VIEW

                // =================================================

                ViewBag.EnrolledCourses =

                    enrolledCourses;



                ViewBag.QuizResults =

                    quizResults;



                return View();

            }

            catch (Exception ex)

            {

                Console.WriteLine(

                    $"STUDENT DASHBOARD ERROR: {ex.Message}"

                );



                ViewBag.Error =

                    "Unable to load dashboard information.";



                ViewBag.EnrolledCourses =

                    enrolledCourses;



                ViewBag.QuizResults =

                    quizResults;



                return View();

            }

        }


        // =============================================================
        // AVAILABLE COURSES
        // =============================================================

        [HttpGet]
        public IActionResult Courses()
        {
            if (!TryGetStudentId(out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            SetStudentViewBag();

            var courses = new List<Course>();
            var enrolledCourseIds = new HashSet<int>();

            try
            {
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
                        CASE WHEN e.id IS NULL THEN 0 ELSE 1 END AS is_enrolled
                    FROM courses c
                    LEFT JOIN users u
                        ON c.lecturer_id = u.id
                    LEFT JOIN enrollments e
                        ON e.course_id = c.id
                       AND e.user_id = @userId
                    ORDER BY c.created_at DESC";

                using var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@userId", userId);

                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int courseId = reader.GetInt32("id");

                    courses.Add(
                        new Course
                        {
                            Id = courseId,
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
                            LecturerName = reader.IsDBNull(reader.GetOrdinal("lecturer_name"))
                                ? "Unassigned"
                                : reader.GetString("lecturer_name"),
                            CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at"))
                                ? DateTime.MinValue
                                : reader.GetDateTime("created_at")
                        }
                    );

                    if (reader.GetInt32("is_enrolled") == 1)
                    {
                        enrolledCourseIds.Add(courseId);
                    }
                }

                ViewBag.EnrolledCourseIds = enrolledCourseIds;
                return View(courses);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"STUDENT COURSES ERROR: {ex.Message}");

                ViewBag.Error = "Unable to load available courses.";
                ViewBag.EnrolledCourseIds = enrolledCourseIds;

                return View(courses);
            }
        }


        // =============================================================
        // ENROLL IN COURSE
        // =============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enroll(int courseId)
        {
            if (!TryGetStudentId(out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (courseId <= 0)
            {
                TempData["Error"] = "Invalid course selected.";
                return RedirectToAction(nameof(Courses));
            }

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string courseSql = @"
                    SELECT COUNT(*)
                    FROM courses
                    WHERE id = @courseId";

                using (var courseCommand = new MySqlCommand(courseSql, connection))
                {
                    courseCommand.Parameters.AddWithValue("@courseId", courseId);

                    int courseExists =
                        Convert.ToInt32(courseCommand.ExecuteScalar());

                    if (courseExists == 0)
                    {
                        TempData["Error"] = "The selected course does not exist.";
                        return RedirectToAction(nameof(Courses));
                    }
                }

                const string enrolledSql = @"
                    SELECT COUNT(*)
                    FROM enrollments
                    WHERE user_id = @userId
                      AND course_id = @courseId";

                using (var enrolledCommand = new MySqlCommand(enrolledSql, connection))
                {
                    enrolledCommand.Parameters.AddWithValue("@userId", userId);
                    enrolledCommand.Parameters.AddWithValue("@courseId", courseId);

                    int alreadyEnrolled =
                        Convert.ToInt32(enrolledCommand.ExecuteScalar());

                    if (alreadyEnrolled > 0)
                    {
                        TempData["Warning"] =
                            "You are already enrolled in this course.";

                        return RedirectToAction(nameof(Courses));
                    }
                }

                const string insertSql = @"
                    INSERT INTO enrollments
                        (user_id, course_id, progress)
                    VALUES
                        (@userId, @courseId, 0)";

                using var insertCommand =
                    new MySqlCommand(insertSql, connection);

                insertCommand.Parameters.AddWithValue("@userId", userId);
                insertCommand.Parameters.AddWithValue("@courseId", courseId);

                insertCommand.ExecuteNonQuery();

                TempData["Success"] =
                    "Course enrolment completed successfully.";

                return RedirectToAction(nameof(Courses));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"STUDENT ENROLL ERROR: {ex.Message}");

                TempData["Error"] =
                    "Unable to enrol in the course right now.";

                return RedirectToAction(nameof(Courses));
            }
        }


        // =============================================================
        // STUDENT QUIZZES
        // =============================================================

        [HttpGet]
        public IActionResult Quizzes()
        {
            if (!TryGetStudentId(out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            SetStudentViewBag();

            var quizCourses = new List<StudentQuizCourse>();

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string sql = @"
                    SELECT
                        c.id AS course_id,
                        c.title AS course_title,
                        c.category,
                        COALESCE(u.name, 'Unassigned') AS lecturer_name,
                        COUNT(q.id) AS question_count,
                        (
                            SELECT qr.score
                            FROM quiz_results qr
                            WHERE qr.user_id = @userId
                              AND qr.course_id = c.id
                            ORDER BY qr.taken_at DESC, qr.id DESC
                            LIMIT 1
                        ) AS latest_score,
                        (
                            SELECT qr.taken_at
                            FROM quiz_results qr
                            WHERE qr.user_id = @userId
                              AND qr.course_id = c.id
                            ORDER BY qr.taken_at DESC, qr.id DESC
                            LIMIT 1
                        ) AS latest_taken_at
                    FROM enrollments e
                    INNER JOIN courses c
                        ON e.course_id = c.id
                    LEFT JOIN users u
                        ON c.lecturer_id = u.id
                    LEFT JOIN quizzes q
                        ON q.course_id = c.id
                    WHERE e.user_id = @userId
                    GROUP BY
                        c.id,
                        c.title,
                        c.category,
                        u.name
                    HAVING COUNT(q.id) > 0
                    ORDER BY c.title ASC";

                using var command =
                    new MySqlCommand(sql, connection);

                command.Parameters.AddWithValue(
                    "@userId",
                    userId
                );

                using var reader =
                    command.ExecuteReader();

                while (reader.Read())
                {
                    quizCourses.Add(
                        new StudentQuizCourse
                        {
                            CourseId =
                                reader.GetInt32("course_id"),

                            CourseTitle =
                                reader.IsDBNull(
                                    reader.GetOrdinal("course_title")
                                )
                                    ? string.Empty
                                    : reader.GetString("course_title"),

                            Category =
                                reader.IsDBNull(
                                    reader.GetOrdinal("category")
                                )
                                    ? string.Empty
                                    : reader.GetString("category"),

                            LecturerName =
                                reader.IsDBNull(
                                    reader.GetOrdinal("lecturer_name")
                                )
                                    ? "Unassigned"
                                    : reader.GetString("lecturer_name"),

                            QuestionCount =
                                Convert.ToInt32(
                                    reader["question_count"]
                                ),

                            LatestScore =
                                reader.IsDBNull(
                                    reader.GetOrdinal("latest_score")
                                )
                                    ? null
                                    : Convert.ToInt32(
                                        reader["latest_score"]
                                    ),

                            LatestTakenAt =
                                reader.IsDBNull(
                                    reader.GetOrdinal("latest_taken_at")
                                )
                                    ? null
                                    : reader.GetDateTime(
                                        "latest_taken_at"
                                    )
                        }
                    );
                }

                ViewBag.QuizCourses = quizCourses;
                ViewBag.AvailableQuizCount =
                    quizCourses.Count;

                ViewBag.CompletedQuizCount =
                    quizCourses.Count(
                        q => q.LatestScore.HasValue
                    );

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"STUDENT QUIZZES ERROR: {ex.Message}"
                );

                ViewBag.Error =
                    "Unable to load available quizzes.";

                ViewBag.QuizCourses = quizCourses;
                ViewBag.AvailableQuizCount = 0;
                ViewBag.CompletedQuizCount = 0;

                return View();
            }
        }


        // =============================================================
        // TAKE QUIZ
        // =============================================================

        [HttpGet]
        public IActionResult TakeQuiz(int courseId)
        {
            if (!TryGetStudentId(out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            SetStudentViewBag();

            if (courseId <= 0)
            {
                TempData["Error"] =
                    "Invalid course selected.";

                return RedirectToAction(nameof(Quizzes));
            }

            var questions =
                new List<StudentQuizQuestion>();

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string courseSql = @"
                    SELECT
                        c.id,
                        c.title,
                        c.category,
                        COALESCE(u.name, 'Unassigned') AS lecturer_name
                    FROM enrollments e
                    INNER JOIN courses c
                        ON e.course_id = c.id
                    LEFT JOIN users u
                        ON c.lecturer_id = u.id
                    WHERE e.user_id = @userId
                      AND c.id = @courseId
                    LIMIT 1";

                using (
                    var courseCommand =
                        new MySqlCommand(
                            courseSql,
                            connection
                        )
                )
                {
                    courseCommand.Parameters.AddWithValue(
                        "@userId",
                        userId
                    );

                    courseCommand.Parameters.AddWithValue(
                        "@courseId",
                        courseId
                    );

                    using var courseReader =
                        courseCommand.ExecuteReader();

                    if (!courseReader.Read())
                    {
                        TempData["Error"] =
                            "You must be enrolled in this course before taking its quiz.";

                        return RedirectToAction(
                            nameof(Quizzes)
                        );
                    }

                    ViewBag.CourseId =
                        courseReader.GetInt32("id");

                    ViewBag.CourseTitle =
                        courseReader.IsDBNull(
                            courseReader.GetOrdinal("title")
                        )
                            ? "Course Quiz"
                            : courseReader.GetString("title");

                    ViewBag.Category =
                        courseReader.IsDBNull(
                            courseReader.GetOrdinal("category")
                        )
                            ? ""
                            : courseReader.GetString("category");

                    ViewBag.LecturerName =
                        courseReader.IsDBNull(
                            courseReader.GetOrdinal("lecturer_name")
                        )
                            ? "Unassigned"
                            : courseReader.GetString("lecturer_name");
                }

                const string quizSql = @"
                    SELECT
                        id,
                        question,
                        option_a,
                        option_b,
                        option_c,
                        option_d
                    FROM quizzes
                    WHERE course_id = @courseId
                    ORDER BY id ASC";

                using var quizCommand =
                    new MySqlCommand(
                        quizSql,
                        connection
                    );

                quizCommand.Parameters.AddWithValue(
                    "@courseId",
                    courseId
                );

                using var quizReader =
                    quizCommand.ExecuteReader();

                while (quizReader.Read())
                {
                    questions.Add(
                        new StudentQuizQuestion
                        {
                            Id =
                                quizReader.GetInt32("id"),

                            Question =
                                quizReader.IsDBNull(
                                    quizReader.GetOrdinal("question")
                                )
                                    ? ""
                                    : quizReader.GetString("question"),

                            OptionA =
                                quizReader.IsDBNull(
                                    quizReader.GetOrdinal("option_a")
                                )
                                    ? ""
                                    : quizReader.GetString("option_a"),

                            OptionB =
                                quizReader.IsDBNull(
                                    quizReader.GetOrdinal("option_b")
                                )
                                    ? ""
                                    : quizReader.GetString("option_b"),

                            OptionC =
                                quizReader.IsDBNull(
                                    quizReader.GetOrdinal("option_c")
                                )
                                    ? ""
                                    : quizReader.GetString("option_c"),

                            OptionD =
                                quizReader.IsDBNull(
                                    quizReader.GetOrdinal("option_d")
                                )
                                    ? ""
                                    : quizReader.GetString("option_d")
                        }
                    );
                }

                if (questions.Count == 0)
                {
                    TempData["Warning"] =
                        "No quiz questions are available for this course yet.";

                    return RedirectToAction(
                        nameof(Quizzes)
                    );
                }

                ViewBag.QuizQuestions = questions;
                ViewBag.QuestionCount = questions.Count;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"TAKE QUIZ ERROR: {ex.Message}"
                );

                TempData["Error"] =
                    "Unable to load this quiz right now.";

                return RedirectToAction(nameof(Quizzes));
            }
        }


        // =============================================================
        // SUBMIT QUIZ
        // =============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitQuiz(int courseId)
        {
            if (!TryGetStudentId(out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (courseId <= 0)
            {
                TempData["Error"] =
                    "Invalid quiz submission.";

                return RedirectToAction(nameof(Quizzes));
            }

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string enrolledSql = @"
                    SELECT COUNT(*)
                    FROM enrollments
                    WHERE user_id = @userId
                      AND course_id = @courseId";

                using (
                    var enrolledCommand =
                        new MySqlCommand(
                            enrolledSql,
                            connection
                        )
                )
                {
                    enrolledCommand.Parameters.AddWithValue(
                        "@userId",
                        userId
                    );

                    enrolledCommand.Parameters.AddWithValue(
                        "@courseId",
                        courseId
                    );

                    int enrolled =
                        Convert.ToInt32(
                            enrolledCommand.ExecuteScalar()
                        );

                    if (enrolled == 0)
                    {
                        TempData["Error"] =
                            "You are not enrolled in this course.";

                        return RedirectToAction(
                            nameof(Quizzes)
                        );
                    }
                }

                const string quizSql = @"
                    SELECT
                        id,
                        correct_answer
                    FROM quizzes
                    WHERE course_id = @courseId
                    ORDER BY id ASC";

                var correctAnswers =
                    new Dictionary<int, string>();

                using (
                    var quizCommand =
                        new MySqlCommand(
                            quizSql,
                            connection
                        )
                )
                {
                    quizCommand.Parameters.AddWithValue(
                        "@courseId",
                        courseId
                    );

                    using var reader =
                        quizCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        correctAnswers[
                            reader.GetInt32("id")
                        ] =
                            reader.IsDBNull(
                                reader.GetOrdinal(
                                    "correct_answer"
                                )
                            )
                                ? ""
                                : reader.GetString(
                                    "correct_answer"
                                )
                                    .Trim()
                                    .ToUpperInvariant();
                    }
                }

                if (correctAnswers.Count == 0)
                {
                    TempData["Warning"] =
                        "No quiz questions are available for this course.";

                    return RedirectToAction(
                        nameof(Quizzes)
                    );
                }

                int correctCount = 0;

                foreach (
                    var item in correctAnswers
                )
                {
                    string answerKey =
                        $"answer_{item.Key}";

                    string submittedAnswer =
                        Request.Form[answerKey]
                            .ToString()
                            .Trim()
                            .ToUpperInvariant();

                    if (
                        submittedAnswer ==
                        item.Value
                    )
                    {
                        correctCount++;
                    }
                }

                int score =
                    (int)Math.Round(
                        (
                            correctCount * 100.0
                        ) /
                        correctAnswers.Count
                    );

                const string insertSql = @"
                    INSERT INTO quiz_results
                        (
                            user_id,
                            course_id,
                            score
                        )
                    VALUES
                        (
                            @userId,
                            @courseId,
                            @score
                        )";

                using var insertCommand =
                    new MySqlCommand(
                        insertSql,
                        connection
                    );

                insertCommand.Parameters.AddWithValue(
                    "@userId",
                    userId
                );

                insertCommand.Parameters.AddWithValue(
                    "@courseId",
                    courseId
                );

                insertCommand.Parameters.AddWithValue(
                    "@score",
                    score
                );

                insertCommand.ExecuteNonQuery();

                TempData["QuizScore"] = score;
                TempData["CorrectAnswers"] =
                    correctCount;
                TempData["TotalQuestions"] =
                    correctAnswers.Count;

                return RedirectToAction(
                    nameof(QuizResult),
                    new { courseId }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"SUBMIT QUIZ ERROR: {ex.Message}"
                );

                TempData["Error"] =
                    "Unable to submit your quiz right now.";

                return RedirectToAction(
                    nameof(TakeQuiz),
                    new { courseId }
                );
            }
        }


        // =============================================================
        // QUIZ RESULT
        // =============================================================

        [HttpGet]
        public IActionResult QuizResult(int courseId)
        {
            if (!TryGetStudentId(out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            SetStudentViewBag();

            if (courseId <= 0)
            {
                return RedirectToAction(nameof(Quizzes));
            }

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string resultSql = @"
                    SELECT
                        qr.score,
                        qr.taken_at,
                        c.title AS course_title
                    FROM quiz_results qr
                    INNER JOIN courses c
                        ON qr.course_id = c.id
                    WHERE qr.user_id = @userId
                      AND qr.course_id = @courseId
                    ORDER BY qr.taken_at DESC, qr.id DESC
                    LIMIT 1";

                using var command =
                    new MySqlCommand(
                        resultSql,
                        connection
                    );

                command.Parameters.AddWithValue(
                    "@userId",
                    userId
                );

                command.Parameters.AddWithValue(
                    "@courseId",
                    courseId
                );

                using var reader =
                    command.ExecuteReader();

                if (!reader.Read())
                {
                    TempData["Warning"] =
                        "No quiz result was found for this course.";

                    return RedirectToAction(
                        nameof(Quizzes)
                    );
                }

                ViewBag.CourseId = courseId;

                ViewBag.CourseTitle =
                    reader.IsDBNull(
                        reader.GetOrdinal("course_title")
                    )
                        ? "Course Quiz"
                        : reader.GetString("course_title");

                ViewBag.Score =
                    reader.IsDBNull(
                        reader.GetOrdinal("score")
                    )
                        ? 0
                        : reader.GetInt32("score");

                ViewBag.TakenAt =
                    reader.IsDBNull(
                        reader.GetOrdinal("taken_at")
                    )
                        ? DateTime.MinValue
                        : reader.GetDateTime("taken_at");

                ViewBag.CorrectAnswers =
                    TempData["CorrectAnswers"];

                ViewBag.TotalQuestions =
                    TempData["TotalQuestions"];

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"QUIZ RESULT ERROR: {ex.Message}"
                );

                TempData["Error"] =
                    "Unable to load the quiz result.";

                return RedirectToAction(nameof(Quizzes));
            }
        }


        // =============================================================
        // UPLOAD STUDENT PROFILE PICTURE
        // =============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UploadProfilePicture(IFormFile? profilePicture)
        {
            if (!TryGetStudentId(out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (profilePicture == null ||
                profilePicture.Length == 0)
            {
                TempData["ProfileError"] =
                    "Please select a profile picture.";

                return RedirectToAction(nameof(Dashboard));
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (profilePicture.Length > maxFileSize)
            {
                TempData["ProfileError"] =
                    "Profile picture must be 5 MB or smaller.";

                return RedirectToAction(nameof(Dashboard));
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

            string[] allowedContentTypes =
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedExtensions.Contains(extension) ||
                !allowedContentTypes.Contains(
                    profilePicture.ContentType.ToLowerInvariant()
                ))
            {
                TempData["ProfileError"] =
                    "Only JPG, JPEG, PNG and WebP images are allowed.";

                return RedirectToAction(nameof(Dashboard));
            }

            string uploadDirectory =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "profiles"
                );

            Directory.CreateDirectory(uploadDirectory);

            string fileName =
                $"user_{userId}_{Guid.NewGuid():N}{extension}";

            string physicalPath =
                Path.Combine(
                    uploadDirectory,
                    fileName
                );

            string publicPath =
                $"/uploads/profiles/{fileName}";

            string? oldProfileImage = null;

            try
            {
                using (
                    var stream =
                        new FileStream(
                            physicalPath,
                            FileMode.Create
                        )
                )
                {
                    profilePicture.CopyTo(stream);
                }

                using var connection =
                    _db.GetConnection();

                connection.Open();

                const string oldImageSql = @"
                    SELECT profile_image
                    FROM users
                    WHERE id = @userId
                      AND role = 'student'
                    LIMIT 1";

                using (
                    var oldImageCommand =
                        new MySqlCommand(
                            oldImageSql,
                            connection
                        )
                )
                {
                    oldImageCommand.Parameters.AddWithValue(
                        "@userId",
                        userId
                    );

                    object? oldImageResult =
                        oldImageCommand.ExecuteScalar();

                    if (oldImageResult != null &&
                        oldImageResult != DBNull.Value)
                    {
                        oldProfileImage =
                            oldImageResult.ToString();
                    }
                }

                const string updateSql = @"
                    UPDATE users
                    SET profile_image = @profileImage
                    WHERE id = @userId
                      AND role = 'student'";

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

                int affectedRows =
                    updateCommand.ExecuteNonQuery();

                if (affectedRows <= 0)
                {
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }

                    TempData["ProfileError"] =
                        "Unable to update your profile picture.";

                    return RedirectToAction(nameof(Dashboard));
                }

                HttpContext.Session.SetString(
                    "ProfileImage",
                    publicPath
                );

                if (!string.IsNullOrWhiteSpace(oldProfileImage) &&
                    oldProfileImage.StartsWith(
                        "/uploads/profiles/",
                        StringComparison.OrdinalIgnoreCase
                    ) &&
                    !string.Equals(
                        oldProfileImage,
                        publicPath,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    string oldFileName =
                        Path.GetFileName(oldProfileImage);

                    string oldPhysicalPath =
                        Path.Combine(
                            uploadDirectory,
                            oldFileName
                        );

                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        System.IO.File.Delete(oldPhysicalPath);
                    }
                }

                TempData["ProfileSuccess"] =
                    "Profile picture updated successfully.";

                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"STUDENT PROFILE PICTURE ERROR: {ex.Message}"
                );

                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }

                TempData["ProfileError"] =
                    "Unable to update your profile picture right now.";

                return RedirectToAction(nameof(Dashboard));
            }
        }


        // =============================================================
        // STUDENT SESSION HELPERS
        // =============================================================

        private bool TryGetStudentId(out int userId)
        {
            userId = 0;

            string? userIdString =
                HttpContext.Session.GetString("UserId");

            string? role =
                HttpContext.Session.GetString("Role");

            if (string.IsNullOrWhiteSpace(userIdString) ||
                !string.Equals(
                    role,
                    "student",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return false;
            }

            return int.TryParse(userIdString, out userId);
        }


        private void SetStudentViewBag()
        {
            ViewBag.Name =
                HttpContext.Session.GetString("UserName");

            ViewBag.Email =
                HttpContext.Session.GetString("UserEmail");

            ViewBag.Role =
                HttpContext.Session.GetString("Role");

            ViewBag.ProfileImage = null;

            if (!TryGetStudentId(out int userId))
            {
                HttpContext.Session.Remove("ProfileImage");
                return;
            }

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string sql = @"
                    SELECT profile_image
                    FROM users
                    WHERE id = @userId
                      AND role = 'student'
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
                    result != DBNull.Value &&
                    !string.IsNullOrWhiteSpace(result.ToString()))
                {
                    string profileImage =
                        result.ToString()!;

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
                ViewBag.ProfileImage =
                    HttpContext.Session.GetString("ProfileImage");
            }
        }

    }



    // =============================================================
    // SIMPLE VIEW MODEL FOR AVAILABLE STUDENT QUIZZES
    // =============================================================

    public class StudentQuizCourse
    {
        public int CourseId { get; set; }

        public string CourseTitle { get; set; }
            = string.Empty;

        public string Category { get; set; }
            = string.Empty;

        public string LecturerName { get; set; }
            = string.Empty;

        public int QuestionCount { get; set; }

        public int? LatestScore { get; set; }

        public DateTime? LatestTakenAt { get; set; }
    }


    // =============================================================
    // SIMPLE VIEW MODEL FOR QUIZ QUESTIONS
    // =============================================================

    public class StudentQuizQuestion
    {
        public int Id { get; set; }

        public string Question { get; set; }
            = string.Empty;

        public string OptionA { get; set; }
            = string.Empty;

        public string OptionB { get; set; }
            = string.Empty;

        public string OptionC { get; set; }
            = string.Empty;

        public string OptionD { get; set; }
            = string.Empty;
    }


    // =============================================================

    // SIMPLE VIEW MODEL FOR QUIZ RESULTS

    // =============================================================

    public class StudentQuizResult

    {

        public int Id { get; set; }



        public int CourseId { get; set; }



        public string CourseTitle { get; set; }

            = string.Empty;



        public int Score { get; set; }



        public DateTime TakenAt { get; set; }

    }

}