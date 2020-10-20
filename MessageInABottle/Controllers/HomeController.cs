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
            //get email address of user
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.Name);
            model.WrittenBy = claim.Value;

            Debug.WriteLine(claim.Value.ToString());

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("AddMessage", connection))
                    {
                        await connection.OpenAsync();
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add("@Message", SqlDbType.VarChar).Value = model.Message;
                        command.Parameters.Add("@WrittenBy", SqlDbType.VarChar).Value = model.WrittenBy;

                        await command.ExecuteNonQueryAsync();

                        connection.Close();

                        ViewBag.MessageType = "alert-success";
                        ViewBag.MessageResponse = "Message sent!";

                    }
                }
            } catch (Exception e)
            {
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
                id = "";
            }


            //select random message from database
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand("SelectRandom", connection))
                {
                    await connection.OpenAsync();

                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("@Id", SqlDbType.VarChar).Value = id;

                    SqlDataReader r = await command.ExecuteReaderAsync();

                    r.Read();

                    message = (string)r["Message"];
                    messageId = (int)r["Id"];
                    
                    connection.Close();
                }
            }

            string m = "{\"message\":\""+message+"\",\"id\":\""+messageId.ToString()+"\"}";
            return m;
        }

        public async Task<string> KeepBottle(string messageid)
        {
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

                        return "Message added to Kept Bottles!";
                    }
                }
            }catch(Exception e)
            {
                return "An error occured.";
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

                        return "";
                    }
                }
            }
            catch (Exception e)
            {
                return "";
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