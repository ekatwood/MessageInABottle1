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

namespace MessageInABottle.Controllers
{
    public class HomeController : Controller
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

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
                ViewBag.MessageResponse = "Message can't be blank";

                return View();
            }


            //get email address of user
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.Name);
            model.WrittenBy = claim.Value;

            try
            {
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
                            ViewBag.MessageResponse = "You can only send 5 messages a day";

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
                Debug.WriteLine(e.Message);

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

            try {

                //get email address of user. If it fails, set to empty string (not logged in)
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claim = claimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.Name);
                id = claim.Value;

            } catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                id = "";
            }

            try
            {
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
                Debug.WriteLine(e.Message);
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
            string id = "";
            try
            {
                //get email address of user. If it fails, redirect to login screen
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claim = claimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.Name);
                id = claim.Value;

            }
            catch (Exception e)
            {
                // redirect to log in 
            }

            try
            {
                //select random message from database
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("KeepBottle", connection))
                    {
                        await connection.OpenAsync();

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

                        return "{\"errorMessage\":\"Bottle returned to sea\",\"another\":\"nother\"}"; ;
                    }
                }
            }
            catch (Exception e)
            {
                return "{\"errorMessage\":\"An error occured\"}";
            }


        }

        public async Task<ActionResult> MyBottles()
        {
            var id = "";
            var tupleList = new List<(string, int)> { };

            try
            {
                //get email address of user. If it fails, redirect to login screen
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claim = claimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.Name);
                id = claim.Value;

            }
            catch (Exception e)
            {
                return View("Login");
                // redirect to log in 
            }

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

            return View();
        }

        public async Task<ActionResult> UpdateBottles(string rowid)
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
            return  View("MyBottles");
        }

        public async Task<ActionResult> MyMessages()
        {
            var id = "";
            var tupleList = new List<(string, int, bool)> { };

            try
            {
                //get email address of user. If it fails, redirect to login screen
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claim = claimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.Name);
                id = claim.Value;

            }
            catch (Exception e)
            {
                return View("Login");
                // redirect to log in 
            }


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
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "e.rock.at@gmail.com";

            return View();
        }
    }
}