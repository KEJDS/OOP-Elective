using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SQPMS
{
    public partial class Collection : System.Web.UI.Page
    {
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

            if (currentUserRole != "admin" && currentUserRole != "operation")
            {
                Response.Redirect("Dashboard.aspx", true);
                return;
            }

            UpdateSidebarFooter(currentUserRole);

            phSalesMenu.Visible = (currentUserRole == "admin" || currentUserRole == "sales");
            phOperationsMenu.Visible = (currentUserRole == "admin" || currentUserRole == "operation");
            phOwnerMenu.Visible = (currentUserRole == "admin");

            if (!IsPostBack)
            {
                BindOrdersDropdown();
                BindPaymentsGrid();
                BindArchivedOrders();
                UpdateTrackerTiles();
            }
        }

        private void UpdateSidebarFooter(string role)
        {
            string displayRole = char.ToUpper(role[0]) + role.Substring(1);

            var lblUserRoleControl = (Label)FindControl("lblUserRole");
            var lblUserInitialsControl = (Label)FindControl("lblUserInitials");

            if (lblUserRoleControl != null) lblUserRoleControl.Text = displayRole;
            if (lblUserInitialsControl != null) lblUserInitialsControl.Text = role.Length >= 2 ? role.Substring(0, 2).ToUpper() : role.ToUpper();
        }

        private void BindOrdersDropdown()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = @"
                    WITH QuoteTotals AS (
                        SELECT 
                            o.QuotationID,
                            c.CompanyName,
                            SUM((CAST(o.Cost AS DECIMAL(18,2)) * CAST(o.Qty AS DECIMAL(18,2))) + ISNULL(TRY_CAST(o.Customize AS DECIMAL(18,2)), 0)) AS TotalCost,
                            ISNULL((SELECT SUM(Amount) FROM Payments p INNER JOIN Orders o2 ON p.OrderID = o2.OrderID WHERE o2.QuotationID = o.QuotationID), 0) AS TotalPaid,
                            STUFF((SELECT ', ' + o3.Item FROM Orders o3 WHERE o3.QuotationID = o.QuotationID FOR XML PATH('')), 1, 2, '') AS Items
                        FROM Orders o
                        INNER JOIN Clients c ON o.ClientID = c.ClientID
                        WHERE o.Status <> 'Cancelled'
                        GROUP BY o.QuotationID, c.CompanyName
                    )
                    SELECT 
                        QuotationID AS GroupValue,
                        (CompanyName + ' - ' + Items + ' [Quote Ref: #' + CAST(QuotationID AS VARCHAR) + '] [Due: ' + NCHAR(8369) + CAST(CAST((TotalCost - TotalPaid) AS DECIMAL(18,2)) AS VARCHAR) + ']') AS OrderDisplay
                    FROM QuoteTotals
                    WHERE (TotalCost - TotalPaid) > 0
                    ORDER BY QuotationID DESC";

                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        ddlOrders.DataSource = dt;
                        ddlOrders.DataTextField = "OrderDisplay";
                        ddlOrders.DataValueField = "GroupValue";
                        ddlOrders.DataBind();

                        DropDownList2.DataSource = dt;
                        DropDownList2.DataTextField = "OrderDisplay";
                        DropDownList2.DataValueField = "GroupValue";
                        DropDownList2.DataBind();
                    }
                }
                ddlOrders.Items.Insert(0, new ListItem("-- Select an Invoice Bundle --", ""));
                DropDownList2.Items.Insert(0, new ListItem("-- Select Invoice to Adjust --", ""));
            }
            catch (Exception ex) { lblMsg.Text = "Error loading orders: " + ex.Message; }
        }

        protected void ddlOrders_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddlOrders.SelectedValue))
            {
                txtOrderAmount.Text = "0.00";
                txtRemainingBalance.Text = "0.00";
                txtAmount.Text = "0.00";
                txtAmount.Enabled = false;
                btnSavePayment.Enabled = false;
                lblMsg.Text = "";
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    int selectedQuoteID = Convert.ToInt32(ddlOrders.SelectedValue);

                    string orderQuery = "SELECT ISNULL(SUM((CAST(Cost AS DECIMAL(18,2)) * CAST(Qty AS DECIMAL(18,2))) + ISNULL(TRY_CAST(Customize AS DECIMAL(18,2)), 0)), 0) FROM Orders WHERE QuotationID = @QuoteID AND Status <> 'Cancelled'";
                    decimal orderTotal = 0;
                    using (SqlCommand cmd = new SqlCommand(orderQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@QuoteID", selectedQuoteID);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value) orderTotal = Convert.ToDecimal(result);
                        txtOrderAmount.Text = orderTotal.ToString("F2");
                    }

                    string paymentQuery = "SELECT ISNULL(SUM(p.Amount), 0) FROM Payments p INNER JOIN Orders o ON p.OrderID = o.OrderID WHERE o.QuotationID = @QuoteID";
                    decimal totalPaid = 0;
                    using (SqlCommand cmd = new SqlCommand(paymentQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@QuoteID", selectedQuoteID);
                        object paidResult = cmd.ExecuteScalar();
                        if (paidResult != null && paidResult != DBNull.Value) totalPaid = Convert.ToDecimal(paidResult);
                    }

                    decimal balanceRemaining = orderTotal - totalPaid;
                    txtRemainingBalance.Text = balanceRemaining.ToString("F2");

                    if (balanceRemaining <= 0)
                    {
                        txtAmount.Text = "0.00";
                        txtAmount.Enabled = false;
                        btnSavePayment.Enabled = false;
                        lblMsg.ForeColor = System.Drawing.Color.MediumSeaGreen;
                        lblMsg.Text = "✔ Invoice Bundle is fully paid.";
                    }
                    else
                    {
                        string paymentTerms = "Full Payment";
                        string termQuery = @"
                            SELECT TOP 1 ISNULL(c.PaymentTerms, 'Full Payment') 
                            FROM Clients c 
                            INNER JOIN Quotations q ON c.ClientID = q.ClientID 
                            WHERE q.QuotationID = @QuoteID";

                        using (SqlCommand termCmd = new SqlCommand(termQuery, con))
                        {
                            termCmd.Parameters.AddWithValue("@QuoteID", selectedQuoteID);
                            object termObj = termCmd.ExecuteScalar();
                            if (termObj != null && termObj != DBNull.Value) paymentTerms = termObj.ToString().Trim();
                        }

                        decimal suggestedAmount = balanceRemaining;
                        if (paymentTerms.Equals("Weekly", StringComparison.OrdinalIgnoreCase))
                            suggestedAmount = orderTotal / 4;
                        else if (paymentTerms.Equals("Monthly", StringComparison.OrdinalIgnoreCase))
                            suggestedAmount = orderTotal / 2;
                        else if (decimal.TryParse(paymentTerms, out decimal customFixed))
                            suggestedAmount = customFixed;

                        if (suggestedAmount > balanceRemaining) suggestedAmount = balanceRemaining;

                        txtAmount.Text = suggestedAmount.ToString("F2");
                        txtAmount.Enabled = true;
                        btnSavePayment.Enabled = true;

                        lblMsg.ForeColor = System.Drawing.Color.DodgerBlue;
                        lblMsg.Text = $"Terms: {paymentTerms}. Suggested input: &#8369;{suggestedAmount:F2}.";
                    }
                }
            }
            catch (Exception ex)
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Error calculating account: " + ex.Message;
            }
        }

        protected void btnSavePayment_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddlOrders.SelectedValue)) { lblMsg.ForeColor = System.Drawing.Color.Red; lblMsg.Text = "Select an active invoice."; return; }
            decimal amountPaid = 0; decimal.TryParse(txtAmount.Text, out amountPaid);
            DateTime dueDate;
            if (!DateTime.TryParse(txtDueDate.Text, out dueDate)) { lblMsg.ForeColor = System.Drawing.Color.Red; lblMsg.Text = "Provide a valid billing due date."; return; }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    // Process standard insertion/distribution logic
                    int selectedQuoteID = Convert.ToInt32(ddlOrders.SelectedValue);

                    string getItemsQuery = @"
                    SELECT o.OrderID, 
                           ((CAST(o.Cost AS DECIMAL(18,2)) * CAST(o.Qty AS DECIMAL(18,2))) + ISNULL(TRY_CAST(o.Customize AS DECIMAL(18,2)), 0)) AS ItemTotal,
                           ISNULL((SELECT SUM(Amount) FROM Payments WHERE OrderID = o.OrderID), 0) AS ItemPaid
                    FROM Orders o
                    WHERE o.QuotationID = @QuoteID AND o.Status <> 'Cancelled'
                    ORDER BY o.OrderID ASC";

                    DataTable itemsTable = new DataTable();
                    using (SqlCommand cmd = new SqlCommand(getItemsQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@QuoteID", selectedQuoteID);
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            sda.Fill(itemsTable);
                        }
                    }

                    decimal remainingPaymentToDistribute = amountPaid;

                    foreach (DataRow row in itemsTable.Rows)
                    {
                        if (remainingPaymentToDistribute <= 0) break;

                        int currentOrderID = Convert.ToInt32(row["OrderID"]);
                        decimal itemTotal = Convert.ToDecimal(row["ItemTotal"]);
                        decimal itemPaid = Convert.ToDecimal(row["ItemPaid"]);
                        decimal itemBalance = itemTotal - itemPaid;

                        if (itemBalance > 0)
                        {
                            decimal paymentToApply = Math.Min(remainingPaymentToDistribute, itemBalance);
                            string status = (itemPaid + paymentToApply >= itemTotal) ? "Paid" : "Partially Paid";

                            string insertQuery = "INSERT INTO Payments (OrderID, Amount, DueDate, Status) VALUES (@OrderID, @Amount, @DueDate, @Status)";
                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, con))
                            {
                                insertCmd.Parameters.AddWithValue("@OrderID", currentOrderID);
                                insertCmd.Parameters.AddWithValue("@Amount", paymentToApply);
                                insertCmd.Parameters.AddWithValue("@DueDate", dueDate);
                                insertCmd.Parameters.AddWithValue("@Status", status);
                                insertCmd.ExecuteNonQuery();
                            }
                            remainingPaymentToDistribute -= paymentToApply;
                        }
                    }
                }

                lblMsg.ForeColor = System.Drawing.Color.Green;
                lblMsg.Text = "Invoice payment processed successfully.";
                ResetForm();

                BindOrdersDropdown();
                BindPaymentsGrid();
                BindArchivedOrders();
                UpdateTrackerTiles();
            }
            catch (Exception ex) { lblMsg.ForeColor = System.Drawing.Color.Red; lblMsg.Text = "Error: " + ex.Message; }
        }

        protected void btnAddPayment_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(DropDownList2.SelectedValue)) return;
            decimal addAmount = 0;
            decimal.TryParse(txtAdditionalAmount.Text, out addAmount);
            if (addAmount <= 0) { lblMsg.Text = "Please enter a valid positive amount."; return; }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    int targetQuoteID = Convert.ToInt32(DropDownList2.SelectedValue);

                    string getOrderQuery = "SELECT TOP 1 OrderID FROM Orders WHERE QuotationID = @QuoteID AND Status <> 'Cancelled' ORDER BY OrderID DESC";
                    int targetOrderID = 0;
                    using (SqlCommand gCmd = new SqlCommand(getOrderQuery, con))
                    {
                        gCmd.Parameters.AddWithValue("@QuoteID", targetQuoteID);
                        object result = gCmd.ExecuteScalar();
                        if (result != null) targetOrderID = Convert.ToInt32(result);
                    }

                    if (targetOrderID > 0)
                    {
                        string query = "INSERT INTO Payments (OrderID, Amount, DueDate, Status) VALUES (@OrderID, @Amount, GETDATE(), 'Paid')";
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@OrderID", targetOrderID);
                            cmd.Parameters.AddWithValue("@Amount", addAmount);
                            cmd.ExecuteNonQuery();
                        }
                        lblMsg.ForeColor = System.Drawing.Color.Green;
                        lblMsg.Text = "Adjustment recorded successfully.";
                    }
                }

                BindOrdersDropdown();
                BindPaymentsGrid();
                BindArchivedOrders();
                UpdateTrackerTiles();
                txtAdditionalAmount.Text = "";
            }
            catch (Exception ex) { lblMsg.Text = "Error: " + ex.Message; }
        }

        protected void btnReversePayment_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(DropDownList2.SelectedValue)) return;
            decimal reverseAmount = 0;
            decimal.TryParse(txtAdditionalAmount.Text, out reverseAmount);
            if (reverseAmount <= 0) { lblMsg.ForeColor = System.Drawing.Color.Red; lblMsg.Text = "Please enter a valid positive amount to reverse."; return; }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    int targetQuoteID = Convert.ToInt32(DropDownList2.SelectedValue);

                    decimal totalCurrentlyPaid = 0;
                    string checkQuery = "SELECT ISNULL(SUM(p.Amount), 0) FROM Payments p INNER JOIN Orders o ON p.OrderID = o.OrderID WHERE o.QuotationID = @QuoteID";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@QuoteID", targetQuoteID);
                        object checkObj = checkCmd.ExecuteScalar();
                        if (checkObj != null && checkObj != DBNull.Value) totalCurrentlyPaid = Convert.ToDecimal(checkObj);
                    }

                    if (reverseAmount > totalCurrentlyPaid)
                    {
                        lblMsg.ForeColor = System.Drawing.Color.Red;
                        lblMsg.Text = $"Reversal failed: You cannot refund &#8369;{reverseAmount:N2}. The maximum refundable amount for this bundle is &#8369;{totalCurrentlyPaid:N2}.";
                        return;
                    }

                    string getOrderQuery = "SELECT TOP 1 OrderID FROM Orders WHERE QuotationID = @QuoteID ORDER BY OrderID DESC";
                    int targetOrderID = 0;
                    using (SqlCommand gCmd = new SqlCommand(getOrderQuery, con))
                    {
                        gCmd.Parameters.AddWithValue("@QuoteID", targetQuoteID);
                        object result = gCmd.ExecuteScalar();
                        if (result != null) targetOrderID = Convert.ToInt32(result);
                    }

                    if (targetOrderID > 0)
                    {
                        decimal finalNegativeAmount = -Math.Abs(reverseAmount);
                        string insertQuery = "INSERT INTO Payments (OrderID, Amount, DueDate, Status) VALUES (@OrderID, @Amount, GETDATE(), 'Reversal')";
                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, con))
                        {
                            insertCmd.Parameters.AddWithValue("@OrderID", targetOrderID);
                            insertCmd.Parameters.AddWithValue("@Amount", finalNegativeAmount);
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }

                lblMsg.ForeColor = System.Drawing.Color.Green;
                lblMsg.Text = "Overpayment reversed successfully.";

                BindOrdersDropdown();
                BindPaymentsGrid();
                BindArchivedOrders();
                UpdateTrackerTiles();
                txtAdditionalAmount.Text = "";
            }
            catch (Exception ex)
            {
                lblMsg.ForeColor = System.Drawing.Color.Red;
                lblMsg.Text = "Error: " + ex.Message;
            }
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            string start = txtFilterStart.Text;
            string end = txtFilterEnd.Text;
            string status = ddlStatusFilter.SelectedValue;
            string search = "";
            var searchControl = FindControl("txtSearch") as TextBox;
            if (searchControl != null) search = searchControl.Text.Trim();

            BindPaymentsGrid(start, end, status, search);
        }

        private void BindPaymentsGrid(string startDate = "", string endDate = "", string statusFilter = "", string searchQuery = "")
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = @"
            WITH BundleTotals AS (
                SELECT o.QuotationID, c.CompanyName, 
                       STUFF((SELECT ', ' + o2.Item FROM Orders o2 WHERE o2.QuotationID = o.QuotationID FOR XML PATH('')), 1, 2, '') AS Item,
                       SUM((CAST(o.Cost AS DECIMAL(18,2)) * CAST(o.Qty AS DECIMAL(18,2))) + ISNULL(TRY_CAST(o.Customize AS DECIMAL(18,2)), 0)) AS OrderTotal
                FROM Orders o
                INNER JOIN Clients c ON o.ClientID = c.ClientID
                GROUP BY o.QuotationID, c.CompanyName
            ),
            BundlePayments AS (
                SELECT 
                    MIN(p.PaymentID) AS PaymentID, 
                    o.QuotationID, 
                    p.DueDate, 
                    MAX(p.Status) AS SavedStatus,
                    SUM(p.Amount) AS AmountPaid
                FROM Payments p
                INNER JOIN Orders o ON p.OrderID = o.OrderID
                GROUP BY o.QuotationID, p.DueDate
            ),
            Ledger AS (
                SELECT 
                    bp.PaymentID, 
                    bp.QuotationID AS OrderID, 
                    bt.CompanyName, 
                    bt.Item, 
                    bt.OrderTotal, 
                    bp.AmountPaid AS Amount,
                    (bt.OrderTotal - SUM(bp.AmountPaid) OVER(PARTITION BY bp.QuotationID)) AS RemainingBalance,
                    CASE 
                        WHEN (bt.OrderTotal - SUM(bp.AmountPaid) OVER(PARTITION BY bp.QuotationID)) < 0.00 THEN 'Overpaid'
                        WHEN (bt.OrderTotal - SUM(bp.AmountPaid) OVER(PARTITION BY bp.QuotationID)) = 0.00 THEN 'Paid'
                        WHEN bp.SavedStatus = 'Reversal' THEN 'Reversed'
                        WHEN SUM(bp.AmountPaid) OVER(PARTITION BY bp.QuotationID) > 0 THEN 'Partially Paid'
                        ELSE 'Unpaid'
                    END AS Status,
                    bp.DueDate
                FROM BundlePayments bp
                INNER JOIN BundleTotals bt ON bp.QuotationID = bt.QuotationID
            )
            SELECT * FROM Ledger WHERE 1=1 ";

                    if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                        query += " AND DueDate BETWEEN @Start AND @End ";
                    if (!string.IsNullOrEmpty(statusFilter))
                        query += " AND Status = @Status ";
                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        if (int.TryParse(searchQuery, out _))
                            query += " AND (OrderID = @OrderID OR CompanyName LIKE @Search) ";
                        else
                            query += " AND CompanyName LIKE @Search ";
                    }

                    query += " ORDER BY PaymentID DESC";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                        {
                            cmd.Parameters.AddWithValue("@Start", startDate);
                            cmd.Parameters.AddWithValue("@End", endDate);
                        }
                        if (!string.IsNullOrEmpty(statusFilter))
                            cmd.Parameters.AddWithValue("@Status", statusFilter);

                        if (!string.IsNullOrEmpty(searchQuery))
                        {
                            if (int.TryParse(searchQuery, out int orderId))
                                cmd.Parameters.AddWithValue("@OrderID", orderId);
                            cmd.Parameters.AddWithValue("@Search", "%" + searchQuery + "%");
                        }

                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            sda.Fill(dt);
                            gvPayments.DataSource = dt;
                            gvPayments.DataBind();
                        }
                    }
                }
            }
            catch (Exception ex) { lblMsg.Text = "Error loading ledger logs: " + ex.Message; }
        }

        private void BindArchivedOrders()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = @"
                    WITH QuoteTotals AS (
                        SELECT 
                            o.QuotationID AS OrderID,
                            c.CompanyName,
                            STUFF((SELECT ', ' + o2.Item FROM Orders o2 WHERE o2.QuotationID = o.QuotationID FOR XML PATH('')), 1, 2, '') AS Item,
                            SUM((CAST(o.Cost AS DECIMAL(18,2)) * CAST(o.Qty AS DECIMAL(18,2))) + ISNULL(TRY_CAST(o.Customize AS DECIMAL(18,2)), 0)) AS OrderTotal
                        FROM Orders o
                        INNER JOIN Clients c ON o.ClientID = c.ClientID
                        WHERE o.Status <> 'Cancelled'
                        GROUP BY o.QuotationID, c.CompanyName
                    ),
                    QuotePayments AS (
                        SELECT 
                            o.QuotationID,
                            SUM(p.Amount) AS TotalPaid,
                            MAX(p.DueDate) AS LastPaymentDate
                        FROM Payments p
                        INNER JOIN Orders o ON p.OrderID = o.OrderID
                        GROUP BY o.QuotationID
                    )
                    SELECT 
                        qt.OrderID, qt.CompanyName, qt.Item, qt.OrderTotal, 
                        ISNULL(qp.TotalPaid, 0) AS TotalPaid, 
                        qp.LastPaymentDate 
                    FROM QuoteTotals qt
                    LEFT JOIN QuotePayments qp ON qt.OrderID = qp.QuotationID
                    WHERE (qt.OrderTotal - ISNULL(qp.TotalPaid, 0)) <= 0 AND ISNULL(qp.TotalPaid, 0) > 0
                    ORDER BY qp.LastPaymentDate DESC";

                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        gvArchivedOrders.DataSource = dt;
                        gvArchivedOrders.DataBind();
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Archive Error: " + ex.Message); }
        }

        private void UpdateTrackerTiles()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = @"
            WITH OrderSummary AS (
                SELECT 
                    o.QuotationID,
                    SUM((CAST(o.Cost AS DECIMAL(18,2)) * CAST(o.Qty AS DECIMAL(18,2))) + ISNULL(TRY_CAST(o.Customize AS DECIMAL(18,2)), 0)) AS OrderTotal,
                    ISNULL((SELECT SUM(Amount) FROM Payments p INNER JOIN Orders o2 ON p.OrderID = o2.OrderID WHERE o2.QuotationID = o.QuotationID), 0) AS TotalPaid
                FROM Orders o
                WHERE o.Status <> 'Cancelled' 
                GROUP BY o.QuotationID
            )
            SELECT 
                ISNULL(SUM(OrderTotal - TotalPaid), 0) AS TotalPendingBalance,
                ISNULL(SUM(CASE WHEN (OrderTotal - TotalPaid) > 0 THEN 1 ELSE 0 END), 0) AS NonPaidCount
            FROM OrderSummary";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        con.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            if (sdr.Read())
                            {
                                decimal pendingBalance = sdr["TotalPendingBalance"] != DBNull.Value ? Convert.ToDecimal(sdr["TotalPendingBalance"]) : 0;
                                int overdueCount = sdr["NonPaidCount"] != DBNull.Value ? Convert.ToInt32(sdr["NonPaidCount"]) : 0;

                                lblTotalPending.Text = string.Format("&#8369;{0:N2}", pendingBalance);
                                lblOutstandingInvoices.Text = $"{overdueCount} collection bundles outstanding";
                                lblOverdueAccounts.Text = overdueCount.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { lblMsg.Text = "Error loading metric tracking: " + ex.Message; }
        }

        protected void gvPayments_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                Label lblStatus = (Label)e.Row.FindControl("lblStatus");
                if (lblStatus != null)
                {
                    string val = lblStatus.Text.Trim();
                    if (val == "Paid") lblStatus.CssClass = "badge-paid";
                    else if (val == "Partially Paid") lblStatus.CssClass = "badge-partial";
                    else if (val == "Overpaid") lblStatus.CssClass = "badge-overpaid";
                    else if (val == "Reversal" || val == "Reversed")
                    {
                        lblStatus.CssClass = "badge-reversed";
                        e.Row.Cells[5].ForeColor = System.Drawing.Color.Gray;
                        e.Row.Cells[6].ForeColor = System.Drawing.Color.Gray;
                    }
                    else lblStatus.CssClass = "badge-unpaid";
                }
            }
        }

        protected void gvPayments_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            // Edit functionality removed
        }

        protected void gvPayments_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            try
            {
                int quoteID = Convert.ToInt32(gvPayments.DataKeys[e.RowIndex].Values["OrderID"]);
                DateTime.TryParse(gvPayments.Rows[e.RowIndex].Cells[8].Text.Trim(), out DateTime dateValue);

                using (SqlConnection con = new SqlConnection(connString))
                {
                    string delQuery = "DELETE FROM Payments WHERE DueDate = @DueDate AND OrderID IN (SELECT OrderID FROM Orders WHERE QuotationID = @QuoteID)";
                    using (SqlCommand cmd = new SqlCommand(delQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@DueDate", dateValue);
                        cmd.Parameters.AddWithValue("@QuoteID", quoteID);
                        con.Open(); cmd.ExecuteNonQuery();
                    }
                }
                ResetForm();
                BindOrdersDropdown();
                BindPaymentsGrid();
                BindArchivedOrders();
                UpdateTrackerTiles();
            }
            catch (Exception ex) { lblMsg.Text = "Error: " + ex.Message; }
        }

        private void ResetForm()
        {
            txtAmount.Text = "";
            txtAmount.Enabled = false;
            btnSavePayment.Text = "✔ Record Payment Entry";
            btnSavePayment.Enabled = true;

            ddlOrders.SelectedIndex = 0;
            txtOrderAmount.Text = "0.00";
            txtRemainingBalance.Text = "0.00";
            lblMsg.Text = "";
        }
    }
}