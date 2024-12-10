using CHATAPP.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace CHATAPP.Controllers
{
    public class AccessController : Controller
    {
        private readonly string connectionString;
        public AccessController(string _connectionString)
        {
            connectionString = _connectionString;
        }
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(Login ModelLogin)
        {
            bool validatuser = ValidateUser(ModelLogin.Username, ModelLogin.Password);
            if (validatuser)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, ModelLogin.Username),
                    new Claim(ClaimTypes.Role, "User")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewData["ValidateMessage"] = "Invalid Username Or Password";
                return View();
            }
        }
        [HttpPost]
        public async Task<IActionResult> Signup(Login ModelLogin)
        {
            bool userExists = CheckIfUserExists(ModelLogin.Username);
            if (userExists)
            {
                ViewData["AlreadyExistMessage"] = "Username already exists. Please choose another.";
                return RedirectToAction("Login", "Access");


            }

            // Insert the new user into the database
            bool userCreated = CreateUser(ModelLogin.Username, ModelLogin.Password, ModelLogin.Name);

            if (userCreated)
            {
                // Optionally, log the user in immediately after signup
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, ModelLogin.Username),
                    new Claim(ClaimTypes.Role, "User") // You can add roles or other claims here
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewData["AlreadyExistMessage"] = "An error occurred while creating your account. Please try again.";
                return RedirectToAction("Login", "Access");

            }
        }

        public bool CheckIfUserExists(string username)
        {
            bool exists = false;
            string query = "SELECT COUNT(*) FROM tbl_users WHERE Username = @Username";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);

                        int count = (int)command.ExecuteScalar();
                        if (count > 0)
                        {
                            exists = true; // Username already exists
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }

            return exists;
        }
        public bool CreateUser(string username, string password, string name)
        {
            bool isCreated = false;
            string query = "INSERT INTO tbl_users (Username, Password, Name) VALUES (@Username, @Password, @Name)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password); // This should be hashed in production

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            isCreated = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }

            return isCreated;
        }
        public bool ValidateUser(string username, string password)
        {
            bool isValid = false;
            string query = "SELECT Username, Password FROM tbl_users WHERE Username = @Username AND Password = @Password";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                isValid = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }

            return isValid;
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
    public class Login
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
