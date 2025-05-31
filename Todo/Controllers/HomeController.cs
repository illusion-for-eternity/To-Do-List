using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Todo.Models;
using Todo.Models.ViewModels;

namespace Todo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private const string ConnectionString = "Data Source=db.sqlite";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            CreateUsersTable();
            CreateTodoTable(); // unit testing
        }

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");

            if (username == null)
                return RedirectToAction("Login");

            var todoListViewModel = GetAllTodos(username);
            return View(todoListViewModel);
        }
        //for unit testing
        private void CreateTodoTable()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string sql = @"CREATE TABLE IF NOT EXISTS todo (
                      Id INTEGER PRIMARY KEY AUTOINCREMENT,
                      Name TEXT NOT NULL,
                      Username TEXT NOT NULL
                  );";

            using var command = new SqliteCommand(sql, connection);
            command.ExecuteNonQuery();
        }



        [HttpGet]
        public JsonResult PopulateForm(int id)
        {
            var todo = GetById(id);
            return Json(todo);
        }

        internal TodoViewModel GetAllTodos(string username)
        {
            List<TodoItem> todoList = new();
            using var con = new SqliteConnection(ConnectionString);
            using var tableCmd = con.CreateCommand();
            con.Open();
            tableCmd.CommandText = $"SELECT * FROM todo WHERE username = '{username}'";

            using var reader = tableCmd.ExecuteReader();
            while (reader.Read())
            {
                todoList.Add(new TodoItem
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }

            return new TodoViewModel { TodoList = todoList };
        }



        internal TodoItem GetById(int id)
        {
            TodoItem todo = new();
            using var connection = new SqliteConnection(ConnectionString);
            using var tableCmd = connection.CreateCommand();
            connection.Open();
            tableCmd.CommandText = $"SELECT * FROM todo WHERE Id = '{id}'";

            using var reader = tableCmd.ExecuteReader();
            if (reader.Read())
            {
                todo.Id = reader.GetInt32(0);
                todo.Name = reader.GetString(1);
            }
            return todo;
        }

        public RedirectResult Insert(TodoItem todo)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("Insert attempt without session user");
                
                return Redirect("/Home/Login");
            }
            using var con = new SqliteConnection(ConnectionString);
            using var tableCmd = con.CreateCommand();
            con.Open();
            tableCmd.CommandText = $"INSERT INTO todo (name, username) VALUES ('{todo.Name}', '{username}')";
            tableCmd.ExecuteNonQuery();

            _logger.LogInformation($"Todo item inserted for user {username}: {todo.Name}");

            return Redirect("/");
        }


        [HttpPost]
        public JsonResult Delete(int id)
        {
            using var con = new SqliteConnection(ConnectionString);
            using var tableCmd = con.CreateCommand();
            con.Open();
            tableCmd.CommandText = $"DELETE FROM todo WHERE Id = '{id}'";
            tableCmd.ExecuteNonQuery();

            _logger.LogInformation($"Todo item with ID {id} was deleted");

            return Json(new { });
        }

        public RedirectResult Update(TodoItem todo)
        {
            using var con = new SqliteConnection(ConnectionString);
            using var tableCmd = con.CreateCommand();
            con.Open();
            tableCmd.CommandText = $"UPDATE todo SET name = '{todo.Name}' WHERE Id = '{todo.Id}'";
            tableCmd.ExecuteNonQuery();

            _logger.LogInformation($"Todo item updated: ID={todo.Id}, NewName={todo.Name}");

            return Redirect("/");
        }

        private void CreateUsersTable()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string sql = @"CREATE TABLE IF NOT EXISTS Users (
                              Username TEXT PRIMARY KEY,
                              Password TEXT NOT NULL
                          );";
            using var command = new SqliteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = $"SELECT COUNT(*) FROM Users WHERE Username = '{user.Username}'";
            long count = (long)checkCmd.ExecuteScalar();

            if (count > 0)
            {
                ViewBag.Error = "User already exists!";
                return View();
            }

            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = $"INSERT INTO Users (Username, Password) VALUES ('{user.Username}', '{user.Password}')";
            insertCmd.ExecuteNonQuery();

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(User user)
        {
            _logger.LogInformation($"Login attempt: {user.Username}");

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM Users WHERE Username = '{user.Username}' AND Password = '{user.Password}'";
            long count = (long)cmd.ExecuteScalar();

            if (count > 0)
            {
                _logger.LogInformation($"User {user.Username} logged in successfully");
                HttpContext.Session.SetString("Username", user.Username);  // Зберігаємо ім'я користувача в сесії
                return RedirectToAction("Index");
            }
            else
            {
                _logger.LogWarning($"Failed login for user: {user.Username}");
                ViewBag.Error = "Incorrect login or password! Try again.";
                return View();
            }
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();  // Очищаємо сесію
            return RedirectToAction("Login");
        }

    }
}
