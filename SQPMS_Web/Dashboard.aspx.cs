using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;
using System.Web.UI.WebControls;

namespace SQPMS
{
    public partial class Dashboard : System.Web.UI.Page
    {
        private string connString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            if (!IsPostBack)
            {
                if (Session["UserRole"] == null || Session["Username"] == null)
                {
                    Response.Redirect("Login.aspx");
                    return;
                }

                string userRole = Session["UserRole"].ToString().ToLower().Trim();
                string username = Session["Username"].ToString();

                litUserRoleHeader.Text = userRole;
                litUserSidebarLabel.Text = username;
                litUserInitials.Text = username.Substring(0, 1).ToUpper();

                // Setup Navigation Access
                if (userRole == "sales")
                {
                    phSalesMenu.Visible = true;
                    phOperationsMenu.Visible = false;
                    phOwnerMenu.Visible = false;
                }
                else if (userRole == "operation")
                {
                    phSalesMenu.Visible = false;
                    phOperationsMenu.Visible = true;
                    phOwnerMenu.Visible = false;
                }
                else if (userRole == "admin")
                {
                    phSalesMenu.Visible = true;
                    phOperationsMenu.Visible = true;
                    phOwnerMenu.Visible = true;
                }

                CalculateLiveSummaryMetrics();
                GenerateDynamicChartBars();
                BindRecentActivityStream();
            }
        }

        private void CalculateLiveSummaryMetrics()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    decimal totalSales = 0;

                    // 1. Total Sales (Current Month)
                    string salesQuery = @"
                    SELECT ISNULL(SUM(Amount), 0) FROM Payments 
                    WHERE Status IN ('Paid', 'Partially Paid', 'Reversal') 
                    AND MONTH(DueDate) = MONTH(GETDATE()) 
                    AND YEAR(DueDate) = YEAR(GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(salesQuery, con))
                    {
                        totalSales = Convert.ToDecimal(cmd.ExecuteScalar());
                        lblTotalSales.Text = string.Format("&#8369;{0:N2}", totalSales);
                    }

                    // 1b. Get Previous Month Sales for Trend Calculation
                    string prevSalesQuery = @"
                    SELECT ISNULL(SUM(Amount), 0) FROM Payments 
                    WHERE Status IN ('Paid', 'Partially Paid', 'Reversal') 
                    AND MONTH(DueDate) = MONTH(DATEADD(month, -1, GETDATE())) 
                    AND YEAR(DueDate) = YEAR(DATEADD(month, -1, GETDATE()))";

                    using (SqlCommand cmdPrev = new SqlCommand(prevSalesQuery, con))
                    {
                        decimal prevSales = Convert.ToDecimal(cmdPrev.ExecuteScalar());

                        if (prevSales > 0)
                        {
                            decimal trendCalc = ((totalSales - prevSales) / prevSales) * 100;
                            lblSalesTrend.Text = string.Format("{0:F1}% vs Prev", Math.Abs(trendCalc));

                            if (trendCalc >= 0)
                            {
                                divSalesTrend.Attributes["class"] = "metric-subtext positive";
                                litTrendIcon.Text = "<i class='fas fa-arrow-up me-1'></i>+";
                            }
                            else
                            {
                                divSalesTrend.Attributes["class"] = "metric-subtext negative";
                                litTrendIcon.Text = "<i class='fas fa-arrow-down me-1'></i>-";
                            }
                        }
                        else if (totalSales > 0 && prevSales == 0)
                        {
                            lblSalesTrend.Text = "100% vs Prev";
                            divSalesTrend.Attributes["class"] = "metric-subtext positive";
                            litTrendIcon.Text = "<i class='fas fa-arrow-up me-1'></i>+";
                        }
                        else
                        {
                            lblSalesTrend.Text = "0% vs Prev";
                            divSalesTrend.Attributes["class"] = "metric-subtext";
                            litTrendIcon.Text = "";
                        }
                    }

                    // 2. Active Quotations
                    string quoteQuery = "SELECT COUNT(*) FROM Quotations WHERE Status <> 'Rejected'";
                    using (SqlCommand cmd = new SqlCommand(quoteQuery, con))
                    {
                        int activeQuotes = Convert.ToInt32(cmd.ExecuteScalar());
                        lblActiveQuotations.Text = activeQuotes.ToString();
                    }

                    // 3 & 4. Pending Collections & Overdue Accounts
                    // FIXED: Removed EXISTS filter, added QuotationID grouping so it matches the Collections page
                    string collectionsQuery = @"
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

                    using (SqlCommand cmd = new SqlCommand(collectionsQuery, con))
                    {
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            if (sdr.Read())
                            {
                                decimal pendingBalance = Convert.ToDecimal(sdr["TotalPendingBalance"]);
                                int overdueCount = Convert.ToInt32(sdr["NonPaidCount"]);

                                lblPendingCollections.Text = string.Format("&#8369;{0:N2}", pendingBalance);
                                lblOverdueSubtext.Text = $"{overdueCount} overdue";
                            }
                        }
                    }

                    // 5. In Production
                    string productionQuery = @"
                    SELECT COUNT(DISTINCT o.QuotationID) 
                    FROM Production p
                    INNER JOIN Orders o ON p.OrderID = o.OrderID
                    WHERE p.Status <> 'Completed'";

                    using (SqlCommand cmd = new SqlCommand(productionQuery, con))
                    {
                        int runningJobs = Convert.ToInt32(cmd.ExecuteScalar());
                        lblInProduction.Text = runningJobs.ToString();
                    }

                    // 6. Collection Rate
                    string baseRevenueQuery = @"
                    SELECT ISNULL(SUM(OrderTotal), 0)
                    FROM (
                        SELECT DISTINCT o.OrderID, 
                        (CAST(o.Cost AS DECIMAL(18,2)) * CAST(o.Qty AS DECIMAL(18,2)) + ISNULL(TRY_CAST(o.Customize AS DECIMAL(18,2)), 0)) AS OrderTotal
                        FROM Orders o 
                        INNER JOIN Payments p ON o.OrderID = p.OrderID 
                        WHERE MONTH(p.DueDate) = MONTH(GETDATE())
                        AND YEAR(p.DueDate) = YEAR(GETDATE())
                        AND o.Status <> 'Cancelled'
                    ) AS UniqueOrders";

                    using (SqlCommand cmdRev = new SqlCommand(baseRevenueQuery, con))
                    {
                        decimal baseRev = Convert.ToDecimal(cmdRev.ExecuteScalar());

                        if (baseRev > 0 && totalSales > 0)
                        {
                            decimal margin = (totalSales / baseRev) * 100;
                            lblProfitMargin.Text = string.Format("{0:F1}%", Math.Min(margin, 100));
                        }
                        else
                        {
                            lblProfitMargin.Text = "0.0%";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Dashboard Core Metrics Error: " + ex.Message);
            }
        }

        private void GenerateDynamicChartBars()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = @"
                WITH MonthlyPayments AS (
                    SELECT 
                        DATENAME(MONTH, DueDate) AS MonthLabel,
                        MONTH(DueDate) AS MonthNum,
                        YEAR(DueDate) AS YearNum,
                        ISNULL(SUM(Amount), 0) AS TotalPaid
                    FROM Payments
                    WHERE Status IN ('Paid', 'Partially Paid', 'Reversal')
                    GROUP BY DATENAME(MONTH, DueDate), MONTH(DueDate), YEAR(DueDate)
                ),
                OrderBalances AS (
                    SELECT 
                        o.OrderID,
                        MAX(p.DueDate) AS LastActivity,
                        (CAST(o.Cost AS DECIMAL(18,2)) * CAST(o.Qty AS DECIMAL(18,2)) + ISNULL(TRY_CAST(o.Customize AS DECIMAL(18,2)), 0)) - ISNULL(SUM(p.Amount), 0) AS PendingBalance
                    FROM Orders o
                    INNER JOIN Payments p ON o.OrderID = p.OrderID
                    WHERE o.Status <> 'Cancelled'
                    GROUP BY o.OrderID, o.Cost, o.Qty, o.Customize
                ),
                MonthlyPending AS (
                    SELECT 
                        MONTH(LastActivity) AS MonthNum,
                        YEAR(LastActivity) AS YearNum,
                        SUM(PendingBalance) AS TotalPending
                    FROM OrderBalances
                    GROUP BY MONTH(LastActivity), YEAR(LastActivity)
                )
                SELECT TOP 5
                    mp.MonthLabel,
                    mp.MonthNum,
                    mp.YearNum,
                    mp.TotalPaid,
                    ISNULL(mpend.TotalPending, 0) AS TotalPending
                FROM MonthlyPayments mp
                LEFT JOIN MonthlyPending mpend ON mp.MonthNum = mpend.MonthNum AND mp.YearNum = mpend.YearNum
                ORDER BY mp.YearNum DESC, mp.MonthNum DESC";

                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable rawData = new DataTable();
                        sda.Fill(rawData);

                        DataTable chartTable = new DataTable();
                        chartTable.Columns.Add("MonthLabel", typeof(string));
                        chartTable.Columns.Add("TotalPaid", typeof(decimal));
                        chartTable.Columns.Add("TotalPending", typeof(decimal));
                        chartTable.Columns.Add("SalesHeight", typeof(int));
                        chartTable.Columns.Add("ExpenseHeight", typeof(int));

                        decimal maxVal = 1;
                        foreach (DataRow r in rawData.Rows)
                        {
                            decimal paid = Convert.ToDecimal(r["TotalPaid"]);
                            decimal pend = Convert.ToDecimal(r["TotalPending"]);
                            if (paid > maxVal) maxVal = paid;
                            if (pend > maxVal) maxVal = pend;
                        }

                        for (int i = rawData.Rows.Count - 1; i >= 0; i--)
                        {
                            DataRow srcRow = rawData.Rows[i];
                            DataRow newRow = chartTable.NewRow();

                            decimal paid = Convert.ToDecimal(srcRow["TotalPaid"]);
                            decimal pend = Convert.ToDecimal(srcRow["TotalPending"]);

                            newRow["MonthLabel"] = srcRow["MonthLabel"].ToString().Substring(0, 3);
                            newRow["TotalPaid"] = paid;
                            newRow["TotalPending"] = pend;

                            newRow["SalesHeight"] = paid > 0 ? Convert.ToInt32((paid / maxVal) * 120) : 4;
                            newRow["ExpenseHeight"] = pend > 0 ? Convert.ToInt32((pend / maxVal) * 120) : 4;

                            chartTable.Rows.Add(newRow);
                        }

                        rptMonthlyChart.DataSource = chartTable;
                        rptMonthlyChart.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Dashboard Chart Rendering Error: " + ex.Message);
            }
        }

        private void BindRecentActivityStream()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string activityQuery = @"
                        SELECT TOP 3 * FROM (
                            SELECT 'Q-' + CAST(q.QuotationID AS VARCHAR) + ' approved by ' + c.CompanyName AS Details, 'blue' AS DotColor, q.DateCreated AS ActionDate, 'Recently' AS TimeAgo
                            FROM Quotations q 
                            INNER JOIN Clients c ON q.ClientID = c.ClientID 
                            WHERE q.Status = 'Approved'
                            
                            UNION ALL

                            SELECT N'&#8369;' + CAST(CAST(p.Amount AS INT) AS VARCHAR) + ' received from ' + c.CompanyName, 'orange', p.DueDate, 'Today'
                            FROM Payments p 
                            INNER JOIN Orders o ON p.OrderID = o.OrderID 
                            INNER JOIN Clients c ON o.ClientID = c.ClientID 
                            WHERE p.Status IN ('Paid','Partially Paid', 'Reversal')
                            
                            UNION ALL
                            
                            SELECT 'Production for ' + c.CompanyName + ' [Quote #' + CAST(q.QuotationID AS VARCHAR) + '] Completed', 'green', MAX(pr.Deadline), 'Recently' 
                            FROM Orders o 
                            INNER JOIN Production pr ON o.OrderID = pr.OrderID 
                            INNER JOIN Clients c ON o.ClientID = c.ClientID
                            INNER JOIN Quotations q ON o.QuotationID = q.QuotationID
                            WHERE pr.Status = 'Completed'
                            GROUP BY q.QuotationID, c.CompanyName
                        ) AS ActivityPool
                        ORDER BY ActionDate DESC";

                    using (SqlDataAdapter sda = new SqlDataAdapter(activityQuery, con))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            rptRecentActivity.DataSource = dt;
                            rptRecentActivity.DataBind();
                            lblNoActivity.Visible = false;
                        }
                        else
                        {
                            lblNoActivity.Visible = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Dashboard Activity Stream Error: " + ex.Message);
                lblNoActivity.Visible = true;
            }
        }
    }
}
