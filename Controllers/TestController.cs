using Microsoft.AspNetCore.Mvc;
using APULMS.Models;

namespace APULMS.Controllers
{
    [Route("api/test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly DatabaseHelper _database;

        public TestController(DatabaseHelper database)
        {
            _database = database;
        }

        [HttpGet]
        public IActionResult TestConnection()
        {
            try
            {
                using var connection = _database.GetConnection();

                connection.Open();

                return Ok(new
                {
                    success = true,
                    message = "Database Connected Successfully!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}