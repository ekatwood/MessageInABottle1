using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MessageInABottle.Startup))]
namespace MessageInABottle
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
