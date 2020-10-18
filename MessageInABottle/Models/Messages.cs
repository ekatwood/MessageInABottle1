using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace MessageInABottle.Models
{
    public class Messages
    {
        [Required]
        public string Message { get; set; }

        public string WrittenBy { get; set; }

    }

}