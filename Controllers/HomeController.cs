using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using APULMS.Models;

namespace APULMS.Controllers;

public class HomeController : Controller
{
    private readonly DatabaseHelper _databaseHelper;

    public HomeController(DatabaseHelper databaseHelper)
    {
        _databaseHelper = databaseHelper;
    }


    // =========================================================
    // HOME
    // =========================================================

    public IActionResult Index()
    {
        return View();
    }


    // =========================================================
    // ABOUT
    // =========================================================

    public IActionResult About()
    {
        return View();
    }


    // =========================================================
    // PUBLIC / GUEST COURSES
    // =========================================================

    public IActionResult Courses()
    {
        var courses = new List<GuestCourse>();

        try
        {
            using var connection =
                _databaseHelper.GetConnection();

            connection.Open();

            const string sql = @"
                SELECT
                    c.id,
                    c.title,
                    c.description,
                    c.category,
                    c.thumbnail,
                    u.name AS lecturer_name
                FROM courses c
                LEFT JOIN users u
                    ON c.lecturer_id = u.id
                ORDER BY
                    c.created_at DESC,
                    c.id DESC";

            using var command =
                new MySqlCommand(sql, connection);

            using var reader =
                command.ExecuteReader();

            while (reader.Read())
            {
                courses.Add(new GuestCourse
                {
                    Id =
                        reader.GetInt32("id"),

                    Title =
                        reader.IsDBNull(
                            reader.GetOrdinal("title"))
                            ? "Untitled Course"
                            : reader.GetString("title"),

                    Description =
                        reader.IsDBNull(
                            reader.GetOrdinal("description"))
                            ? "Course information is available after registration."
                            : reader.GetString("description"),

                    Category =
                        reader.IsDBNull(
                            reader.GetOrdinal("category"))
                            ? "General"
                            : reader.GetString("category"),

                    Thumbnail =
                        reader.IsDBNull(
                            reader.GetOrdinal("thumbnail"))
                            ? null
                            : reader.GetString("thumbnail"),

                    LecturerName =
                        reader.IsDBNull(
                            reader.GetOrdinal("lecturer_name"))
                            ? "To be assigned"
                            : reader.GetString("lecturer_name")
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"PUBLIC COURSES ERROR: {ex.Message}"
            );

            ViewBag.CourseError =
                "Course information is temporarily unavailable. Please try again later.";
        }

        return View(courses);
    }


    // =========================================================
    // CONTACT / GUEST INQUIRY - GET
    // =========================================================

    [HttpGet]
    public IActionResult Contact()
    {
        return View();
    }


    // =========================================================
    // CONTACT / GUEST INQUIRY - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(
        string name,
        string email,
        string subject,
        string message)
    {
        // -----------------------------------------------------
        // VALIDATE REQUIRED FIELDS
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(message))
        {
            TempData["ContactError"] =
                "Please complete all fields before sending your inquiry.";

            return RedirectToAction(nameof(Contact));
        }


        // -----------------------------------------------------
        // CLEAN INPUT
        // -----------------------------------------------------

        name = name.Trim();
        email = email.Trim();
        subject = subject.Trim();
        message = message.Trim();


        // -----------------------------------------------------
        // LENGTH VALIDATION
        // -----------------------------------------------------

        if (name.Length > 100)
        {
            TempData["ContactError"] =
                "Full name cannot exceed 100 characters.";

            return RedirectToAction(nameof(Contact));
        }

        if (email.Length > 150)
        {
            TempData["ContactError"] =
                "Email address cannot exceed 150 characters.";

            return RedirectToAction(nameof(Contact));
        }

        if (subject.Length > 200)
        {
            TempData["ContactError"] =
                "Subject cannot exceed 200 characters.";

            return RedirectToAction(nameof(Contact));
        }


        // -----------------------------------------------------
        // BASIC EMAIL VALIDATION
        // -----------------------------------------------------

        try
        {
            var emailAddress =
                new System.Net.Mail.MailAddress(email);

            if (!string.Equals(
                    emailAddress.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ContactError"] =
                    "Please enter a valid email address.";

                return RedirectToAction(nameof(Contact));
            }
        }
        catch
        {
            TempData["ContactError"] =
                "Please enter a valid email address.";

            return RedirectToAction(nameof(Contact));
        }


        // -----------------------------------------------------
        // SAVE GUEST INQUIRY TO MYSQL
        // -----------------------------------------------------

        try
        {
            using var connection =
                _databaseHelper.GetConnection();

            connection.Open();

            const string sql = @"
                INSERT INTO contact_messages
                (
                    name,
                    email,
                    subject,
                    message,
                    status
                )
                VALUES
                (
                    @name,
                    @email,
                    @subject,
                    @message,
                    'new'
                )";

            using var command =
                new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@name",
                name
            );

            command.Parameters.AddWithValue(
                "@email",
                email
            );

            command.Parameters.AddWithValue(
                "@subject",
                subject
            );

            command.Parameters.AddWithValue(
                "@message",
                message
            );

            int rowsAffected =
                command.ExecuteNonQuery();


            // -------------------------------------------------
            // SUCCESS
            // -------------------------------------------------

            if (rowsAffected > 0)
            {
                TempData["ContactSuccess"] =
                    "Your inquiry has been sent successfully. The administration team can now review your message.";

                return RedirectToAction(nameof(Contact));
            }


            TempData["ContactError"] =
                "Your inquiry could not be sent. Please try again.";

            return RedirectToAction(nameof(Contact));
        }
        catch (MySqlException ex)
        {
            Console.WriteLine(
                $"CONTACT DATABASE ERROR: {ex.Message}"
            );

            TempData["ContactError"] =
                "Your inquiry could not be saved at this time. Please try again.";

            return RedirectToAction(nameof(Contact));
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"CONTACT ERROR: {ex.Message}"
            );

            TempData["ContactError"] =
                "Something went wrong while sending your inquiry.";

            return RedirectToAction(nameof(Contact));
        }
    }


    // =========================================================
    // PRIVACY
    // =========================================================

    public IActionResult Privacy()
    {
        return View();
    }


    // =========================================================
    // DATABASE TEST
    // =========================================================

    public IActionResult TestDatabase()
    {
        try
        {
            using var connection =
                _databaseHelper.GetConnection();

            connection.Open();

            return Content(
                "Database Connected Successfully!"
            );
        }
        catch (Exception ex)
        {
            return Content(
                "Database Connection Failed\n\n" +
                ex.Message
            );
        }
    }


    // =========================================================
    // ERROR
    // =========================================================

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId =
                Activity.Current?.Id ??
                HttpContext.TraceIdentifier
        });
    }
}


// =============================================================
// PUBLIC / GUEST COURSE MODEL
// =============================================================

public class GuestCourse
{
    public int Id { get; set; }

    public string Title { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public string Category { get; set; } =
        string.Empty;

    public string? Thumbnail { get; set; }

    public string LecturerName { get; set; } =
        string.Empty;
}