using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;

namespace SQPMS
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string connString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connString))
            {
                string query = "SELECT Username, Role FROM Users WHERE Username=@u AND Password=@p";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@u", txtUser.Text.Trim());
                cmd.Parameters.AddWithValue("@p", txtPass.Text.Trim());

                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        Session["Username"] = dr["Username"].ToString().Trim();
                        Session["UserRole"] = dr["Role"].ToString().ToLower().Trim();
                        Response.Redirect("Dashboard.aspx", true);
                    }
                    else
                    {
                        lblMsg.Text = "Invalid username or password.";
                    }
                }
            }
        }
    }
}