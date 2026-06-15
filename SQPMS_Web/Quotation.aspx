<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Quotations.aspx.cs" Inherits="SQPMS.Quotations" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Manage Quotations - Basic Bee Prints</title>
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
        .sidebar-footer { position: absolute; bottom: 0; width: 100%; padding: 20px 24px; border-top: 1px solid rgba(255,255,255,0.05); display: flex; align-items: center; justify-content: space-between; color: white; }

        /* MAIN CONTENT CONTAINER AREA */
        .main-content { margin-left: 260px; padding: 30px 40px; }
        .top-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 35px; }
        .header-title h1 { font-size: 1.6rem; font-weight: 700; color: var(--text-dark); margin: 0; }
        .header-title p { color: #85929e; font-size: 0.9rem; margin: 4px 0 0 0; }
        .header-actions { display: flex; align-items: center; gap: 20px; }

        /* CARDS & FORMS */
        .card { background: #ffffff; padding: 30px; border-radius: 14px; box-shadow: 0 4px 15px rgba(0,0,0,0.02); margin-bottom: 30px; border-top: 5px solid #0d6efd; border-left: none; border-right: none; border-bottom: none; }
        h2 { font-size: 1.3rem; font-weight: 700; color: var(--text-dark); border-bottom: 2px solid #eee; padding-bottom: 12px; margin-bottom: 25px; }
        .form-row { display: flex; flex-wrap: wrap; gap: 15px; margin-bottom: 15px; }
        .form-group { flex: 1; min-width: 200px; }
        .form-group label { display: block; font-weight: bold; margin-bottom: 5px; font-size: 0.9rem; color: #566573; }
        .form-control { width: 100%; padding: 10px; border: 1px solid #d5dbdb; border-radius: 6px; box-sizing: border-box; font-size: 0.95rem; background-color: #fcfcfc; }
        .btn-primary { background-color: #0d6efd; color: white; border: none; padding: 12px 24px; border-radius: 6px; cursor: pointer; font-weight: bold; }
        .btn-secondary { background-color: #6c757d; color: white; border: none; padding: 10px 15px; border-radius: 5px; cursor: pointer; margin-bottom: 20px; font-weight: bold; }

        /* DATA GRIDS & BADGES */
        .table-custom { width: 100%; border-collapse: collapse; margin-top: 15px; background-color: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.01); }
        .table-custom th, .table-custom td { padding: 12px 15px; text-align: left; font-size: 0.9rem; border-bottom: 1px solid #eaeded; }
        .table-custom th { background-color: #343a40; color: white; font-weight: 600; text-transform: uppercase; font-size: 0.78rem; letter-spacing: 0.5px; }
        
        .badge-pending { background-color: #fff3cd; color: #664d03; padding: 5px 10px; border-radius: 20px; font-size: 0.8rem; font-weight: 700; display: inline-block; }
        .badge-approved { background-color: #d1e7dd; color: #0f5132; padding: 5px 10px; border-radius: 20px; font-size: 0.8rem; font-weight: 700; display: inline-block; }
        .badge-rejected { background-color: #f8d7da; color: #842029; padding: 5px 10px; border-radius: 20px; font-size: 0.8rem; font-weight: 700; display: inline-block; }

        /* --- INVOICE SPECIFIC CSS --- */
        .invoice-container { background: white; padding: 50px; border-radius: 8px; box-shadow: 0 4px 20px rgba(0,0,0,0.05); color: #333; max-width: 900px; margin: 0 auto; }
        .invoice-header { display: flex; justify-content: space-between; border-bottom: 3px solid #0b3556; padding-bottom: 20px; margin-bottom: 30px; }
        .invoice-logo { font-size: 2rem; font-weight: 800; color: #0b3556; }
        .invoice-title { font-size: 2.5rem; font-weight: 900; color: #e0e0e0; text-transform: uppercase; letter-spacing: 2px; }
        .invoice-meta { display: flex; justify-content: space-between; margin-bottom: 40px; }
        .invoice-meta-box { background: #f8f9fa; padding: 20px; border-radius: 8px; width: 48%; }
        .invoice-meta-box h4 { margin: 0 0 10px 0; font-size: 1rem; color: #6c757d; text-transform: uppercase; border-bottom: 1px solid #dee2e6; padding-bottom: 5px;}
        .invoice-table { width: 100%; border-collapse: collapse; margin-bottom: 30px; }
        .invoice-table th { background: #0b3556; color: white; padding: 12px; text-align: left; }
        .invoice-table td { padding: 12px; border-bottom: 1px solid #dee2e6; }
        
        .invoice-total-box { float: right; width: 350px; background: #f8f9fa; padding: 20px; border-radius: 8px; text-align: right; }
        .invoice-total-row { display: flex; justify-content: space-between; margin-bottom: 10px; font-size: 1.1rem; }
        .invoice-total-row.grand-total { font-size: 1.4rem; font-weight: 800; color: #dc3545; border-top: 2px solid #dee2e6; padding-top: 10px; margin-top: 10px; }

        @media print {
            body * { visibility: hidden; }
            #printableArea, #printableArea * { visibility: visible; }
            #printableArea { position: absolute; left: 0; top: 0; width: 100%; }
            .no-print { display: none !important; }
            .main-content { margin-left: 0; padding: 0; }
            .invoice-container { box-shadow: none; padding: 0; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="sidebar no-print">
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
                <a href="Quotation.aspx" class="nav-menu-link active">
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
                <asp:Label ID="lblUserInitials" runat="server" CssClass="bg-secondary text-white rounded-circle d-flex align-items-center justify-content-center" style="width:32px; height:32px; font-size:0.8rem; font-weight:bold;"></asp:Label>
                <asp:Label ID="lblUserRole" runat="server" style="font-size:0.85rem; font-weight:600;"></asp:Label>
            </div>
            <a href="Logout.aspx" style="text-decoration: none; color: white; display: inline-block;">
                <i class="fas fa-sign-out-alt" style="opacity: 0.7;"></i>
            </a>
        </div>
        </div>

        <div class="main-content">
            
            <asp:Panel ID="pnlMainView" runat="server">
                <div class="top-header no-print">
                    <div class="header-title">
                        <h1>Manage Project Quotations</h1>
                        <p>Generate customer estimates, scope operational profiles, and review project cost histories.</p>
                    </div>
                </div>

                <div class="card no-print">
                    <h2>Quotation Detail Profile</h2>
                    <asp:Label ID="lblMsg" runat="server" Font-Bold="true" style="display:block; margin-bottom:15px;"></asp:Label>
                    <asp:HiddenField ID="hfEditingQuotationID" runat="server" Value="" />

                    <div class="form-row">
                        <div class="form-group">
                            <label>Select Client Target:</label>
                            <asp:DropDownList ID="ddlClients" runat="server" CssClass="form-control"></asp:DropDownList>
                        </div>
                        <div class="form-group">
                            <label>Project Name / Description:</label>
                            <asp:TextBox ID="txtProjectName" runat="server" CssClass="form-control"></asp:TextBox>
                        </div>
                        <div class="form-group">
                            <label>Quotation Status:</label>
                            <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-control">
                                <asp:ListItem Text="Pending" Value="Pending"></asp:ListItem>
                                <asp:ListItem Text="Approved" Value="Approved"></asp:ListItem>
                                <asp:ListItem Text="Rejected" Value="Rejected"></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>
                    
                    <div class="form-row">
                        <div class="form-group">
                            <asp:Button ID="btnSaveQuotation" runat="server" Text="+ Create Quotation Profile" CssClass="btn-primary" OnClick="btnSaveQuotation_Click" />
                            <asp:LinkButton ID="btnCancelEdit" runat="server" Text="Cancel Edit" OnClick="btnCancelEdit_Click" Visible="false" style="margin-left: 15px; color: #6c757d; font-weight: bold; text-decoration: none;" />
                        </div>
                    </div>
                </div>

                <div class="card no-print" style="border-top: 5px solid #343a40;">
                    <h2>Active Historical Proposals</h2>
                    <asp:GridView ID="gvQuotations" runat="server" AutoGenerateColumns="False" DataKeyNames="QuotationID,ClientID" 
                        OnRowCommand="gvQuotations_RowCommand" OnRowDeleting="gvQuotations_RowDeleting" OnRowDataBound="gvQuotations_RowDataBound" CssClass="table-custom" GridLines="None">
                        <Columns>
                            <asp:BoundField DataField="QuotationID" HeaderText="Ref #" />
                            <asp:BoundField DataField="ClientName" HeaderText="Client Name" ItemStyle-Font-Bold="true" />
                            <asp:BoundField DataField="ProjectName" HeaderText="Project Name" />
                            <asp:BoundField DataField="TotalAmount" HeaderText="Total Amount" DataFormatString="{0:C}" ItemStyle-Font-Bold="true" ItemStyle-ForeColor="Green" />
                            <asp:TemplateField HeaderText="Status">
                                <ItemTemplate>
                                    <asp:Label ID="lblGridStatus" runat="server" Text='<%# Eval("Status") %>'></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="DateCreated" HeaderText="Date Created" DataFormatString="{0:yyyy-MM-dd}" />
                            <asp:TemplateField HeaderText="Actions">
                                <ItemTemplate>
                                    <asp:LinkButton ID="btnInvoice" runat="server" CommandName="PrintInvoice" CommandArgument='<%# Container.DataItemIndex %>' Text="<i class='fas fa-file-invoice-dollar'></i> Invoice" style="text-decoration:none; font-weight:bold; margin-right:12px; color:#198754;" />
                                    <asp:LinkButton ID="btnEdit" runat="server" CommandName="SelectForEdit" CommandArgument='<%# Container.DataItemIndex %>' Text="Edit" style="text-decoration:none; font-weight:bold; margin-right:12px; color:#0d6efd;" />
                                    <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" OnClientClick="return confirm('Delete this quotation?');" Text="Delete" style="text-decoration:none; font-weight:bold; color:#dc3545;" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlInvoiceView" runat="server" Visible="false">
                <div class="mb-4 no-print d-flex justify-content-between align-items-center">
                    <asp:LinkButton ID="btnCloseInvoice" runat="server" Text="&larr; Back to Quotations" CssClass="btn-secondary" OnClick="btnCloseInvoice_Click" style="text-decoration:none; padding:10px 20px; display:inline-block;" />
                    <button type="button" class="btn-primary" onclick="window.print();"><i class="fas fa-print"></i> Print / Save as PDF</button>
                </div>

                <div id="printableArea" class="invoice-container">
                    <div class="invoice-header">
                        <div class="invoice-logo">🌸 Basic Bee Prints</div>
                        <div class="invoice-title">INVOICE</div>
                    </div>

                    <div class="invoice-meta">
                        <div class="invoice-meta-box">
                            <h4>Billed To</h4>
                            <div style="font-size:1.2rem; font-weight:bold; color:#0b3556; margin-bottom:5px;"><asp:Literal ID="litInvClientName" runat="server"></asp:Literal></div>
                            <div>Attn: <asp:Literal ID="litInvContact" runat="server"></asp:Literal></div>
                            <div><asp:Literal ID="litInvEmail" runat="server"></asp:Literal></div>
                            <div><asp:Literal ID="litInvPhone" runat="server"></asp:Literal></div>
                        </div>
                        <div class="invoice-meta-box text-end">
                            <div style="text-align: right; margin-bottom: 10px;">
                                <asp:Literal ID="litInvStatus" runat="server"></asp:Literal>
                            </div>
                            <h4>Invoice Details</h4>
                            <div><strong>Ref Number:</strong> #INV-<asp:Literal ID="litInvID" runat="server"></asp:Literal></div>
                            <div><strong>Date:</strong> <asp:Literal ID="litInvDate" runat="server"></asp:Literal></div>
                            <div><strong>Project:</strong> <asp:Literal ID="litInvProject" runat="server"></asp:Literal></div>
                        </div>
                    </div>

                    <asp:GridView ID="gvInvoiceItems" runat="server" AutoGenerateColumns="False" CssClass="invoice-table" GridLines="None">
                        <Columns>
                            <asp:BoundField DataField="Item" HeaderText="Description" />
                            <asp:BoundField DataField="Details" HeaderText="Specs (Color/Size)" />
                            <asp:BoundField DataField="Qty" HeaderText="Qty" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                            <asp:BoundField DataField="UnitCost" HeaderText="Unit Price" DataFormatString="{0:C}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
                            <asp:BoundField DataField="LineTotal" HeaderText="Line Total" DataFormatString="{0:C}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" ItemStyle-Font-Bold="true" />
                        </Columns>
                    </asp:GridView>

                    <div class="invoice-total-box">
                        <div class="invoice-total-row" style="color: #566573;">
                            <span>Subtotal / Grand Total:</span>
                            <span><asp:Literal ID="litInvGrandTotal" runat="server"></asp:Literal></span>
                        </div>
                        <div class="invoice-total-row" style="color: #198754;">
                            <span>Less Total Paid:</span>
                            <span><asp:Literal ID="litInvTotalPaid" runat="server"></asp:Literal></span>
                        </div>
                        <div class="invoice-total-row grand-total">
                            <span style="color:#0b3556;">Balance Due:</span>
                            <span><asp:Literal ID="litInvBalance" runat="server"></asp:Literal></span>
                        </div>
                    </div>

                    <div style="clear:both; margin-top:100px; text-align:center; color:#85929e; font-size:0.9rem; border-top:1px solid #eee; padding-top:20px;">
                        Thank you for your business! Please make payments payable to Basic Bee Prints.
                    </div>
                </div>
            </asp:Panel>

        </div>
    </form>
   <script type="text/javascript">
       window.onpageshow = function (event) {
           if (event.persisted || (window.performance && window.performance.navigation.type === 2)) {
               window.location.href = "Login.aspx";
           }
       };
   </script>
</body>
</html>