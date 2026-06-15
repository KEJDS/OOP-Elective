using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SQPMS
{
    public partial class Production : System.Web.UI.Page
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

            string currentUserRole = Session["UserRole"].ToString().ToLower().Trim();

            // AUTHORIZATION
            if (currentUserRole != "admin" && currentUserRole != "operation")
            {
                Response.Redirect("Dashboard.aspx", true);
                return;
            }

            UpdateSidebarFooter(currentUserRole);

            // SIDEBAR VISIBILITY
            phSalesMenu.Visible = (currentUserRole == "admin" || currentUserRole == "sales");
            phOperationsMenu.Visible = (currentUserRole == "admin" || currentUserRole == "operation");
            phOwnerMenu.Visible = (currentUserRole == "admin");

            if (!IsPostBack)
            {
                BindActiveOrdersDropdown();
                BindProductionGrid();
            }
        }

        private void UpdateSidebarFooter(string role)
        {
            string displayRole = char.ToUpper(role[0]) + role.Substring(1);

            if (lblUserRole != null)
                lblUserRole.Text = displayRole;

            if (lblUserInitials != null)
                lblUserInitials.Text = role.Length >= 2
                    ? role.Substring(0, 2).ToUpper()
                    : role.ToUpper();
        }

        protected void btnGoBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("Dashboard.aspx");
        }

        // LOAD ACTIVE APPROVED ORDERS
        private void BindActiveOrdersDropdown()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = @"
            SELECT 
                q.QuotationID,
                (c.CompanyName + ' - ' + 
                STUFF((SELECT ', ' + o2.Item FROM Orders o2 WHERE o2.QuotationID = q.QuotationID AND o2.Status <> 'Cancelled' FOR XML PATH('')), 1, 2, '') + 
                ' [Quote Ref: #' + CAST(q.QuotationID AS VARCHAR) + ']') AS OrderDisplay
            FROM Orders o
            INNER JOIN Clients c ON o.ClientID = c.ClientID
            INNER JOIN Quotations q ON o.QuotationID = q.QuotationID
            WHERE q.Status = 'Approved' 
            AND o.OrderID NOT IN (SELECT OrderID FROM Production)
            GROUP BY q.QuotationID, c.CompanyName
            ORDER BY q.QuotationID DESC";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            sda.Fill(dt);
                            ddlOrders.DataSource = dt;
                            ddlOrders.DataTextField = "OrderDisplay";
                            ddlOrders.DataValueField = "QuotationID"; 
                            ddlOrders.DataBind();
                        }
                    }
                }
                ddlOrders.Items.Insert(0, new ListItem("-- Select an Accepted Order Bundle --", ""));
            }
            catch (Exception ex) { lblMsg.Text = "Error: " + ex.Message; }
        }

        protected void ddlOrders_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddlOrders.SelectedValue))
            {
                txtQuantity.Text = "";
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "SELECT SUM(Qty) FROM Orders WHERE QuotationID = @QID AND Status <> 'Cancelled' AND OrderID NOT IN (SELECT OrderID FROM Production)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@QID", Convert.ToInt32(ddlOrders.SelectedValue));
                        con.Open();
                        object quantity = cmd.ExecuteScalar();

                        if (quantity != DBNull.Value && quantity != null)
                            txtQuantity.Text = quantity.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Error loading quantity: " + ex.Message;
            }
        }

        // LOAD PRODUCTION GRID WITH VISUAL GROUPING
        private void BindProductionGrid()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = @"
            SELECT 
                q.QuotationID AS ProductionID, 
                MIN(p.OrderID) AS OrderID, 
                (c.CompanyName + ' [Quote #' + CAST(q.QuotationID AS VARCHAR) + ']') AS CompanyName,
                STUFF((SELECT ', ' + o2.Item FROM Orders o2 WHERE o2.QuotationID = q.QuotationID FOR XML PATH('')), 1, 2, '') AS Item,
                SUM(p.Quantity) AS Quantity,
                MAX(p.Status) AS Status,
                MAX(p.Deadline) AS Deadline
            FROM Production p
            INNER JOIN Orders o ON p.OrderID = o.OrderID
            INNER JOIN Clients c ON o.ClientID = c.ClientID
            INNER JOIN Quotations q ON o.QuotationID = q.QuotationID
            WHERE q.Status = 'Approved'
            GROUP BY q.QuotationID, c.CompanyName
            ORDER BY q.QuotationID DESC";

                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        gvProduction.DataSource = dt;
                        gvProduction.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Error loading production records: " + ex.Message;
            }
        }

        // SAVE / UPDATE PRODUCTION
        protected void btnSaveProduction_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddlOrders.SelectedValue))
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Please select an order bundle.";
                return;
            }

            int qty;
            if (!int.TryParse(txtQuantity.Text, out qty))
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Invalid quantity.";
                return;
            }

            DateTime deadlineDate;
            if (!DateTime.TryParse(txtDeadline.Text, out deadlineDate))
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Invalid deadline date.";
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    if (!string.IsNullOrEmpty(hfEditingProductionID.Value))
                    {
                        // FIXED: Updates all production rows tied to the bundled QuotationID
                        string updateQuery = @"
                            UPDATE Production
                            SET Status = @Status, Deadline = @Deadline
                            WHERE OrderID IN (SELECT OrderID FROM Orders WHERE QuotationID = @QuoteID)";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@QuoteID", hfEditingProductionID.Value);
                            cmd.Parameters.AddWithValue("@Status", ddlStatus.SelectedValue);
                            cmd.Parameters.AddWithValue("@Deadline", deadlineDate);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string checkQuery = @"
                            SELECT OrderID, Qty FROM Orders 
                            WHERE QuotationID = @QID AND Status <> 'Cancelled' 
                            AND OrderID NOT IN (SELECT OrderID FROM Production)";

                        DataTable dtNew = new DataTable();
                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                        {
                            checkCmd.Parameters.AddWithValue("@QID", ddlOrders.SelectedValue);
                            using (SqlDataAdapter sda = new SqlDataAdapter(checkCmd)) { sda.Fill(dtNew); }
                        }

                        if (dtNew.Rows.Count == 0)
                        {
                            lblMsg.ForeColor = System.Drawing.Color.Red;
                            lblMsg.Text = "All items in this Quote are already in the production queue!";
                            return;
                        }

                        string insertQuery = "INSERT INTO Production (OrderID, Quantity, Status, Deadline) VALUES (@OrderID, @Quantity, @Status, @Deadline)";
                        foreach (DataRow row in dtNew.Rows)
                        {
                            using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@OrderID", row["OrderID"]);
                                cmd.Parameters.AddWithValue("@Quantity", row["Qty"]);
                                cmd.Parameters.AddWithValue("@Status", ddlStatus.SelectedValue);
                                cmd.Parameters.AddWithValue("@Deadline", deadlineDate);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                lblMsg.ForeColor = System.Drawing.Color.Green;
                lblMsg.Text = "Production dispatched/updated successfully.";

                ResetForm();
                BindProductionGrid();
                BindActiveOrdersDropdown();
            }
            catch (Exception ex)
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Error saving production: " + ex.Message;
            }
        }

        // EDIT RECORD
        protected void gvProduction_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "SelectForEdit")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                GridViewRow row = gvProduction.Rows[index];

                // DataKeys["ProductionID"] is actually the QuotationID due to our grid grouping
                hfEditingProductionID.Value = gvProduction.DataKeys[index].Values["ProductionID"].ToString();
                string assignedOrderID = gvProduction.DataKeys[index].Values["OrderID"].ToString();

                txtQuantity.Text = row.Cells[4].Text.Trim();

                Label lblGridStatus = (Label)row.FindControl("lblStatus");
                if (lblGridStatus != null)
                {
                    ddlStatus.SelectedValue = lblGridStatus.Text.Trim();
                }

                string rawDeadlineText = row.Cells[6].Text.Split(' ')[0].Trim();
                DateTime deadlineValue;
                if (DateTime.TryParse(rawDeadlineText, out deadlineValue))
                {
                    txtDeadline.Text = deadlineValue.ToString("yyyy-MM-dd");
                }

                try
                {
                    using (SqlConnection con = new SqlConnection(connString))
                    {
                        string getQuoteQry = @"
                            SELECT q.QuotationID, c.CompanyName 
                            FROM Orders o 
                            INNER JOIN Quotations q ON o.QuotationID = q.QuotationID 
                            INNER JOIN Clients c ON o.ClientID = c.ClientID
                            WHERE o.OrderID = @OID";

                        using (SqlCommand cmd = new SqlCommand(getQuoteQry, con))
                        {
                            cmd.Parameters.AddWithValue("@OID", assignedOrderID);
                            con.Open();
                            using (SqlDataReader sdr = cmd.ExecuteReader())
                            {
                                if (sdr.Read())
                                {
                                    string qId = sdr["QuotationID"].ToString();
                                    string compName = sdr["CompanyName"].ToString();

                                    if (ddlOrders.Items.FindByValue(qId) == null)
                                    {
                                        ddlOrders.Items.Add(new ListItem($"{compName} [Quote Ref: #{qId}] (Editing Mode)", qId));
                                    }

                                    ddlOrders.SelectedValue = qId;
                                    ddlOrders.Enabled = false; 
                                }
                            }
                        }
                    }
                }
                catch { }

                btnSaveProduction.Text = "Update Bundle Status";
                btnCancelEdit.Visible = true;
            }
        }

        // DYNAMIC DEADLINE FORMATTING
        protected void gvProduction_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                string status = DataBinder.Eval(e.Row.DataItem, "Status").ToString();
                object deadlineObj = DataBinder.Eval(e.Row.DataItem, "Deadline");

                if (deadlineObj != DBNull.Value && deadlineObj != null)
                {
                    DateTime deadline = Convert.ToDateTime(deadlineObj);
                    DateTime today = DateTime.Today;
                    TimeSpan diff = deadline.Date - today;

                    e.Row.Cells[6].Text = deadline.ToString("yyyy-MM-dd");

                    if (status != "Completed")
                    {
                        if (diff.TotalDays < 0)
                        {
                            e.Row.Cells[6].BackColor = System.Drawing.Color.FromArgb(220, 53, 69);
                            e.Row.Cells[6].ForeColor = System.Drawing.Color.White;
                            e.Row.Cells[6].Font.Bold = true;
                            e.Row.Cells[6].Text += " (Past Due!)";
                        }
                        else if (diff.TotalDays == 0)
                        {
                            e.Row.Cells[6].BackColor = System.Drawing.Color.FromArgb(253, 126, 20);
                            e.Row.Cells[6].ForeColor = System.Drawing.Color.White;
                            e.Row.Cells[6].Font.Bold = true;
                            e.Row.Cells[6].Text += " (Due Today)";
                        }
                        else if (diff.TotalDays <= 3)
                        {
                            e.Row.Cells[6].BackColor = System.Drawing.Color.FromArgb(255, 193, 7);
                            e.Row.Cells[6].ForeColor = System.Drawing.Color.Black;
                            e.Row.Cells[6].Font.Bold = true;
                            e.Row.Cells[6].Text += " (Due Soon)";
                        }
                        else
                        {
                            e.Row.Cells[6].ForeColor = System.Drawing.Color.FromArgb(25, 135, 84);
                            e.Row.Cells[6].Font.Bold = true;
                            e.Row.Cells[6].Text += " (On Track)";
                        }
                    }
                    else
                    {
                        e.Row.Cells[6].ForeColor = System.Drawing.Color.Gray;
                        e.Row.Cells[6].Text += " (Done)";
                    }
                }
            }
        }

        // DELETE RECORD
        protected void gvProduction_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            try
            {
                // FIXED: Delete all items in the bundle based on the QuotationID
                int quoteID = Convert.ToInt32(gvProduction.DataKeys[e.RowIndex].Values["ProductionID"]);

                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "DELETE FROM Production WHERE OrderID IN (SELECT OrderID FROM Orders WHERE QuotationID = @QuoteID)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@QuoteID", quoteID);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                ResetForm();
                BindProductionGrid();
                BindActiveOrdersDropdown(); 
            }
            catch (Exception ex)
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Error deleting production record: " + ex.Message;
            }
        }

        protected void btnCancelEdit_Click(object sender, EventArgs e)
        {
            ResetForm();
        }

        // RESET FORM
        private void ResetForm()
        {
            hfEditingProductionID.Value = "";
            txtQuantity.Text = "";
            txtDeadline.Text = "";
            ddlOrders.Enabled = true;
            ddlOrders.SelectedIndex = 0;
            ddlStatus.SelectedIndex = 0;
            btnSaveProduction.Text = "✔ Dispatch Bundle to Queue";
            btnCancelEdit.Visible = false;
        }
    }
}
