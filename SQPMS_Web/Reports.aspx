<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Reports.aspx.cs" Inherits="SQPMS.Reports" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <title>Financial Reports - Basic Bee Prints</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet" />
    <style>
        :root { --sidebar-bg: #0b3556; --sidebar-active: #16476d; --main-bg: #fdf3e7; --text-dark: #204060; --accent-green: #198754; }
        body { background-color: var(--main-bg); font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; overflow-x: hidden; }

        /* SIDEBAR NAVIGATION SYSTEM */
        .sidebar { width: 260px; background-color: var(--sidebar-bg); min-height: 100vh; position: fixed; top: 0; left: 0; padding: 20px 0; z-index: 100; }
        .brand-section { padding: 10px 24px 30px 24px; display: flex; align-items: center; gap: 12px; color: white; border-bottom: 1px solid rgba(255,255,255,0.05); }
        .brand-logo-placeholder { width: 42px; height: 42px; background: #ffccd5; border-radius: 10px; display: flex; align-items: center; justify-content: center; font-size: 1.2rem; }
        .brand-name { font-weight: 700; font-size: 1.1rem; line-height: 1.2; }
        .menu-category { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 1px; color: rgba(255,255,255,0.4); padding: 24px 24px 8px 24px; font-weight: 600; }
        .nav-menu-link { display: flex; align-items: center; justify-content: space-between; padding: 12px 24px; color: rgba(255,255,255,0.75); text-decoration: none; font-size: 0.95rem; transition: all 0.2s; font-weight: 500; }
        .nav-menu-link:hover, .nav-menu-link.active { background-color: var(--sidebar-active); color: white; }
        .nav-link-left { display: flex; align-items: center; gap: 14px; }
        .nav-menu-link i { font-size: 1.1rem; width: 20px; }
        .nav-badge { background: #f1948a; color: #78281f; font-size: 0.75rem; font-weight: 700; padding: 2px 8px; border-radius: 10px; }
        .nav-badge.blue { background: #aed6f1; color: #1b4f72; }
        .sidebar-footer { position: absolute; bottom: 0; width: 100%; padding: 20px 24px; border-top: 1px solid rgba(255,255,255,0.05); display: flex; align-items: center; justify-content: space-between; color: white; }

        /* MAIN CONTENT AREA */
        .main-content { margin-left: 260px; padding: 30px 40px; }
        .top-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 35px; }
        .header-title h1 { font-size: 1.6rem; font-weight: 700; color: var(--text-dark); margin: 0; }
        .metric-card { background: white; border-radius: 14px; padding: 20px; box-shadow: 0 4px 15px rgba(0,0,0,0.02); height: 100%; border-bottom: 5px solid #eaecee; display: flex; flex-direction: column; justify-content: center; }
        
        .metric-title { font-size: 0.8rem; font-weight: 700; color: #6c757d; text-transform: uppercase; margin-bottom: 5px; }
        
        /* CARD BOTTOM BORDER COLORS */
        .metric-card.sales { border-bottom: 5px solid #0d6efd; }
        .metric-card.materials { border-bottom: 5px solid #ffc107; }
        .metric-card.profit { border-bottom: 5px solid var(--accent-green); }
        .metric-card.collected { border-bottom: 5px solid #0dcaf0; }
        .metric-card.loss { border-bottom: 5px solid #dc3545; }
        
        .metric-value { font-size: 1.6rem; font-weight: 700; color: #1f3a52; line-height: 1.1; }
        .card { background: #ffffff; padding: 30px; border-radius: 14px; box-shadow: 0 4px 15px rgba(0,0,0,0.02); margin-bottom: 30px; border: none; }
        .table-custom { width: 100%; border-collapse: collapse; margin-top: 15px; background-color: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.01); }
        .table-custom th { background-color: #343a40; color: white; font-weight: 600; text-transform: uppercase; font-size: 0.78rem; padding: 12px 15px; }
        .table-custom td { padding: 12px 15px; border-bottom: 1px solid #eaeded; font-size: 0.9rem; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="sidebar">
            <div class="brand-section">
                <div class="brand-logo-placeholder">🌸</div>
                <div class="brand-name">Basic Bee<br />Prints</div>
            </div>

            <div class="menu-category">Overview</div>
            <a href="Dashboard.aspx" class="nav-menu-link">
                <div class="nav-link-left"><i class="fas fa-th-large"></i><span>Dashboard</span></div>
            </a>

            <asp:PlaceHolder ID="phSalesMenu" runat="server">
                <div class="menu-category">Sales</div>
                <a href="Quotation.aspx" class="nav-menu-link"><div class="nav-link-left"><i class="fas fa-file-invoice"></i><span>Quotations</span></div></a>
                <a href="AddOrder.aspx" class="nav-menu-link"><div class="nav-link-left"><i class="fas fa-users"></i><span>Clients & Orders</span></div></a>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="phOperationsMenu" runat="server">
                <div class="menu-category">Operations</div>
                <a href="Production.aspx" class="nav-menu-link"><div class="nav-link-left"><i class="fas fa-industry"></i><span>Production</span></div></a>
                <a href="Collection.aspx" class="nav-menu-link"><div class="nav-link-left"><i class="fas fa-money-bill-wave"></i><span>Collections</span></div></a>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="phOwnerMenu" runat="server">
                <div class="menu-category">Reports</div>
                <a href="Reports.aspx" class="nav-menu-link active"><div class="nav-link-left"><i class="fas fa-chart-bar"></i><span>Reports</span></div></a>
                <div class="menu-category">System</div>
                <a href="UserManagement.aspx" class="nav-menu-link"><div class="nav-link-left"><i class="fas fa-user-cog"></i><span>User Management</span></div></a>
            </asp:PlaceHolder>

            <div class="sidebar-footer">
                <div class="d-flex align-items-center gap-2">
                    <asp:Label ID="lblUserInitials" runat="server" CssClass="avatar-mini bg-secondary text-white rounded-circle d-flex align-items-center justify-content-center" style="width:32px; height:32px; font-size:0.8rem; font-weight:bold;"></asp:Label>
                    <asp:Label ID="lblUserRole" runat="server" style="font-size:0.85rem; font-weight:600; color: white;"></asp:Label>
                </div>
                <a href="Logout.aspx" style="text-decoration: none; color: white; display: inline-block; padding: 5px; cursor: pointer;">
                    <i class="fas fa-sign-out-alt" style="opacity: 0.7; pointer-events: none;"></i>
                </a>
            </div>
        </div>

        <div class="main-content">
            <div class="top-header">
                <div class="header-title">
                    <h1>Financial Report Analysis</h1>
                    <p>Live summary of product margins, structural material expenses, gross sales, and net revenue.</p>
                </div>
            </div>
            
            <div class="header-actions" style="margin-bottom: 25px; display: flex; gap: 15px; align-items: center; background: #fff; padding: 15px 25px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.02); flex-wrap: wrap;">
                <div class="d-flex align-items-center gap-3 flex-grow-1">
                    <label style="font-weight: 700; color: var(--text-dark); margin: 0; white-space: nowrap;"><i class="fas fa-filter"></i> Timeframe:</label>
                    <asp:DropDownList ID="ddlDateRange" runat="server" CssClass="form-select form-select-sm" style="max-width: 250px; font-size: 0.95rem;" AutoPostBack="true" OnSelectedIndexChanged="ddlDateRange_SelectedIndexChanged">
                        <asp:ListItem Text="All Time History" Value="AllTime"></asp:ListItem>
                        <asp:ListItem Text="This Month" Value="ThisMonth"></asp:ListItem>
                        <asp:ListItem Text="This Quarter" Value="ThisQuarter"></asp:ListItem>
                        <asp:ListItem Text="This Year (Annual)" Value="ThisYear"></asp:ListItem>
                    </asp:DropDownList>
                </div>
                <asp:Button ID="btnExportCSV" runat="server" Text="📥 Download Excel Report" 
                            CssClass="btn btn-primary" 
                            style="background-color: #0b3556; color: white; padding: 10px 20px; font-weight: bold; border: none; border-radius: 6px; cursor: pointer;" 
                            OnClick="btnExportCSV_Click" />
            </div>

            <div class="row g-3 mb-4 row-cols-1 row-cols-md-3 row-cols-xl-5">
                <div class="col">
                    <div class="metric-card sales">
                        <span class="metric-title">Total Gross Sales</span>
                        <div class="metric-value"><asp:Label ID="lblGrossSales" runat="server" Text="&#8369;0.00"></asp:Label></div>
                    </div>
                </div>
                <div class="col">
                    <div class="metric-card materials">
                        <span class="metric-title">Material Cost</span>
                        <div class="metric-value"><asp:Label ID="lblMaterialCosts" runat="server" Text="&#8369;0.00"></asp:Label></div>
                    </div>
                </div>
                <div class="col">
                    <div class="metric-card profit">
                        <span class="metric-title">Net Expected Profit</span>
                        <div class="metric-value" style="color: var(--accent-green);"><asp:Label ID="lblNetProfit" runat="server" Text="&#8369;0.00"></asp:Label></div>
                    </div>
                </div>
                
                <div class="col">
                    <div class="metric-card collected">
                        <span class="metric-title">Total Collected</span>
                        <div class="metric-value" style="color: #0dcaf0;"><asp:Label ID="lblTotalCollected" runat="server" Text="&#8369;0.00"></asp:Label></div>
                    </div>
                </div>
                <div class="col">
                    <div class="metric-card loss">
                        <span class="metric-title">Pending / Loss</span>
                        <div class="metric-value" style="color: #dc3545;"><asp:Label ID="lblTotalLoss" runat="server" Text="&#8369;0.00"></asp:Label></div>
                    </div>
                </div>
            </div>

            <div class="card">
                <h2>Itemized Profitability Breakdown Log</h2>
                <asp:GridView ID="gvFinancialLedger" runat="server" AutoGenerateColumns="False" CssClass="table-custom" GridLines="None">
                    <Columns>
                        <asp:BoundField DataField="OrderID" HeaderText="Order Ref #" />
                        <asp:BoundField DataField="CompanyName" HeaderText="Client Name" />
                        <asp:BoundField DataField="Item" HeaderText="Product Item Description" />
                        <asp:BoundField DataField="Qty" HeaderText="Qty" />
                        <asp:BoundField DataField="GrossSales" HeaderText="Gross Sales" DataFormatString="&#8369;{0:N2}" />
                        <asp:BoundField DataField="TotalMaterialCost" HeaderText="Resource Cost" DataFormatString="&#8369;{0:N2}" />
                        <asp:BoundField DataField="ItemNetProfit" HeaderText="Net Profit" DataFormatString="&#8369;{0:N2}" />
                        <asp:BoundField DataField="TotalPaid" HeaderText="Amt Collected" DataFormatString="&#8369;{0:N2}" ItemStyle-ForeColor="#0dcaf0" ItemStyle-Font-Bold="true" />
                        <asp:BoundField DataField="UnpaidBalance" HeaderText="Pending / Loss" DataFormatString="&#8369;{0:N2}" ItemStyle-ForeColor="#dc3545" ItemStyle-Font-Bold="true" />
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </form>
</body>
</html>