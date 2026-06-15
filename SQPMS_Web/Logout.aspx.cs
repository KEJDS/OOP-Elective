using System;
using System.Web;
using System.Web.Security; // Required for FormsAuthentication

namespace SQPMS
{
    public partial class Logout : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // 1. Clear the Session state
            Session.Clear();
            Session.Abandon();
            Session.RemoveAll();

            // 2. Clear Forms Authentication (if your system uses it)
            FormsAuthentication.SignOut();

            // 3. Force the browser to treat this page as expired and non-cacheable
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);

            // 4. Remove all cookies to ensure no session artifacts persist
            if (Request.Cookies["ASP.NET_SessionId"] != null)
            {
                HttpCookie myCookie = new HttpCookie("ASP.NET_SessionId");
                myCookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(myCookie);
            }

            // 5. Final redirect to the Login page
            Response.Redirect("Login.aspx", true);
        }
    }
}