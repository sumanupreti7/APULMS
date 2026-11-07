using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using APULMS.Models;

namespace APULMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly DatabaseHelper _db;

        public AccountController(DatabaseHelper db)
        {
            _db = db;
        }

        // =========================================================
        // REGISTER - GET
        // =========================================================
        [HttpGet]
        public IActionResult Register()
        {
            return View(new User());
        }


        // =========================================================
        // REGISTER - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user)
        {
            // ---------------------------------------------------------
            // Public registration supports Student and Lecturer only.
            // Administrator accounts cannot be created publicly.
            // ---------------------------------------------------------

            user.Role = (user.Role ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            if (user.Role != "student" &&
                user.Role != "lecturer")
            {
                ModelState.AddModelError(
                    nameof(user.Role),
                    "Please select Student or Lecturer."
                );
            }

            // ---------------------------------------------------------
            // Validate name
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(user.Name))
            {
                ModelState.AddModelError(
                    nameof(user.Name),
                    "Full name is required."
                );
            }

            // ---------------------------------------------------------
            // Validate confirm password
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(user.ConfirmPassword))
            {
                ModelState.AddModelError(
                    nameof(user.ConfirmPassword),
                    "Please confirm your password."
                );
            }
            else if (user.Password != user.ConfirmPassword)
            {
                ModelState.AddModelError(
                    nameof(user.ConfirmPassword),
                    "Passwords do not match."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                // -----------------------------------------------------
                // Check whether email already exists
                // -----------------------------------------------------

                const string checkSql = @"
                    SELECT COUNT(*)
                    FROM users
                    WHERE email = @email";

                using (var checkCommand =
                    new MySqlCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue(
                        "@email",
                        user.Email.Trim()
                    );

                    int existingUsers =
                        Convert.ToInt32(
                            checkCommand.ExecuteScalar()
                        );

                    if (existingUsers > 0)
                    {
                        ViewBag.Error =
                            "An account with this email address already exists.";

                        return View(user);
                    }
                }

                // -----------------------------------------------------
                // Hash password
                // -----------------------------------------------------

                string hashedPassword =
                    BCrypt.Net.BCrypt.HashPassword(user.Password);

                // -----------------------------------------------------
                // Insert Student or Lecturer
                // -----------------------------------------------------

                const string insertSql = @"
                    INSERT INTO users
                        (name, email, password, role)
                    VALUES
                        (@name, @email, @password, @role)";

                using var insertCommand =
                    new MySqlCommand(insertSql, connection);

                insertCommand.Parameters.AddWithValue(
                    "@name",
                    user.Name.Trim()
                );

                insertCommand.Parameters.AddWithValue(
                    "@email",
                    user.Email.Trim()
                );

                insertCommand.Parameters.AddWithValue(
                    "@password",
                    hashedPassword
                );

                insertCommand.Parameters.AddWithValue(
                    "@role",
                    user.Role
                );

                int rowsAffected =
                    insertCommand.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    TempData["Success"] =
                        user.Role == "lecturer"
                            ? "Lecturer account created successfully. You can now sign in."
                            : "Student account created successfully. You can now sign in.";

                    return RedirectToAction("Login");
                }

                ViewBag.Error =
                    "Unable to create your account. Please try again.";

                return View(user);
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(
                    $"REGISTER DATABASE ERROR: {ex.Message}"
                );

                ViewBag.Error =
                    "A database error occurred while creating your account.";

                return View(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"REGISTER ERROR: {ex.Message}"
                );

                ViewBag.Error =
                    "Something went wrong. Please try again.";

                return View(user);
            }
        }


        // =========================================================
        // LOGIN - GET
        // =========================================================
        [HttpGet]
        public IActionResult Login()
        {
            string? userId =
                HttpContext.Session.GetString("UserId");

            string? role =
                HttpContext.Session.GetString("Role");

            if (!string.IsNullOrEmpty(userId) &&
                !string.IsNullOrEmpty(role))
            {
                return RedirectToDashboard(role);
            }

            return View(new User());
        }


        // =========================================================
        // LOGIN - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(User user)
        {
            // Login only requires email and password.
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError(
                    nameof(user.Email),
                    "Email address is required."
                );
            }

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                ModelState.AddModelError(
                    nameof(user.Password),
                    "Password is required."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            try
            {
                using var connection = _db.GetConnection();
                connection.Open();

                const string sql = @"
                    SELECT
                        id,
                        name,
                        email,
                        password,
                        role
                    FROM users
                    WHERE email = @email
                    LIMIT 1";

                using var command =
                    new MySqlCommand(sql, connection);

                command.Parameters.AddWithValue(
                    "@email",
                    user.Email.Trim()
                );

                int id;
                string name;
                string email;
                string storedPassword;
                string role;

                // -------------------------------------------------
                // Read account information
                // -------------------------------------------------
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        ViewBag.Error =
                            "Invalid email address or password.";

                        return View(user);
                    }

                    id = reader.GetInt32("id");

                    name =
                        reader.IsDBNull(
                            reader.GetOrdinal("name")
                        )
                            ? string.Empty
                            : reader.GetString("name");

                    email =
                        reader.GetString("email");

                    storedPassword =
                        reader.GetString("password");

                    role =
                        reader.GetString("role");
                }

                // -------------------------------------------------
                // Verify password
                //
                // Supports:
                // 1. New BCrypt passwords
                // 2. Existing plain-text passwords
                //
                // Existing plain-text passwords are automatically
                // upgraded to BCrypt after successful login.
                // -------------------------------------------------

                bool passwordValid = false;
                bool needsMigration = false;

                // BCrypt hashes normally start with:
                // $2a$, $2b$, or $2y$
                bool isBcryptHash =
                    storedPassword.StartsWith("$2a$") ||
                    storedPassword.StartsWith("$2b$") ||
                    storedPassword.StartsWith("$2y$");

                if (isBcryptHash)
                {
                    try
                    {
                        passwordValid =
                            BCrypt.Net.BCrypt.Verify(
                                user.Password,
                                storedPassword
                            );
                    }
                    catch
                    {
                        passwordValid = false;
                    }
                }
                else
                {
                    // Existing APULMS plain-text account
                    passwordValid =
                        storedPassword == user.Password;

                    if (passwordValid)
                    {
                        needsMigration = true;
                    }
                }

                if (!passwordValid)
                {
                    ViewBag.Error =
                        "Invalid email address or password.";

                    return View(user);
                }

                // -------------------------------------------------
                // Automatically migrate old plain-text password
                // to BCrypt after successful login
                // -------------------------------------------------
                if (needsMigration)
                {
                    string newHashedPassword =
                        BCrypt.Net.BCrypt.HashPassword(
                            user.Password
                        );

                    const string updatePasswordSql = @"
                        UPDATE users
                        SET password = @password
                        WHERE id = @id";

                    using var updateCommand =
                        new MySqlCommand(
                            updatePasswordSql,
                            connection
                        );

                    updateCommand.Parameters.AddWithValue(
                        "@password",
                        newHashedPassword
                    );

                    updateCommand.Parameters.AddWithValue(
                        "@id",
                        id
                    );

                    updateCommand.ExecuteNonQuery();
                }

                // -------------------------------------------------
                // Create session
                // -------------------------------------------------
                HttpContext.Session.SetString(
                    "UserId",
                    id.ToString()
                );

                HttpContext.Session.SetString(
                    "UserName",
                    name
                );

                HttpContext.Session.SetString(
                    "UserEmail",
                    email
                );

                HttpContext.Session.SetString(
                    "Role",
                    role.ToLower()
                );

                return RedirectToDashboard(role);
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(
                    $"LOGIN DATABASE ERROR: {ex.Message}"
                );

                ViewBag.Error =
                    "Unable to connect to the database. Please try again.";

                return View(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"LOGIN ERROR: {ex.Message}"
                );

                ViewBag.Error =
                    "Something went wrong while signing in.";

                return View(user);
            }
        }


        // =========================================================
        // LOGOUT
        // =========================================================
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["Success"] =
                "You have been logged out successfully.";

            return RedirectToAction("Login");
        }


        // =========================================================
        // ROLE-BASED DASHBOARD REDIRECT
        // =========================================================
        private IActionResult RedirectToDashboard(string role)
        {
            switch (role.ToLower())
            {
                case "admin":
                    return RedirectToAction(
                        "Dashboard",
                        "Admin"
                    );

                case "lecturer":
                    return RedirectToAction(
                        "Dashboard",
                        "Lecturer"
                    );

                case "student":
                    return RedirectToAction(
                        "Dashboard",
                        "Student"
                    );

                default:
                    HttpContext.Session.Clear();

                    ViewBag.Error =
                        "Your account has an invalid user role.";

                    return View(
                        "Login",
                        new User()
                    );
            }
        }
    }
}