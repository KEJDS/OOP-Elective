<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="SQPMS.Dashboard" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Dashboard - Basic Bee Prints</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet" />
    <style>
        :root {
            --sidebar-bg: #0b3556;
            --sidebar-active: #16476d;
            --main-bg: #fdf3e7; 
            --text-dark: #204060;
            --card-border-pending: #1d4e73;
            --card-border-overdue: #e06d6d;
        }

        body { 
            background-color: var(--main-bg); 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            color: #333;
            overflow-x: hidden;
        }

        /* SIDEBAR NAVIGATION SYSTEM */
        .sidebar {
            width: 260px;
            background-color: var(--sidebar-bg);
            min-height: 100vh;
            position: fixed;
            top: 0;
            left: 0;
            padding: 20px 0;
            z-index: 100;
        }
        .brand-section {
            padding: 10px 24px 30px 24px;
            display: flex;
            align-items: center;
            gap: 12px;
            color: white;
            border-bottom: 1px solid rgba(255,255,255,0.05);
        }
        .brand-logo-placeholder {
            width: 42px;
            height: 42px;
            background: #ffccd5;
            border-radius: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.2rem;
        }
        .brand-name { font-weight: 700; font-size: 1.1rem; line-height: 1.2; }
        
        .menu-category {
            font-size: 0.75rem;
            text-transform: uppercase;
            letter-spacing: 1px;
            color: rgba(255,255,255,0.4);
            padding: 24px 24px 8px 24px;
            font-weight: 600;
        }
        .nav-menu-link {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 12px 24px;
            color: rgba(255,255,255,0.75);
            text-decoration: none;
            font-size: 0.95rem;
            transition: all 0.2s;
            font-weight: 500;
        }
        .nav-menu-link:hover, .nav-menu-link.active {
            background-color: var(--sidebar-active);
            color: white;
        }
        .nav-link-left { display: flex; align-items: center; gap: 14px; }
        .nav-menu-link i { font-size: 1.1rem; width: 20px; }
        .nav-badge {
            background: #f1948a;
            color: #78281f;
            font-size: 0.75rem;
            font-weight: 700;
            padding: 2px 8px;
            border-radius: 10px;
        }
        .nav-badge.blue { background: #aed6f1; color: #1b4f72; }

        .sidebar-footer {
            position: absolute;
            bottom: 0;
            width: 100%;
            padding: 20px 24px;
            border-top: 1px solid rgba(255,255,255,0.05);
            display: flex;
            align-items: center;
            justify-content: space-between;
            color: white;
        }

        /* MAIN CONTENT AREA WINDOW */
        .main-content {
            margin-left: 260px;
            padding: 30px 40px;
        }

        /* TOP BANNER ACTIONS HEADER */
        .top-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 35px;
        }
        .header-title h1 { font-size: 1.6rem; font-weight: 700; color: var(--text-dark); margin: 0; }
        .header-title p { color: #85929e; font-size: 0.9rem; margin: 4px 0 0 0; }
        .header-actions { display: flex; align-items: center; gap: 20px; }
        .user-chip {
            background: #f0e4d7;
            padding: 6px 16px;
            border-radius: 20px;
            font-size: 0.85rem;
            font-weight: 600;
            color: #2c3e50;
            display: flex;
            align-items: center;
            gap: 8px;
            text-transform: capitalize;
        }
        .status-dot { width: 8px; height: 8px; background: #27ae60; border-radius: 50%; }
        .notification-bell { color: #f39c12; font-size: 1.3rem; cursor: pointer; }

        /* SUMMARY TILES */
        .metric-card {
            background: white;
            border: none;
            border-radius: 14px;
            padding: 24px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.02);
            height: 100%;
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            border-bottom: 5px solid #eaecee;
        }
        .metric-card.sales { border-bottom: 5px solid #5499c7; }
        .metric-card.quotations { border-bottom: 5px solid #48c9b0; }
        .metric-card.collections { border-bottom: 5px solid var(--card-border-pending); }
        .metric-card.production { border-bottom: 5px solid var(--card-border-overdue); }
        
        .metric-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 15px; }
        .metric-title { font-size: 0.82rem; font-weight: 700; color: #85929e; text-transform: uppercase; letter-spacing: 0.5px; }
        .metric-icon-box {
            width: 36px;
            height: 36px;
            border-radius: 8px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.1rem;
            background: #f2f4f4;
            color: #7f8c8d;
        }
        .metric-card.sales .metric-icon-box { background: #e8f4f8; color: #2980b9; }
        .metric-card.quotations .metric-icon-box { background: #e8f8f5; color: #117a65; }
        .metric-card.collections .metric-icon-box { background: #fef9e7; color: #b7950b; }
        .metric-card.production .metric-icon-box { background: #fbeee6; color: #a04000; }

        .metric-value { font-size: 1.9rem; font-weight: 700; color: #1f3a52; line-height: 1.1; margin-bottom: 6px; }
        .metric-subtext { font-size: 0.82rem; font-weight: 500; color: #95a5a6; }
        .metric-subtext.positive { color: #27ae60; }
        .metric-subtext.negative { color: #e74c3c; }

        .content-card {
            background: white;
            border-radius: 14px;
            padding: 25px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.02);
            height: 100%;
        }
        .content-card-title { font-size: 1rem; font-weight: 700; color: var(--text-dark); margin-bottom: 20px; }

        /* RECENT ACTIVITY STREAM */
        .activity-stream { position: relative; padding-left: 24px; }
        .activity-stream::before {
            content: '';
            position: absolute;
            left: 5px;
            top: 10px;
            bottom: 10px;
            width: 2px;
            background: #ebedef;
        }
        .activity-item { position: relative; margin-bottom: 24px; }
        .activity-item:last-child { margin-bottom: 0; }
        .activity-dot {
            position: absolute;
            left: -24px;
            top: 4px;
            width: 12px;
            height: 12px;
            border-radius: 50%;
            background: #bdc3c7;
            border: 2px solid white;
            box-shadow: 0 0 0 2px #ebedef;
        }
        .activity-dot.green { background: #27ae60; box-shadow: 0 0 0 2px #d5f5e3; }
        .activity-dot.blue { background: #3498db; box-shadow: 0 0 0 2px #ebf5fb; }
        .activity-dot.orange { background: #e67e22; box-shadow: 0 0 0 2px #fdf2e9; }
        
        .activity-details { font-size: 0.9rem; font-weight: 600; color: #2c3e50; }
        .activity-time { font-size: 0.78rem; color: #95a5a6; margin-top: 2px; font-weight: 500; }

        /* PROGRESS BAR BAR CHART COMPONENT */
        .bar-chart-container { display: flex; align-items: flex-end; justify-content: space-around; height: 160px; padding-top: 10px; }
        .bar-group { display: flex; flex-direction: column; align-items: center; justify-content: flex-end; height: 100%; width: 15%; }
        .bar-bars { display: flex; gap: 4px; align-items: flex-end; height: 130px; width: 100%; justify-content: center; }
        .chart-bar { width: 14px; border-radius: 3px 3px 0 0; min-height: 5px; }
        .chart-bar.sales-bar { background: #0b3556; }
        .chart-bar.expenses-bar { background: #cbd6df; }
        .bar-label { font-size: 0.72rem; color: #7f8c8d; font-weight: bold; margin-top: 6px; text-transform: uppercase; }
        
        .profit-banner {
            background: #fdf2e9;
            padding: 10px 18px;
            border-radius: 8px;
            margin-top: 25px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            font-size: 0.88rem;
            font-weight: 700;
            color: var(--text-dark);
        }
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
            <a href="Dashboard.aspx" class="nav-menu-link active">
                <div class="nav-link-left"><i class="fas fa-th-large"></i><span>Dashboard</span></div>
            </a>

            <asp:PlaceHolder ID="phSalesMenu" runat="server">
                <div class="menu-category">Sales</div>
                <a href="Quotation.aspx" class="nav-menu-link">
                    <div class="nav-link-left"><i class="fas fa-file-invoice"></i><span>Quotations</span></div>
                </a>
                <a href="AddOrder.aspx" class="nav-menu-link">
                    <div class="nav-link-left"><i class="fas fa-users"></i><span>Clients & Orders</span></div>
                </a>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="phOperationsMenu" runat="server">
                <div class="menu-category">Operations</div>
                <a href="Production.aspx" class="nav-menu-link">
                    <div class="nav-link-left"><i class="fas fa-industry"></i><span>Production</span></div>
                </a>
                <a href="Collection.aspx" class="nav-menu-link">
                    <div class="nav-link-left"><i class="fas fa-money-bill-wave"></i><span>Collections</span></div>
                </a>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="phOwnerMenu" runat="server">
                <div class="menu-category">Reports</div>
                <a href="Reports.aspx" class="nav-menu-link"><div class="nav-link-left"><i class="fas fa-chart-bar"></i><span>Reports</span></div></a>
                <div class="menu-category">System</div>
                <a href="UserManagement.aspx" class="nav-menu-link"><div class="nav-link-left"><i class="fas fa-user-cog"></i><span>User Management</span></div></a>
            </asp:PlaceHolder>

            <div class="sidebar-footer">
                <div class="d-flex align-items-center gap-2">
                    <div class="avatar-mini bg-secondary text-white rounded-circle d-flex align-items-center justify-content-center" style="width:32px; height:32px; font-size:0.8rem; font-weight:bold;">
                        <asp:Literal ID="litUserInitials" runat="server" Text="U"></asp:Literal>
                    </div>
                    <span style="font-size:0.85rem; font-weight:600;">
                        <asp:Literal ID="litUserSidebarLabel" runat="server" Text="User"></asp:Literal>
                    </span>
                </div>
                <a href="Logout.aspx" style="text-decoration: none; color: inherit; display: inline-block; padding: 5px; cursor: pointer;">
                    <i class="fas fa-sign-out-alt" style="opacity: 0.7; pointer-events: none;"></i>
                </a>            
            </div>
        </div>

        <div class="main-content">
            
            <div class="top-header">
                <div class="header-title">
                    <h1>Dashboard</h1>
                    <p>Welcome back! Here's your business overview.</p>
                </div>
                <div class="header-actions">
                    <div class="user-chip">
                        <div class="status-dot"></div>
                        <span><asp:Literal ID="litUserRoleHeader" runat="server" Text="User"></asp:Literal></span>
                    </div>
                    <i class="fas fa-bell notification-bell"></i>
                </div>
            </div>

            <div class="row g-3 mb-4">
                <div class="col-xl-3 col-md-6">
                    <div class="metric-card sales">
                        <div class="metric-header">
                            <span class="metric-title">Total Sales (Month)</span>
                            <div class="metric-icon-box"><i class="fas fa-briefcase"></i></div>
                        </div>
                        <div>
                            <div class="metric-value">
                                <asp:Label ID="lblTotalSales" runat="server" Text="₱0"></asp:Label>
                            </div>
                           <div class="metric-subtext" id="divSalesTrend" runat="server">
    <asp:Literal ID="litTrendIcon" runat="server"></asp:Literal>
    <asp:Label ID="lblSalesTrend" runat="server" Text="0% vs Prev"></asp:Label>
</div>
                        </div>
                    </div>
                </div>

                <div class="col-xl-3 col-md-6">
                    <div class="metric-card quotations">
                        <div class="metric-header">
                            <span class="metric-title">Active Quotations</span>
                            <div class="metric-icon-box"><i class="fas fa-file-alt"></i></div>
                        </div>
                        <div>
                            <div class="metric-value">
                                <asp:Label ID="lblActiveQuotations" runat="server" Text="0"></asp:Label>
                            </div>
                            <div class="metric-subtext" style="color:#117a65;"></div>
                        </div>
                    </div>
                </div>

                <div class="col-xl-3 col-md-6">
                    <div class="metric-card collections">
                        <div class="metric-header">
                            <span class="metric-title">Pending Collections</span>
                            <div class="metric-icon-box"><i class="fas fa-sack-dollar"></i></div>
                        </div>
                        <div>
                            <div class="metric-value">
                                <asp:Label ID="lblPendingCollections" runat="server" Text="₱0"></asp:Label>
                            </div>
                            <div class="metric-subtext negative">
                                <i class="fas fa-exclamation-triangle me-1"></i>
                                <asp:Label ID="lblOverdueSubtext" runat="server" Text="0 overdue"></asp:Label>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-xl-3 col-md-6">
                    <div class="metric-card production">
                        <div class="metric-header">
                            <span class="metric-title">In Production</span>
                            <div class="metric-icon-box"><i class="fas fa-boxes"></i></div>
                        </div>
                        <div>
                            <div class="metric-value">
                                <asp:Label ID="lblInProduction" runat="server" Text="0"></asp:Label>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row g-4">
                <div class="col-lg-7">
                    <div class="content-card">
                        <div class="d-flex align-items-center justify-content-between mb-2">
                            <span class="content-card-title">Monthly Sales vs Expenses</span>
                            <div class="d-flex gap-3" style="font-size:0.8rem; font-weight:600;">
                                <span><i class="fas fa-square me-1" style="color:#0b3556;"></i>Sales</span>
                                <span><i class="fas fa-square me-1" style="color:#cbd6df;"></i>Pending Balances</span>
                            </div>
                        </div>
                        
                        <div class="bar-chart-container">
                            <asp:Repeater ID="rptMonthlyChart" runat="server">
                                <ItemTemplate>
                                    <div class="bar-group">
                                        <div class="bar-bars">
                                            <div class="chart-bar expenses-bar" style='height: <%# Eval("ExpenseHeight") %>px;' title='Pending: ₱<%# Eval("TotalPending","{0:N0}") %>'></div>
                                            <div class="chart-bar sales-bar" style='height: <%# Eval("SalesHeight") %>px;' title='Paid: ₱<%# Eval("TotalPaid","{0:N0}") %>'></div>
                                        </div>
                                        <span class="bar-label"><%# Eval("MonthLabel") %></span>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>

                       <div class="profit-banner">
    <span>Collection Rate — Month</span>
    <span>
        <asp:Label ID="lblProfitMargin" runat="server" Text="0.0%"></asp:Label>
    </span>
</div>
                    </div>
                </div>

                <div class="col-lg-5">
                    <div class="content-card">
                        <div class="d-flex align-items-center justify-content-between mb-4">
                            <span class="content-card-title" style="margin:0;">Recent Activity Log</span>
                            <span style="font-size:0.8rem; color:#95a5a6; font-weight:600;">Latest</span>
                        </div>
                        
                        <div class="activity-stream">
                            <asp:Repeater ID="rptRecentActivity" runat="server">
                                <ItemTemplate>
                                    <div class="activity-item">
                                        <div class='<%# "activity-dot " + Eval("DotColor") %>'></div>
                                        <div class="activity-details"><%# Eval("Details") %></div>
                                        <div class="activity-time"><%# Eval("TimeAgo") %></div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                            <asp:Label ID="lblNoActivity" runat="server" Text="No activities recorded today." Visible="false" style="font-size:0.9rem; color:#aaa; font-style:italic;" />
                        </div>
                    </div>
                </div>
            </div>

        </div>

   <script type="text/javascript">
       window.onpageshow = function (event) {
           // If the browser tries to load from 'bfcache' (Back/Forward Cache), force a reload
           if (event.persisted || (window.performance && window.performance.navigation.type === 2)) {
               window.location.href = "Login.aspx"; // Force redirect to login if they try to go back
           }
       };
   </script>
    </form>
</body>
</html>