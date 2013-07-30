using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Service
{
    public class Greeting
    {
        public string Text { get; set; }
    }

    public class MessageController : ApiController
    {
        public Greeting Get()
        {
            return new Greeting
            {
                Text = "Hello, World!"
            };
        }
    }

}
