using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
   
    public class Startup
    {
        static void Main(string[] args)
        {
            string uri = "http://localhost:8084/";

            using (WebApp.Start<Startup>(uri))
            {
                Console.WriteLine("Started");
                Console.ReadKey();
                Console.WriteLine("Stopping");
            }

        }

        public void Configuration(IAppBuilder app)
        {
            var config = new MyHttpConfiguration();
            app.UseWebApi(config);

        }
    }

}
