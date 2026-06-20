using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SQPMS
{
    public partial class Reports : System.Web.UI.Page
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

            string role = Session["UserRole"].ToString().ToLower().Trim();

            if (role != "admin")
            {
                Response.Redirect("Dashboard.aspx", true);
                return;
            }

            UpdateSidebarFooter(role);

            var phSalesMenu = (PlaceHolder)FindControl("phSalesMenu");
            var phOperationsMenu = (PlaceHolder)FindControl("phOperationsMenu");
            var phOwnerMenu = (PlaceHolder)FindControl("phOwnerMenu");

            if (phSalesMenu != null) phSalesMenu.Visible = (role == "admin" || role == "sales");
            if (phOperationsMenu != null) phOperationsMenu.Visible = (role == "admin" || role == "operation");
            if (phOwnerMenu != null) phOwnerMenu.Visible = (role == "admin");

            if (!IsPostBack)
            {
                LoadConsolidatedFinancialMetrics();
            }
        }

        private void UpdateSidebarFooter(string role)
        {
            string displayRole = char.ToUpper(role[0]) + role.Substring(1);
            var lblUserRole = (Label)FindControl("lblUserRole");
            var lblUserInitials = (Label)FindControl("lblUserInitials");

            if (lblUserRole != null) lblUserRole.Text = displayRole;
            if (lblUserInitials != null) lblUserInitials.Text = role.Length >= 2 ? role.Substring(0, 2).ToUpper() : role.ToUpper();
        }

        protected void ddlDateRange_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadConsolidatedFinancialMetrics();
        }

        private void GetDateParameters(out DateTime? startDate, out DateTime? endDate)
        {
            startDate = null;
            endDate = null;
            DateTime today = DateTime.Today;

            switch (ddlDateRange.SelectedValue)
            {
                case "ThisMonth":
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = startDate.Value.AddMonths(1).AddDays(-1);
                    break;
                case "ThisQuarter":
                    int quarter = (today.Month - 1) / 3 + 1;
                    startDate = new DateTime(today.Year, (quarter - 1) * 3 + 1, 1);
                    endDate = startDate.Value.AddMonths(3).AddDays(-1);
                    break;
                case "ThisYear":
                    startDate = new DateTime(today.Year, 1, 1);
                    endDate = new DateTime(today.Year, 12, 31);
                    break;
            }
        }

        private void LoadConsolidatedFinancialMetrics()
        {
            try
            {
                GetDateParameters(out DateTime? start, out DateTime? end);

                string orderDateFilter = start.HasValue ? " AND q.DateCreated >= @Start AND q.DateCreated <= @End " : "";

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    // FIXED: Now groups the Financial Matrix by QuotationID (The Bundle)
                    string ledgerGridSql = @"
                    WITH OrderData AS (
                        SELECT 
                            o.QuotationID AS OrderID, 
                            c.CompanyName, 
                            STUFF((SELECT ', ' + o2.Item FROM Orders o2 WHERE o2.QuotationID = o.QuotationID FOR XML PATH('')), 1, 2, '') AS Item,
                            SUM(o.Qty) AS Qty, 
                            SUM((CAST(o.Cost AS DECIMAL(18,2)) * CAST(o.Qty AS DECIMAL(18,2))) + ISNULL(CAST(NULLIF(RTRIM(LTRIM(o.Customize)), '') AS DECIMAL(18,2)), 0)) AS GrossSales,
                            SUM(ISNULL((CAST(o.Qty AS DECIMAL(18,2)) * pr.MaterialCost), 0)) AS TotalMaterialCost,
                            ISNULL((SELECT SUM(Amount) FROM Payments p2 INNER JOIN Orders o3 ON p2.OrderID = o3.OrderID WHERE o3.QuotationID = o.QuotationID), 0) AS TotalPaid
                        FROM Orders o
                        INNER JOIN Clients c ON o.ClientID = c.ClientID
                        INNER JOIN Quotations q ON o.QuotationID = q.QuotationID
                        LEFT JOIN (SELECT ProductName, MAX(MaterialCost) AS MaterialCost FROM Products GROUP BY ProductName) pr ON o.Item = pr.ProductName
                        WHERE o.Status <> 'Cancelled' " + orderDateFilter + @"
                        GROUP BY o.QuotationID, c.CompanyName
                    )
                    SELECT 
                        OrderID, CompanyName, Item, Qty, GrossSales, TotalMaterialCost, 
                        (GrossSales - TotalMaterialCost) AS ItemNetProfit,
                        TotalPaid,
                        (GrossSales - TotalPaid) AS UnpaidBalance
                    FROM OrderData
                    ORDER BY OrderID DESC";

                    DataTable dt = new DataTable();
                    using (SqlCommand cmd = new SqlCommand(ledgerGridSql, con))
                    {
                        if (start.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@Start", start.Value);
                            cmd.Parameters.AddWithValue("@End", end.Value.AddHours(23).AddMinutes(59).AddSeconds(59));
                        }

                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            sda.Fill(dt);
                            var gvFinancialLedger = (GridView)FindControl("gvFinancialLedger");
                            if (gvFinancialLedger != null)
                            {
                                gvFinancialLedger.DataSource = dt;
                                gvFinancialLedger.DataBind();
                            }
                        }
                    }

                    // CALCULATE ALL 5 METRICS DYNAMICALLY FROM THE DATATABLE 
                    decimal totalGrossSales = 0;
                    decimal totalMaterialCosts = 0;
                    decimal totalCollected = 0;
                    decimal totalPendingLoss = 0;

                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["GrossSales"] != DBNull.Value) totalGrossSales += Convert.ToDecimal(row["GrossSales"]);
                        if (row["TotalMaterialCost"] != DBNull.Value) totalMaterialCosts += Convert.ToDecimal(row["TotalMaterialCost"]);
                        if (row["TotalPaid"] != DBNull.Value) totalCollected += Convert.ToDecimal(row["TotalPaid"]);
                        if (row["UnpaidBalance"] != DBNull.Value) totalPendingLoss += Convert.ToDecimal(row["UnpaidBalance"]);
                    }

                    decimal netProfit = totalGrossSales - totalMaterialCosts;

                    // Bind to the Top KPI Labels
                    var lblGrossSales = (Label)FindControl("lblGrossSales");
                    if (lblGrossSales != null) lblGrossSales.Text = string.Format("&#8369;{0:N2}", totalGrossSales);

                    var lblMaterialCosts = (Label)FindControl("lblMaterialCosts");
                    if (lblMaterialCosts != null) lblMaterialCosts.Text = string.Format("&#8369;{0:N2}", totalMaterialCosts);

                    var lblNetProfit = (Label)FindControl("lblNetProfit");
                    if (lblNetProfit != null) lblNetProfit.Text = string.Format("&#8369;{0:N2}", netProfit);

                    var lblTotalCollected = (Label)FindControl("lblTotalCollected");
                    if (lblTotalCollected != null) lblTotalCollected.Text = string.Format("&#8369;{0:N2}", totalCollected);

                    var lblTotalLoss = (Label)FindControl("lblTotalLoss");
                    if (lblTotalLoss != null) lblTotalLoss.Text = string.Format("&#8369;{0:N2}", totalPendingLoss);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Financial Compilation Processing Exception: " + ex.Message);
            }
        }

        protected void btnExportCSV_Click(object sender, EventArgs e)
        {
            try
            {
                GetDateParameters(out DateTime? start, out DateTime? end);

                string orderDateFilter = start.HasValue ? " AND q.DateCreated >= @Start AND q.DateCreated <= @End " : "";
                string paymentDateFilter = start.HasValue ? " AND DueDate >= @Start AND DueDate <= @End " : "";

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();

                    // --- INJECT EXCEL HTML STYLING ---
                    sb.AppendLine("<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:x='urn:schemas-microsoft-com:office:excel' xmlns='http://www.w3.org/TR/REC-html40'>");
                    sb.AppendLine("<head><meta charset='utf-8'><style>");
                    sb.AppendLine("table { border-collapse: collapse; margin-bottom: 30px; width: 100%; font-family: 'Segoe UI', Arial, sans-serif; }");
                    sb.AppendLine("th { background-color: #0b3556; color: white; font-weight: bold; text-align: left; padding: 10px; border: 1px solid #dddddd; }");
                    sb.AppendLine("td { padding: 8px; border: 1px solid #dddddd; }");
                    sb.AppendLine(".title { font-size: 18px; font-weight: bold; color: #0b3556; margin-bottom: 10px; }");
                    sb.AppendLine(".summary-lbl { font-weight: bold; background-color: #fdf3e7; width: 200px; }");
                    sb.AppendLine(".money { mso-number-format:'\\#\\,\\#\\#0\\.00'; }");
                    sb.AppendLine("</style></head><body>");

                    // --- SECTION 1: FINANCIAL SUMMARY ---
                    sb.AppendLine("<div class='title'>FINANCIAL SUMMARY REPORT (" + ddlDateRange.SelectedItem.Text + ")</div>");
                    sb.AppendLine("<div style='margin-bottom:15px; color:#666;'>Generated On: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "</div>");

                    var lblGrossSales = (Label)FindControl("lblGrossSales");
                    var lblMaterialCosts = (Label)FindControl("lblMaterialCosts");
                    var lblNetProfit = (Label)FindControl("lblNetProfit");
                    var lblTotalCollected = (Label)FindControl("lblTotalCollected");
                    var lblTotalLoss = (Label)FindControl("lblTotalLoss");

                    sb.AppendLine("<table>");
                    sb.AppendLine($"<tr><td class='summary-lbl'>Total Gross Sales</td><td class='money'>{CleanMoney(lblGrossSales?.Text)}</td></tr>");
                    sb.AppendLine($"<tr><td class='summary-lbl'>Total Material Costs</td><td class='money'>{CleanMoney(lblMaterialCosts?.Text)}</td></tr>");
                    sb.AppendLine($"<tr><td class='summary-lbl'>Net Expected Profit</td><td class='money'>{CleanMoney(lblNetProfit?.Text)}</td></tr>");
                    sb.AppendLine($"<tr><td class='summary-lbl'>Total Collected</td><td class='money'>{CleanMoney(lblTotalCollected?.Text)}</td></tr>");
                    sb.AppendLine($"<tr><td class='summary-lbl'>Pending Loss</td><td class='money' style='color:red;'>{CleanMoney(lblTotalLoss?.Text)}</td></tr>");
                    sb.AppendLine("</table>");

                    // --- SECTION 2: ITEMIZED FINANCIAL MATRIX ---
                    sb.AppendLine("<div class='title'>ITEMIZED FINANCIAL MATRIX</div>");
                    sb.AppendLine("<table>");
                    sb.AppendLine("<tr><th>Order Ref (Bundle)</th><th>Client Name</th><th>Items</th><th>Total Qty</th><th>Gross Sales</th><th>Material Cost</th><th>Net Profit</th><th>Amount Collected</th><th>Pending / Loss</th></tr>");

                    // FIXED: Same query here to ensure the Excel sheet correctly groups the bundle
                    string ledgerGridSql = @"
                    WITH OrderData AS (
                        SELECT 
                            o.QuotationID AS OrderID, 
                            c.CompanyName, 
                            STUFF((SELECT ', ' + o2.Item FROM Orders o2 WHERE o2.QuotationID = o.QuotationID FOR XML PATH('')), 1, 2, '') AS Item,
                            SUM(o.Qty) AS Qty, 
                            SUM((CAST(o.Cost AS DECIMAL(18,2)) * CAST(o.Qty AS DECIMAL(18,2))) + ISNULL(CAST(NULLIF(RTRIM(LTRIM(o.Customize)), '') AS DECIMAL(18,2)), 0)) AS GrossSales,
                            SUM(ISNULL((CAST(o.Qty AS DECIMAL(18,2)) * pr.MaterialCost), 0)) AS TotalMaterialCost,
                            ISNULL((SELECT SUM(Amount) FROM Payments p2 INNER JOIN Orders o3 ON p2.OrderID = o3.OrderID WHERE o3.QuotationID = o.QuotationID), 0) AS TotalPaid
                        FROM Orders o
                        INNER JOIN Clients c ON o.ClientID = c.ClientID
                        INNER JOIN Quotations q ON o.QuotationID = q.QuotationID
                        LEFT JOIN (SELECT ProductName, MAX(MaterialCost) AS MaterialCost FROM Products GROUP BY ProductName) pr ON o.Item = pr.ProductName
                        WHERE o.Status <> 'Cancelled' " + orderDateFilter + @"
                        GROUP BY o.QuotationID, c.CompanyName
                    )
                    SELECT 
                        OrderID, CompanyName, Item, Qty, GrossSales, TotalMaterialCost, 
                        (GrossSales - TotalMaterialCost) AS ItemNetProfit,
                        TotalPaid,
                        (GrossSales - TotalPaid) AS UnpaidBalance
                    FROM OrderData
                    ORDER BY OrderID DESC";

                    using (SqlCommand cmd = new SqlCommand(ledgerGridSql, con))
                    {
                        if (start.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@Start", start.Value);
                            cmd.Parameters.AddWithValue("@End", end.Value.AddHours(23).AddMinutes(59).AddSeconds(59));
                        }

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                sb.AppendLine("<tr>");
                                sb.AppendLine($"<td>{dr["OrderID"]}</td>");
                                sb.AppendLine($"<td>{HttpUtility.HtmlEncode(dr["CompanyName"].ToString())}</td>");
                                sb.AppendLine($"<td>{HttpUtility.HtmlEncode(dr["Item"].ToString())}</td>");
                                sb.AppendLine($"<td>{dr["Qty"]}</td>");
                                sb.AppendLine($"<td class='money'>{dr["GrossSales"]}</td>");
                                sb.AppendLine($"<td class='money'>{dr["TotalMaterialCost"]}</td>");
                                sb.AppendLine($"<td class='money'>{dr["ItemNetProfit"]}</td>");
                                sb.AppendLine($"<td class='money' style='color:#198754;'>{dr["TotalPaid"]}</td>");
                                sb.AppendLine($"<td class='money' style='color:#dc3545;'>{dr["UnpaidBalance"]}</td>");
                                sb.AppendLine("</tr>");
                            }
                        }
                    }
                    sb.AppendLine("</table>");

                    // --- SECTION 3: RAW COLLECTION PAYMENTS ---
                    sb.AppendLine("<div class='title'>DETAILED PAYMENT COLLECTION HISTORY</div>");
                    sb.AppendLine("<table>");
                    sb.AppendLine("<tr><th>Payment ID</th><th>Order Line ID</th><th>Amount Paid</th><th>Status</th><th>Due Date</th></tr>");

                    string paymentsSql = @"
                        SELECT PaymentID, OrderID, Amount, Status, DueDate 
                        FROM Payments 
                        WHERE 1=1 " + paymentDateFilter + " ORDER BY PaymentID DESC";

                    using (SqlCommand cmd2 = new SqlCommand(paymentsSql, con))
                    {
                        if (start.HasValue)
                        {
                            cmd2.Parameters.AddWithValue("@Start", start.Value);
                            cmd2.Parameters.AddWithValue("@End", end.Value.AddHours(23).AddMinutes(59).AddSeconds(59));
                        }

                        using (SqlDataReader dr2 = cmd2.ExecuteReader())
                        {
                            while (dr2.Read())
                            {
                                string dateValue = dr2["DueDate"] != DBNull.Value
                                    ? Convert.ToDateTime(dr2["DueDate"]).ToString("yyyy-MM-dd")
                                    : "N/A";

                                sb.AppendLine("<tr>");
                                sb.AppendLine($"<td>{dr2["PaymentID"]}</td>");
                                sb.AppendLine($"<td>{dr2["OrderID"]}</td>");
                                sb.AppendLine($"<td class='money'>{dr2["Amount"]}</td>");
                                sb.AppendLine($"<td>{dr2["Status"]}</td>");
                                sb.AppendLine($"<td>{dateValue}</td>");
                                sb.AppendLine("</tr>");
                            }
                        }
                    }
                    sb.AppendLine("</table>");
                    sb.AppendLine("</body></html>");

                    // --- TRIGGER BROWSER DOWNLOAD ---
                    Response.Clear();
                    Response.Buffer = true;
                    Response.AddHeader("content-disposition", "attachment;filename=BasicBee_Report_" + ddlDateRange.SelectedValue + "_" + DateTime.Now.ToString("yyyyMMdd") + ".xls");
                    Response.Charset = "utf-8";
                    Response.ContentType = "application/vnd.ms-excel";
                    Response.Output.Write(sb.ToString());
                    Response.Flush();
                    Response.SuppressContent = true;
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Export Exception: " + ex.Message);
            }
        }

        private string CleanMoney(string input)
        {
            if (string.IsNullOrEmpty(input)) return "0.00";
            return input.Replace("&#8369;", "").Replace("₱", "").Replace(",", "").Trim();
        }
    }
}