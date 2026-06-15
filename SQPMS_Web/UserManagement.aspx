<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UserManagement.aspx.cs" Inherits="SQPMS.UserManagement" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>User Management - Basic Bee Prints</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet" />
    <style>
        :root {
            --sidebar-bg: #0b3556;
            --sidebar-active: #16476d;
            --main-bg: #fdf3e7; 
            --text-dark: #204060;
        }

        body { 
            background-color: var(--main-bg); 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            color: #333;
            overflow-x: hidden;
        }

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

        /* MAIN CONTENT AREA WINDOW */
        .main-content { margin-left: 260px; padding: 30px 40px; }
        .top-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 35px; }
        .header-title h1 { font-size: 1.6rem; font-weight: 700; color: var(--text-dark); margin: 0; }
        .header-title p { color: #85929e; font-size: 0.9rem; margin: 4px 0 0 0; }
        .header-actions { display: flex; align-items: center; gap: 20px; }
        .user-chip { background: #f0e4d7; padding: 6px 16px; border-radius: 20px; font-size: 0.85rem; font-weight: 600; color: #2c3e50; display: flex; align-items: center; gap: 8px; }
        .status-dot { width: 8px; height: 8px; background: #27ae60; border-radius: 50%; }

        /* FORM & CONTAINER BLOCKS */
        .card { background: #ffffff; padding: 30px; border-radius: 14px; box-shadow: 0 4px 15px rgba(0,0,0,0.02); margin-bottom: 30px; border-top: 5px solid #6f42c1; border-left: none; border-right: none; border-bottom: none; }
        h2 { font-size: 1.3rem; font-weight: 700; color: var(--text-dark); border-bottom: 2px solid #eee; padding-bottom: 12px; margin-bottom: 25px; }
        
        .form-row { display: flex; flex-wrap: wrap; gap: 15px; margin-bottom: 15px; }
        .form-group { flex: 1; min-width: 200px; }
        .form-group label { display: block; font-weight: bold; margin-bottom: 5px; font-size: 0.9rem; color: #566573; }
        .form-control { width: 100%; padding: 10px; border: 1px solid #d5dbdb; border-radius: 6px; box-sizing: border-box; font-size: 0.95rem; background-color: #fcfcfc; }
        .form-control:focus { border-color: #3498db; outline: none; background-color: #fff; }
        
        .btn-purple { background-color: #6f42c1; color: white; border: none; padding: 12px 24px; border-radius: 6px; cursor: pointer; font-weight: bold; transition: background 0.2s; }
        .btn-purple:hover { background-color: #59359a; }
        
        /* DATA GRIDS */
        .table-custom { width: 100%; border-collapse: collapse; margin-top: 15px; background-color: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.01); }
        .table-custom th, .table-custom td { padding: 12px 15px; text-align: left; font-size: 0.9rem; border-bottom: 1px solid #eaeded; }
        .table-custom th { background-color: #343a40; color: white; font-weight: 600; text-transform: uppercase; font-size: 0.78rem; letter-spacing: 0.5px; }
        .table-custom tr:nth-child(even) { background-color: #fafbfc; }
        
        .badge-role { background-color: #e8daef; color: #6c3483; padding: 5px 12px; border-radius: 20px; font-size: 0.8rem; font-weight: 700; display: inline-block; }
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
                <a href="Reports.aspx" class="nav-menu-link">
                    <div class="nav-link-left"><i class="fas fa-chart-bar"></i><span>Reports</span></div>
                </a>
                <div class="menu-category">System</div>
                <a href="UserManagement.aspx" class="nav-menu-link active">
                    <div class="nav-link-left"><i class="fas fa-user-cog"></i><span>User Management</span></div>
                </a>
            </asp:PlaceHolder>

           <div class="sidebar-footer">
    <div class="d-flex align-items-center gap-2">
        <asp:Label ID="lblUserInitials" runat="server" 
            CssClass="avatar-mini bg-secondary text-white rounded-circle d-flex align-items-center justify-content-center" 
            style="width:32px; height:32px; font-size:0.8rem; font-weight:bold;"></asp:Label>
        
        <asp:Label ID="lblUserRole" runat="server" 
            style="font-size:0.85rem; font-weight:600; color: white;"></asp:Label>
    </div>
    <a href="Logout.aspx" style="text-decoration: none; color: white; display: inline-block; padding: 5px; cursor: pointer;">
        <i class="fas fa-sign-out-alt" style="opacity: 0.7; pointer-events: none;"></i>
    </a>
</div>
        </div>

        <div class="main-content">
            <div class="top-header">
                <div class="header-title">
                    <h1>User Account Management</h1>
                    <p>Provision security access roles, audit operator authorization status, and manage profile credentials.</p>
                </div>
            </div>

            <div class="card">
                <h2>Manage System Accounts</h2>
                <asp:Label ID="lblMsg" runat="server" Font-Bold="true" style="display:block; margin-bottom:15px;"></asp:Label>
                <asp:HiddenField ID="hfEditingUserID" runat="server" Value="" />

                <div class="form-row">
                    <div class="form-group">
                        <label>Account Username:</label>
                        <asp:TextBox ID="txtUsername" runat="server" CssClass="form-control" placeholder="e.g. edrian_sales"></asp:TextBox>
                    </div>
                    <div class="form-group">
                        <label>Security Password:</label>
                        <asp:TextBox ID="txtPassword" runat="server" CssClass="form-control" TextMode="Password" placeholder="••••••••"></asp:TextBox>
                    </div>
                    <div class="form-group">
                        <label>Assign Security Role:</label>
                        <asp:DropDownList ID="ddlRole" runat="server" CssClass="form-control">
                            <asp:ListItem Text="Owner" Value="owner"></asp:ListItem>
                            <asp:ListItem Text="Sales" Value="sales"></asp:ListItem>
                            <asp:ListItem Text="Operation" Value="operation"></asp:ListItem>
                        </asp:DropDownList>
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <asp:Button ID="btnSaveUser" runat="server" Text="✔ Save User Account" CssClass="btn-purple" OnClick="btnSaveUser_Click" />
                        <asp:LinkButton ID="btnCancelEdit" runat="server" Text="Cancel Edit" OnClick="btnCancelEdit_Click" Visible="false" style="margin-left:15px; color:#6c757d; text-decoration:none; font-weight:bold;" />
                    </div>
                </div>

                <h2>Registered Operators Matrix</h2>
                <asp:GridView ID="gvUsers" runat="server" AutoGenerateColumns="False" DataKeyNames="UserID"
                    OnRowCommand="gvUsers_RowCommand" OnRowDeleting="gvUsers_RowDeleting" CssClass="table-custom" GridLines="None">
                    <Columns>
                        <asp:BoundField DataField="UserID" HeaderText="User ID" />
                        <asp:BoundField DataField="Username" HeaderText="Username" ItemStyle-Font-Bold="true" />
                        <asp:TemplateField HeaderText="Access Role Level">
                            <ItemTemplate>
                                <asp:Label ID="lblRole" runat="server" Text='<%# Eval("Role") %>' CssClass="badge-role"></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Actions">
                            <ItemTemplate>
                                <asp:LinkButton ID="btnEdit" runat="server" CommandName="SelectForEdit" CommandArgument='<%# Container.DataItemIndex %>' Text="Edit" style="text-decoration:none; font-weight:bold; margin-right:8px; color:#0d6efd;" />
                                <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" Text="Remove" OnClientClick="return confirm('Permanently drop this operator account access link?');" style="text-decoration:none; font-weight:bold; color:#dc3545;" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </form>
   <script type="text/javascript">
       window.onpageshow = function (event) {
           // If the browser tries to load from 'bfcache' (Back/Forward Cache), force a reload
           if (event.persisted || (window.performance && window.performance.navigation.type === 2)) {
               window.location.href = "Login.aspx"; // Force redirect to login if they try to go back
           }
       };
   </script>
</body>
</html>