using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SQPMS
{
    public partial class UserManagement : System.Web.UI.Page
    {
        // 1. DYNAMIC UI CONTROLS (Required for footer)
        protected global::System.Web.UI.WebControls.Label lblUserInitials;
        protected global::System.Web.UI.WebControls.Label lblUserRole;

        // PlaceHolders for Sidebar Visibility
        protected global::System.Web.UI.WebControls.PlaceHolder phSalesMenu;
        protected global::System.Web.UI.WebControls.PlaceHolder phOperationsMenu;
        protected global::System.Web.UI.WebControls.PlaceHolder phOwnerMenu;

        private string connString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            // KILL BROWSER CACHE
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            // SECURITY ACCESS INTERCEPT ENGINE
            if (Session["UserRole"] == null)
            {
                Response.Redirect("Login.aspx", true);
                return;
            }

            string role = Session["UserRole"].ToString().ToLower().Trim();

            // AUTHORIZATION: Only "admin" can access UserManagement
            if (role != "admin")
            {
                Response.Redirect("Dashboard.aspx", true);
                return;
            }

            // DYNAMIC IDENTITY SYNC
            UpdateSidebarFooter(role);

            // SIDEBAR VISIBILITY
            phSalesMenu.Visible = (role == "admin" || role == "sales");
            phOperationsMenu.Visible = (role == "admin" || role == "operation");
            phOwnerMenu.Visible = (role == "admin");

            if (!IsPostBack)
            {
                BindUsersGrid();
            }
        }

        private void UpdateSidebarFooter(string role)
        {
            string displayRole = char.ToUpper(role[0]) + role.Substring(1);
            if (lblUserRole != null) lblUserRole.Text = displayRole;
            if (lblUserInitials != null) lblUserInitials.Text = role.Length >= 2 ? role.Substring(0, 2).ToUpper() : role.ToUpper();
        }

        private void BindUsersGrid()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "SELECT UserID, Username, Role FROM Users ORDER BY UserID DESC";
                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        gvUsers.DataSource = dt;
                        gvUsers.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Error binding user elements matrix: " + ex.Message;
            }
        }

        protected void btnSaveUser_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Account Username field cannot be blank.";
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = !string.IsNullOrEmpty(hfEditingUserID.Value) ?
                        (!string.IsNullOrWhiteSpace(txtPassword.Text) ?
                            "UPDATE Users SET Username=@Username, Password=@Password, Role=@Role WHERE UserID=@UserID" :
                            "UPDATE Users SET Username=@Username, Role=@Role WHERE UserID=@UserID") :
                        "INSERT INTO Users (Username, Password, Role) VALUES (@Username, @Password, @Role)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        if (!string.IsNullOrEmpty(hfEditingUserID.Value)) cmd.Parameters.AddWithValue("@UserID", hfEditingUserID.Value);
                        cmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim());
                        cmd.Parameters.AddWithValue("@Role", ddlRole.SelectedValue.ToLower().Trim());
                        if (!string.IsNullOrEmpty(txtPassword.Text) || string.IsNullOrEmpty(hfEditingUserID.Value))
                            cmd.Parameters.AddWithValue("@Password", txtPassword.Text.Trim());

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                lblMsg.ForeColor = System.Drawing.Color.Green;
                lblMsg.Text = "User account saved successfully.";
                ResetForm();
                BindUsersGrid();
            }
            catch (Exception ex)
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Error: " + ex.Message;
            }
        }

        protected void gvUsers_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "SelectForEdit")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                GridViewRow row = gvUsers.Rows[index];
                hfEditingUserID.Value = gvUsers.DataKeys[index].Values["UserID"].ToString();
                txtUsername.Text = Server.HtmlDecode(row.Cells[1].Text).Trim();

                Label lblRole = (Label)row.FindControl("lblRole");
                if (lblRole != null) ddlRole.SelectedValue = lblRole.Text.Trim().ToLower();

                btnSaveUser.Text = "Update Operator Permissions";
                btnCancelEdit.Visible = true;
            }
        }

        protected void gvUsers_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            try
            {
                int targetID = Convert.ToInt32(gvUsers.DataKeys[e.RowIndex].Values["UserID"]);
                using (SqlConnection con = new SqlConnection(connString))
                {
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Users WHERE UserID = @UserID", con))
                    {
                        cmd.Parameters.AddWithValue("@UserID", targetID);
                        con.Open(); cmd.ExecuteNonQuery();
                    }
                }
                ResetForm(); BindUsersGrid();
            }
            catch (Exception ex) { lblMsg.Text = "Error: " + ex.Message; }
        }

        protected void btnCancelEdit_Click(object sender, EventArgs e) { ResetForm(); }

        private void ResetForm()
        {
            hfEditingUserID.Value = ""; txtUsername.Text = ""; txtPassword.Text = "";
            ddlRole.SelectedIndex = 0;
            btnSaveUser.Text = "✔ Save User Account";
            btnCancelEdit.Visible = false;
        }
    }
}