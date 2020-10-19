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

        public ActionResult MyBottles()
        {
            
            return View();
        }

        public ActionResult MyMessages()
        {
            
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "e.rock.at@gmail.com";

            return View();
        }
    }
}