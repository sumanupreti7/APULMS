using Microsoft.AspNetCore.Mvc;
using APULMS.Models;

namespace APULMS.Controllers
{
    public class DatabaseController : Controller
    {

        private readonly DatabaseHelper _databaseHelper;


        public DatabaseController(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }


        public IActionResult Index()
        {

            try
            {
                using var connection = _databaseHelper.GetConnection();

                connection.Open();

                return Content("Database Connected Successfully!");

            }
            catch(Exception ex)
            {
                return Content(
                    "Database Connection Failed: " 
                    + ex.Message
                );
            }

        }

    }
}