using CHATAPP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CHATAPP.Controllers
{
    public class ChatController : Controller
    {
        private string connectionString = "Data Source=Hamza;Initial Catalog=CHATAPP;Integrated Security=True;TrustServerCertificate=True;";

        public IActionResult Index()
        {
            string receiverUsername = Request.Query["receiverUsername"];
            string senderUsername = User.Identity.Name;

            // Retrieve messages between sender and receiver, ordered by date
            List<Message> messages = GetMessages(senderUsername, receiverUsername);
            ViewData["ReceiverUsername"] = receiverUsername; // Pass the receiver's username to the view

            return View(messages);
        }

        public List<Message> GetMessages(string sender, string receiver)
        {
            List<Message> messages = new List<Message>();

            // Update query to filter messages between the sender and receiver, and order them by date
            string query = @"
                SELECT * 
                FROM tbl_messages 
                WHERE (senderusername = @sender AND receiverusername = @receiver) 
                   OR (senderusername = @receiver AND receiverusername = @sender) 
                ORDER BY Date ASC";  // Orders messages by date (ascending)

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add parameters to avoid SQL injection
                        command.Parameters.AddWithValue("@sender", sender);
                        command.Parameters.AddWithValue("@receiver", receiver);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Mapping data from reader to Message model
                                Message message = new Message
                                {
                                    SenderUsername = reader["senderusername"].ToString(),
                                    ReceiverUsername = reader["receiverusername"].ToString(),
                                    Date = Convert.ToDateTime(reader["Date"]),
                                    Text = reader["Text"].ToString()
                                };

                                messages.Add(message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }

            return messages;
        }

        // Add SendMessage function to insert a message
        [HttpPost]
        public IActionResult SendMessage(string senderUsername, string receiverUsername, string messageText)
        {
            // Insert the message into the database
            InsertMessage(senderUsername, receiverUsername, messageText);

            // Redirect to the Index action to reload messages
            return RedirectToAction("Index", new { receiverUsername = receiverUsername });
        }

        private void InsertMessage(string sender, string receiver, string messageText)
        {
            string query = "INSERT INTO tbl_messages (senderusername, receiverusername, text, Date) VALUES (@sender, @receiver, @text, @date)";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@sender", sender);
                    command.Parameters.AddWithValue("@receiver", receiver);
                    command.Parameters.AddWithValue("@text", messageText);
                    command.Parameters.AddWithValue("@date", DateTime.Now);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
