using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Todo.Models;

namespace Todo.Controllers
{
    [ApiController]
    public class AccountApiController : ControllerBase
    {
        private const string ConnectionString = "Data Source=db.sqlite";

        [HttpPost]
        [Route("Home/ApiLogin")]
        public IActionResult Login([FromBody] User user)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM Users WHERE Username = '{user.Username}' AND Password = '{user.Password}'";
            long count = (long)cmd.ExecuteScalar();

            if (count > 0)
                return Ok(new { success = true, message = "Login successful" });

            return BadRequest(new { success = false, message = "Invalid credentials" });
        }

        [HttpPost]
        [Route("Home/ApiRegister")]
        public IActionResult Register([FromBody] User user)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = $"SELECT COUNT(*) FROM Users WHERE Username = '{user.Username}'";
            long count = (long)checkCmd.ExecuteScalar();

            if (count > 0)
                return Conflict(new { success = false, message = "User already exists" });

            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = $"INSERT INTO Users (Username, Password) VALUES ('{user.Username}', '{user.Password}')";
            insertCmd.ExecuteNonQuery();

            return Ok(new { success = true, message = "Registration successful" });
        }

        [HttpPost]
        [Route("Home/ApiAddNote")]
        public IActionResult AddTodo([FromBody] TodoItem item, [FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username))
                return BadRequest(new { success = false, message = "Username is required" });

            using var con = new SqliteConnection(ConnectionString);
            con.Open();

            var cmd = con.CreateCommand();
            cmd.CommandText = $"INSERT INTO todo (name, username) VALUES ('{item.Name}', '{username}')";
            cmd.ExecuteNonQuery();

            return Ok(new { success = true, message = "Todo added successfully" });
        }

        [HttpPost]
        [Route("Home/ApiUpdateNote")]
        public IActionResult UpdateTodo([FromBody] TodoItem item, [FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username))
                return BadRequest(new { success = false, message = "Username is required" });

            using var con = new SqliteConnection(ConnectionString);
            con.Open();

            var cmd = con.CreateCommand();
            cmd.CommandText = $"UPDATE todo SET name = @name WHERE Id = @id AND username = @username";
            cmd.Parameters.AddWithValue("@id", item.Id);
            cmd.Parameters.AddWithValue("@name", item.Name);
            cmd.Parameters.AddWithValue("@username", username);

            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected == 0)
                return NotFound(new { success = false, message = "Todo not found or not owned by user" });

            return Ok(new { success = true, message = "Todo updated successfully" });
        }

        [HttpPost]
        [Route("Home/ApiDeleteNote")]
        public IActionResult DeleteTodo([FromQuery] int id, [FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username))
                return BadRequest(new { success = false, message = "Username is required" });

            using var con = new SqliteConnection(ConnectionString);
            con.Open();

            var cmd = con.CreateCommand();
            cmd.CommandText = $"DELETE FROM todo WHERE Id = @id AND username = @username";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@username", username);

            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected == 0)
                return NotFound(new { success = false, message = "Todo not found or not owned by user" });

            return Ok(new { success = true, message = "Todo deleted successfully" });
        }
    }
}
