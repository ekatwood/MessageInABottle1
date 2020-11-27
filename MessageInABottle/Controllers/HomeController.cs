using MessageInABottle.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Security.Claims;
using Microsoft.AspNet.Identity;

namespace MessageInABottle.Controllers
{
    public class HomeController : Controller
    {
        //private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        private string connectionString = "Server=tcp:messageinabottledbserver.database.windows.net,1433;Initial Catalog=MessageInABottle_db;Persist Security Info=False;User ID=ekatwood;Password=eka132EKA;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";


        public ActionResult Index()
        {
            return View();
        }

        //POST: /Home/Home
        [HttpPost]
        public async Task<ActionResult> Index(Messages model)
        {
            if (!Request.IsAuthenticated)
            {
                return Redirect("~/Account/Login");
                
            }


            if(String.IsNullOrEmpty(model.Message))
            {
                ViewBag.MessageType = "alert-danger";
                ViewBag.MessageResponse = "Message can't be blank.";

                return View();
            }

            /* old way of getting username
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.Name);
            model.WrittenBy = claim.Value; 
            */

            try
            {

                //get email address of user
                model.WrittenBy = User.Identity.GetUserName();


                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("TodayCount", connection))
                    {
                        
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@dateadded", SqlDbType.VarChar).Value = DateTime.Today;
                        Debug.WriteLine(DateTime.Today.ToString());
                        command.Parameters.Add("@writtenBy", SqlDbType.VarChar).Value = model.WrittenBy;

                        SqlDataReader r = await command.ExecuteReaderAsync();

                        int counter = 0;

                        while (r.Read())
                        {
                            counter++;
                        }

                        if (counter == 5)
                        {
                            ViewBag.MessageType = "alert-danger";
                            ViewBag.MessageResponse = "You can only send 5 messages a day!";

                            connection.Close();
                            r.Close();
                            return View();
                        }

                        r.Close();

                    }

                    using (var command = new SqlCommand("AddMessage", connection))
                    {
                        
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@Message", SqlDbType.NVarChar).Value = model.Message;
                        command.Parameters.Add("@WrittenBy", SqlDbType.VarChar).Value = model.WrittenBy;

                        await command.ExecuteNonQueryAsync();

                        ViewBag.MessageType = "alert-success";
                        ViewBag.MessageResponse = "Message sent!";

                    }

                    connection.Close();
                }
            } catch (Exception e)
            {
                //send message to DB
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("LogException", connection))
                    {

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@inFunction", SqlDbType.VarChar).Value = "Index (Post)";
                        command.Parameters.Add("@excMessage", SqlDbType.VarChar).Value = e.Message;

                        await command.ExecuteNonQueryAsync();

                    }

                    connection.Close();
                }

                ViewBag.MessageType = "alert-danger";
                ViewBag.MessageResponse = "Error sending message.";

                return View();
            }

            

            return View();
        }
        public async Task<string> DisplayMessage()
        {
            //user id
            string id;

            //returned random message, and written by id
            int messageId = 0;
            string message = "";

            try
            {
                if (String.IsNullOrEmpty(User.Identity.GetUserName()))
                {
                    id = "";
                }
                else
                    id = User.Identity.GetUserName();


                //select random message from database
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("SelectRandom", connection))
                    {
                        await connection.OpenAsync();

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@Id", SqlDbType.VarChar).Value = id;

                        try
                        {
                            SqlDataReader r = await command.ExecuteReaderAsync();

                            r.Read();

                            message = (string)r["Message"];
                            messageId = (int)r["Id"];
                        } catch (Exception e)
                        {
                            Debug.WriteLine(e.Message);
                            message = "There are no new messages at sea. Be the first to write one!";
                        }
                        

                        connection.Close();
                    }
                }
                
            } catch(Exception e)
            {
                //send message to DB
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("LogException", connection))
                    {

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@inFunction", SqlDbType.VarChar).Value = "DisplayMessage()";
                        command.Parameters.Add("@excMessage", SqlDbType.VarChar).Value = e.Message;

                        await command.ExecuteNonQueryAsync();

                    }

                    connection.Close();
                }
            }

            string m = "{\"message\":\"" + message + "\",\"id\":\"" + messageId.ToString() + "\"}";
            return m;
        }

        public async Task<string> KeepBottle(string messageid)
        {
            if (!Request.IsAuthenticated)
            {
                //redirect to log in
                return "{\"errorMessage\":\"\"}";

            }
            string id;

            try
            {
                if (String.IsNullOrEmpty(User.Identity.GetUserName()))
                {
                    id = "";
                }
                else
                    id = User.Identity.GetUserName();


                //select random message from database
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("KeptCount", connection))
                    {

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@datekept", SqlDbType.VarChar).Value = DateTime.Today;
                        Debug.WriteLine(DateTime.Today.ToString());
                        command.Parameters.Add("@ownedBy", SqlDbType.VarChar).Value = User.Identity.GetUserName();
                        Debug.WriteLine(User.Identity.GetUserName());
                        SqlDataReader r = await command.ExecuteReaderAsync();

                        int counter = 0;

                        while (r.Read())
                        {
                            counter++;
                        }

                        if (counter == 5)
                        {
                            connection.Close();
                            r.Close();
                            return "{\"errorMessage\":\"You can only keep 5 messages a day.\"}";
                        }

                        r.Close();

                    }
                    using (var command = new SqlCommand("KeepBottle", connection))
                    {

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@OwnedById", SqlDbType.VarChar).Value = id;
                        command.Parameters.Add("@MessageId", SqlDbType.VarChar).Value = messageid;

                        await command.ExecuteNonQueryAsync();

                        connection.Close();

                        return "{\"errorMessage\":\"Message added to My Bottles!\",\"another\":\"nother\"}"; ;
                    }
                }
            }catch(Exception e)
            {

                //send message to DB
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("LogException", connection))
                    {

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@inFunction", SqlDbType.VarChar).Value = "KeepBottle(string messageid)";
                        command.Parameters.Add("@excMessage", SqlDbType.VarChar).Value = e.Message;

                        await command.ExecuteNonQueryAsync();

                    }

                    connection.Close();
                }

                return "{\"errorMessage\":\"An error occured\"}";
            }
            

        }

        public async Task<string> ReturnBottle(string messageid)
        {
            try
            {
                //select random message from database
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("ReturnBottle", connection))
                    {
                        await connection.OpenAsync();

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@MessageId", SqlDbType.VarChar).Value = messageid;

                        await command.ExecuteNonQueryAsync();

                        connection.Close();

                        return "{\"errorMessage\":\"Bottle returned to sea!\",\"another\":\"nother\"}"; ;
                    }
                }
            }
            catch (Exception e)
            {
                //send message to DB
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("LogException", connection))
                    {

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@inFunction", SqlDbType.VarChar).Value = "ReturnBottle(string messageid)";
                        command.Parameters.Add("@excMessage", SqlDbType.VarChar).Value = e.Message;

                        await command.ExecuteNonQueryAsync();

                    }

                    connection.Close();
                }

                return "{\"errorMessage\":\"An error occured\"}";
            }


        }

        public async Task<ActionResult> MyBottles()
        {
            if (!Request.IsAuthenticated)
            {
                //redirect to log in
                return Redirect("~/Account/Login");

            }

            var id = "";
            var tupleList = new List<(string, int)> { };

            try
            {

                if (String.IsNullOrEmpty(User.Identity.GetUserName()))
                {
                    id = "";
                }
                else
                    id = User.Identity.GetUserName();


                //get bottles owned by user
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("DisplayBottles", connection))
                    {
                        await connection.OpenAsync();

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@OwnedBy", SqlDbType.VarChar).Value = id;

                        SqlDataReader r = await command.ExecuteReaderAsync();

                        //read the results
                        while (r.Read())
                        {
                            tupleList.Add(((string)r["Message"], (int)r["Id"]));
                        }

                        connection.Close();
                    }
                }

                ViewBag.MyBottles = tupleList;


            }
            catch(Exception e)
            {
                //send message to DB
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("LogException", connection))
                    {

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@inFunction", SqlDbType.VarChar).Value = "MyBottles()";
                        command.Parameters.Add("@excMessage", SqlDbType.VarChar).Value = e.Message;

                        await command.ExecuteNonQueryAsync();

                    }

                    connection.Close();
                }
            }

            return View();
        }

        public async Task<ActionResult> UpdateBottles(string rowid)
        {

            try
            {
                //remove selected bottle
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("RemoveBottle", connection))
                    {
                        await connection.OpenAsync();

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@BottleId", SqlDbType.Int).Value = Convert.ToInt32(rowid);

                        await command.ExecuteNonQueryAsync();

                        connection.Close();


                    }
                }

                //reload page
                await MyBottles();

            }
            catch (Exception e)
            {
                //send message to DB
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("LogException", connection))
                    {

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@inFunction", SqlDbType.VarChar).Value = "UpdateBottles(string rowid)";
                        command.Parameters.Add("@excMessage", SqlDbType.VarChar).Value = e.Message;

                        await command.ExecuteNonQueryAsync();

                    }

                    connection.Close();
                }
            }
            
            return  View("MyBottles");
        }

        public async Task<ActionResult> MyMessages()
        {
            if (!Request.IsAuthenticated)
            {
                //redirect to log in
                return Redirect("~/Account/Login");

            }

            var id = "";
            var tupleList = new List<(string, int, bool)> { };

            try
            {

                if (String.IsNullOrEmpty(User.Identity.GetUserName()))
                {
                    id = "";
                }
                else
                    id = User.Identity.GetUserName();


                //select random message from database
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("DisplayMessages", connection))
                    {
                        await connection.OpenAsync();

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@WrittenBy", SqlDbType.VarChar).Value = id;

                        SqlDataReader r = await command.ExecuteReaderAsync();

                        //read the results
                        while (r.Read())
                        {
                            tupleList.Add(((string)r["Message"], (int)r["SeenCount"], (bool)r["KeptBool"]));
                        }

                        connection.Close();
                    }
                }

                ViewBag.MyMessages = tupleList;

            }
            catch(Exception e)
            {
                //send message to DB
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("LogException", connection))
                    {

                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@inFunction", SqlDbType.VarChar).Value = "MyMessages()";
                        command.Parameters.Add("@excMessage", SqlDbType.VarChar).Value = e.Message;

                        await command.ExecuteNonQueryAsync();

                    }

                    connection.Close();
                }
            }

            
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "To get in touch: app.messageinabottle@gmail.com";

            return View();
        }
    }
}