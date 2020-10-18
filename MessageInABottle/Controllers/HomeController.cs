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
        public async Task<ActionResult> Index(MessageDB model)
        {
            //get email address of user
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.Name);
            model.WrittenBy = claim.Value;

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
        public string DisplayMessage()
        {
            return "MESSAGE";

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