# APULMS — APU Learning Management System

A role-based **Learning Management System (LMS)** built with **ASP.NET Core MVC (.NET 10), C#, Razor, and MySQL**. APULMS brings course discovery, enrolment, quizzes, grading insights, and academic administration into one web application with dedicated interfaces for students, lecturers, and administrators.

> **Project status:** Source-code project for local development and demonstration. The ASP.NET Core project has built successfully with .NET 10 on macOS. Running the complete application requires a local MySQL database; this repository is **not** a hosted/live demo.

## What can APULMS do?

| Role | Main capabilities |
| --- | --- |
| **Guest** | View the public home/about/course pages, use the contact form, and register or sign in. |
| **Student** | Access a student dashboard, browse courses, enrol, take multiple-choice quizzes, review quiz results, and manage a profile. |
| **Lecturer** | Access a lecturer dashboard, view assigned courses and students, create/delete quiz questions, view grades/results, and manage a profile. The assignments section also has its own page. |
| **Administrator** | View platform statistics, manage users and courses, review students/lecturers, access reports, respond to contact enquiries, and manage a profile. |

**Authentication:** Students and lecturers can register themselves. Passwords are hashed with BCrypt. Admin users are not created by the public registration form. User sessions are handled through ASP.NET Core session state.

## Tech stack

- **Backend:** C#, ASP.NET Core MVC, .NET 10
- **Frontend:** Razor (`.cshtml`), HTML, CSS, JavaScript, Bootstrap, jQuery
- **Database:** MySQL 8.0+ (also compatible with the original MySQL 9.x dump)
- **Database access:** MySqlConnector / ADO.NET with SQL commands
- **Password hashing:** BCrypt.Net-Next

## Project structure

```text
APULMS/
├── Controllers/                 # MVC routes and actions (public + role portals)
├── Models/                      # User, Course, database helper, etc.
├── Views/
│   ├── Account/                # Sign-in and registration
│   ├── Admin/                  # Administrator pages
│   ├── Home/                   # Public pages
│   ├── Lecturer/               # Lecturer pages
│   ├── Student/                # Student pages
│   └── Shared/                 # Layouts and reusable Razor views
├── wwwroot/                    # CSS, JavaScript, images, vendor assets
├── database/
│   └── schema.sql              # Empty MySQL schema: no accounts or private data
├── Properties/launchSettings.json
├── APULMS.csproj
├── Program.cs
├── appsettings.example.json    # Template only; no working credentials
└── README.md
```

## Prerequisites

1. [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`dotnet --version` should report `10.x`).
2. [MySQL Server 8.0+](https://dev.mysql.com/downloads/mysql/), running locally.
3. Git and a terminal (macOS, Windows, or Linux). The examples below use macOS/Linux shell syntax.

## Run locally (from a fresh clone)

### 1. Clone and enter the project

```bash
git clone https://github.com/sumanupreti7/APULMS.git
cd APULMS
```

### 2. Create the MySQL database and tables

Start your local MySQL server, then run:

```bash
mysql -u root -p < database/schema.sql
```

Enter your **local MySQL root password** when prompted. This schema creates `apu_lms` with seven tables: `users`, `courses`, `enrollments`, `quizzes`, `quiz_results`, `discussions`, and `contact_messages`.

**Important:** The public schema contains **no real user records, password hashes, contact messages, or default login accounts**. On a fresh setup, the LMS starts with an empty database. To add courses, first create an admin account as explained below.

If `mysql: command not found`, install the MySQL command-line client or add it to your `PATH`.

### 3. Configure the database connection

Create your own local configuration file from the included template:

```bash
cp appsettings.example.json appsettings.json
```

In `appsettings.json`, update the `DefaultConnection` value to match your MySQL credentials and the `apu_lms` database:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=apu_lms;User=root;Password=YOUR_LOCAL_MYSQL_PASSWORD;"
  }
}
```

The JSON above only illustrates the relevant setting; keep the `Logging` and `AllowedHosts` settings in the copied template. If your MySQL root account has no password, use `Password=;` instead. For shared machines or deployed environments, prefer a dedicated MySQL user with limited permissions.

**Never commit your actual `appsettings.json` or passwords.** The local configuration and uploaded profile photos are excluded by `.gitignore`. If you already have a working `appsettings.json`, **do not overwrite it**.

### 4. Restore, build, and run

```bash
dotnet restore
dotnet build
dotnet run --launch-profile http
```

The HTTP launch profile is configured for:

**http://localhost:5128**

Open that address in your browser. Press **Ctrl+C** in the terminal to stop the server. The app's HTTPS launch profile is also available if you prefer HTTPS and have configured development certificates.

### 5. Create local test accounts

1. Visit **Register** in the app and create a **Student** or **Lecturer** account.
2. Sign in with that account to explore its dashboard.
3. To use the **Administrator** dashboard on your *own local test database*, first register an account normally, then promote only that account with a local MySQL command:

```bash
mysql -u root -p apu_lms -e "UPDATE users SET role='admin' WHERE email='YOUR_OWN_TEST_EMAIL@example.com';"
```

Sign out and sign in again to refresh your session role. This uses the account's own BCrypt password; it does not publish or create a shared default administrator password. Only run the promotion command against a database you administer.

4. As an admin, create courses; as a lecturer, add quizzes to assigned courses; as a student, enrol and attempt quizzes.

## Database tables

| Table | Purpose |
| --- | --- |
| `users` | Student, lecturer, and administrator accounts |
| `courses` | Course details and lecturer association |
| `enrollments` | Student enrolments and progress |
| `quizzes` | Multiple-choice questions and correct answers |
| `quiz_results` | Recorded quiz scores |
| `discussions` | Database structure for course-related discussion posts |
| `contact_messages` | Public enquiries and administrator responses |

The schema is in [`database/schema.sql`](database/schema.sql). It contains **table definitions only**; it is not a copy of a live/production database. The application uses direct SQL via MySqlConnector rather than Entity Framework migrations.

## Troubleshooting

| Problem | What to check |
| --- | --- |
| `dotnet: command not found` | Install the **.NET 10 SDK** and reopen Terminal. |
| `mysql: command not found` | Install MySQL client tools or update your shell `PATH`. |
| `Access denied for user` | Check the MySQL username/password in the **local** `appsettings.json`. |
| `Unable to connect to localhost:3306` | Confirm MySQL is running and listening on the configured host/port. |
| Pages load but lists are empty | The public schema intentionally includes no sample users or courses. Create test accounts and courses. |
| Port 5128 is in use | Stop the conflicting process, or run with `dotnet run --urls http://localhost:5005` and open that URL. |

## Privacy and repository hygiene

- Local connection settings (`appsettings.json`, `appsettings.Development.json`) are excluded from Git.
- Generated files (`bin/`, `obj/`) and uploaded user profile photos (`wwwroot/uploads/`) are not included.
- Do **not** publish an unfiltered database dump containing email addresses, password hashes, private messages, or other user information.
- This README documents local setup, not production hardening or a security audit.

## Project author

Developed by [sumanupreti7](https://github.com/sumanupreti7).

---

*APULMS is a learning-management project demonstrating server-rendered ASP.NET Core MVC and a role-based MySQL-backed application.*
