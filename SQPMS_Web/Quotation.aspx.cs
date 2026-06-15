using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SQPMS
{
    public partial class Quotations : System.Web.UI.Page
    {
        protected global::System.Web.UI.WebControls.Label lblUserInitials;
        protected global::System.Web.UI.WebControls.Label lblUserRole;

        private string connString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            if (Session["UserRole"] == null)
            {
                Response.Redirect("Login.aspx", true);
                return;
            }

            string role = Session["UserRole"].ToString().ToLower().Trim();

            if (role != "admin" && role != "sales")
            {
                Response.Redirect("Dashboard.aspx", true);
                return;
            }

            UpdateSidebarFooter(role);

            phSalesMenu.Visible = (role == "admin" || role == "sales");
            phOperationsMenu.Visible = (role == "admin" || role == "operation");
            phOwnerMenu.Visible = (role == "admin");

            if (!IsPostBack)
            {
                BindClientDropdown();
                BindQuotationsGrid();
            }
        }

        private void UpdateSidebarFooter(string role)
        {
            string displayRole = char.ToUpper(role[0]) + role.Substring(1);
            lblUserRole.Text = displayRole;
            lblUserInitials.Text = role.Length >= 2 ? role.Substring(0, 2).ToUpper() : role.ToUpper();
        }

        private void BindClientDropdown()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "SELECT ClientID, CompanyName FROM Clients WHERE Status='Active' ORDER BY CompanyName ASC";
                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        ddlClients.DataSource = dt;
                        ddlClients.DataTextField = "CompanyName";
                        ddlClients.DataValueField = "ClientID";
                        ddlClients.DataBind();
                    }
                }
                ddlClients.Items.Insert(0, new ListItem("-- Select a Client --", ""));
            }
            catch (Exception ex) { lblMsg.Text = "Error loading clients: " + ex.Message; }
        }

        private void BindQuotationsGrid()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = @"
            SELECT 
                q.QuotationID, 
                q.ClientID,
                c.CompanyName AS ClientName, 
                q.ProjectName, 
                ISNULL((
                    SELECT SUM(CAST(o.Cost AS DECIMAL(18,2)) * CAST(o.Qty AS DECIMAL(18,2)) + ISNULL(TRY_CAST(o.Customize AS DECIMAL(18,2)), 0)) 
                    FROM Orders o 
                    -- FIX: We now filter by QuotationID instead of ClientID
                    WHERE o.QuotationID = q.QuotationID 
                    AND o.Status <> 'Cancelled'
                ), 0) AS TotalAmount, 
                q.Status, 
                q.DateCreated
            FROM Quotations q
            INNER JOIN Clients c ON q.ClientID = c.ClientID
            ORDER BY q.QuotationID DESC";

                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        gvQuotations.DataSource = dt;
                        gvQuotations.DataBind();
                    }
                }
            }
            catch (Exception ex) { lblMsg.Text = "Error loading quotations: " + ex.Message; }
        }

        protected void gvQuotations_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                Label lblStatus = (Label)e.Row.FindControl("lblGridStatus");
                if (lblStatus != null)
                {
                    string status = lblStatus.Text.Trim();
                    if (status == "Approved")
                        lblStatus.CssClass = "badge-approved";
                    else if (status == "Rejected")
                        lblStatus.CssClass = "badge-rejected";
                    else
                        lblStatus.CssClass = "badge-pending";
                }
            }
        }

        protected void btnSaveQuotation_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddlClients.SelectedValue)) { lblMsg.ForeColor = System.Drawing.Color.Red; lblMsg.Text = "Please select a client target layout profile."; return; }
            if (string.IsNullOrWhiteSpace(txtProjectName.Text)) { lblMsg.ForeColor = System.Drawing.Color.Red; lblMsg.Text = "Project Description label cannot be empty."; return; }

            int clientID = Convert.ToInt32(ddlClients.SelectedValue);
            string targetStatus = ddlStatus.SelectedValue;

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            string query = !string.IsNullOrEmpty(hfEditingQuotationID.Value) ?
                                "UPDATE Quotations SET ClientID=@ClientID, ProjectName=@ProjectName, Status=@Status WHERE QuotationID=@QuotationID" :
                                "INSERT INTO Quotations (ClientID, ProjectName, Status, DateCreated) VALUES (@ClientID, @ProjectName, @Status, GETDATE())";

                            using (SqlCommand cmd = new SqlCommand(query, con, transaction))
                            {
                                if (!string.IsNullOrEmpty(hfEditingQuotationID.Value)) cmd.Parameters.AddWithValue("@QuotationID", hfEditingQuotationID.Value);
                                cmd.Parameters.AddWithValue("@ClientID", clientID);
                                cmd.Parameters.AddWithValue("@ProjectName", txtProjectName.Text.Trim());
                                cmd.Parameters.AddWithValue("@Status", targetStatus);
                                cmd.ExecuteNonQuery();
                            }
                            transaction.Commit();
                        }
                        catch { transaction.Rollback(); throw; }
                    }
                }
                lblMsg.ForeColor = System.Drawing.Color.Green;
                lblMsg.Text = "Quotation settings mapped over successfully!";
                ResetForm();
                BindQuotationsGrid();
            }
            catch (Exception ex) { lblMsg.ForeColor = System.Drawing.Color.Red; lblMsg.Text = "Error: " + ex.Message; }
        }

        protected void gvQuotations_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "SelectForEdit")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                GridViewRow row = gvQuotations.Rows[index];
                hfEditingQuotationID.Value = gvQuotations.DataKeys[index].Values["QuotationID"].ToString();
                txtProjectName.Text = Server.HtmlDecode(row.Cells[2].Text).Trim();
                Label lblGridStatus = (Label)row.FindControl("lblGridStatus");
                if (lblGridStatus != null) ddlStatus.SelectedValue = lblGridStatus.Text.Trim();
                string companyName = Server.HtmlDecode(row.Cells[1].Text).Trim();
                ListItem item = ddlClients.Items.FindByText(companyName);
                if (item != null) ddlClients.SelectedValue = item.Value;
                btnSaveQuotation.Text = "Update Quotation Profile";
                btnCancelEdit.Visible = true;
            }
            else if (e.CommandName == "PrintInvoice")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                int quoteId = Convert.ToInt32(gvQuotations.DataKeys[index].Values["QuotationID"]);
                int clientId = Convert.ToInt32(gvQuotations.DataKeys[index].Values["ClientID"]);
                GenerateInvoice(quoteId, clientId);
            }
        }

        private void GenerateInvoice(int quotationId, int clientId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    // 1. Fetch Header Info
                    string headerQuery = @"
            SELECT q.QuotationID, q.ProjectName, q.DateCreated, c.CompanyName, c.ContactPerson, c.Email, c.Phone
            FROM Quotations q
            INNER JOIN Clients c ON q.ClientID = c.ClientID
            WHERE q.QuotationID = @QID";

                    using (SqlCommand cmd = new SqlCommand(headerQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@QID", quotationId);
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            if (sdr.Read())
                            {
                                litInvID.Text = sdr["QuotationID"].ToString();
                                litInvDate.Text = Convert.ToDateTime(sdr["DateCreated"]).ToString("MMMM dd, yyyy");
                                litInvProject.Text = sdr["ProjectName"].ToString();
                                litInvClientName.Text = sdr["CompanyName"].ToString();
                                litInvContact.Text = sdr["ContactPerson"].ToString();
                                litInvEmail.Text = sdr["Email"].ToString();
                                litInvPhone.Text = sdr["Phone"].ToString();
                            }
                        }
                    }

                    // 2. Fetch Order Items specifically for THIS Quotation
                    string itemsQuery = @"
            SELECT 
                o.Item, 
                (o.Color + ' / ' + o.Size + CASE WHEN ISNULL(TRY_CAST(o.Customize AS DECIMAL(18,2)), 0) > 0 THEN ' (Customized)' ELSE '' END) AS Details, 
                o.Qty, 
                (CAST(o.Cost AS DECIMAL(18,2)) + ISNULL(TRY_CAST(o.Customize AS DECIMAL(18,2)), 0)) AS UnitCost,
                (CAST(o.Cost AS DECIMAL(18,2)) * CAST(o.Qty AS DECIMAL(18,2)) + ISNULL(TRY_CAST(o.Customize AS DECIMAL(18,2)), 0)) AS LineTotal
            FROM Orders o
            WHERE o.QuotationID = @QID 
            AND o.Status <> 'Cancelled'";

                    decimal grandTotal = 0;
                    using (SqlCommand cmd = new SqlCommand(itemsQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@QID", quotationId);
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            sda.Fill(dt);
                            gvInvoiceItems.DataSource = dt;
                            gvInvoiceItems.DataBind();

                            foreach (DataRow row in dt.Rows)
                            {
                                grandTotal += Convert.ToDecimal(row["LineTotal"]);
                            }
                        }
                    }

                    // 3. Calculate Payments and Status
                    string paymentQuery = @"
            SELECT ISNULL(SUM(p.Amount), 0)
            FROM Payments p
            INNER JOIN Orders o ON p.OrderID = o.OrderID
            WHERE o.QuotationID = @QID AND o.Status <> 'Cancelled'";

                    decimal totalPaid = 0;
                    using (SqlCommand cmdPaid = new SqlCommand(paymentQuery, con))
                    {
                        cmdPaid.Parameters.AddWithValue("@QID", quotationId);
                        totalPaid = Convert.ToDecimal(cmdPaid.ExecuteScalar());
                    }

                    decimal balance = grandTotal - totalPaid;

                    // Determine the badge color and text
                    string invoiceStatus = "UNPAID";
                    string statusColor = "#dc3545"; // Red

                    if (balance <= 0 && grandTotal > 0)
                    {
                        invoiceStatus = "PAID";
                        statusColor = "#198754"; // Green
                    }
                    else if (totalPaid > 0)
                    {
                        invoiceStatus = "PARTIALLY PAID";
                        statusColor = "#ffc107"; // Yellow/Orange
                    }

                    // Bind all totals and status to the UI
                    litInvStatus.Text = $"<span style='background-color: {statusColor}; color: {(invoiceStatus == "PARTIALLY PAID" ? "#000" : "#fff")}; padding: 6px 12px; border-radius: 4px; font-weight: bold; font-size: 1rem; display: inline-block; margin-bottom: 10px;'>{invoiceStatus}</span>";

                    litInvGrandTotal.Text = grandTotal.ToString("C");
                    litInvTotalPaid.Text = totalPaid.ToString("C");
                    litInvBalance.Text = (balance < 0 ? 0 : balance).ToString("C");

                    // Toggle Panels
                    pnlMainView.Visible = false;
                    pnlInvoiceView.Visible = true;
                }
            }
            catch (Exception ex)
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Error generating invoice: " + ex.Message;
            }
        }

        protected void btnCloseInvoice_Click(object sender, EventArgs e)
        {
            pnlMainView.Visible = true;
            pnlInvoiceView.Visible = false;
        }

        protected void gvQuotations_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            try
            {
                int quotationId = Convert.ToInt32(gvQuotations.DataKeys[e.RowIndex].Values["QuotationID"]);
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "DELETE FROM Quotations WHERE QuotationID = @QuotationID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@QuotationID", quotationId);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                ResetForm();
                BindQuotationsGrid();
            }
            catch (Exception ex) { lblMsg.Text = "Error: " + ex.Message; }
        }

        protected void btnCancelEdit_Click(object sender, EventArgs e) { ResetForm(); }

        private void ResetForm()
        {
            hfEditingQuotationID.Value = "";
            txtProjectName.Text = "";
            ddlClients.SelectedIndex = 0;
            ddlStatus.SelectedIndex = 0;
            btnSaveQuotation.Text = "+ Create Quotation Profile";
            btnCancelEdit.Visible = false;
        }
    }
}