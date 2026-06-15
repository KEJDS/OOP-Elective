<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AddOrder.aspx.cs" Inherits="SQPMS.AddOrder" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Manage Clients & Orders - Basic Bee Prints</title>
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
        .notification-bell { color: #f39c12; font-size: 1.3rem; cursor: pointer; }

        /* CARDS Styling */
        .card { background: #ffffff; padding: 30px; border-radius: 14px; box-shadow: 0 4px 15px rgba(0,0,0,0.02); margin-bottom: 30px; border-top: 5px solid #ffc107; border-left: none; border-right: none; border-bottom: none; }
        .card-clients { border-top: 5px solid #0d6efd; }
        .card-orders { border-top: 5px solid #198754; }
        .card-history { border-top: 5px solid #6f42c1; }
        h2 { font-size: 1.3rem; font-weight: 700; color: var(--text-dark); border-bottom: 2px solid #eee; padding-bottom: 12px; margin-bottom: 25px; }
         
        .form-row { display: flex; flex-wrap: wrap; gap: 15px; margin-bottom: 15px; }
        .form-row { display: flex; flex-wrap: wrap; gap: 15px; margin-bottom: 15px; }
        .form-group { flex: 1; min-width: 200px; }
        .form-group label { display: block; font-weight: bold; margin-bottom: 5px; font-size: 0.9rem; color: #566573; }
        .form-control { width: 100%; padding: 10px; border: 1px solid #d5dbdb; border-radius: 6px; box-sizing: border-box; font-size: 0.95rem; background-color: #fcfcfc; }
        .form-control:focus { border-color: #3498db; outline: none; background-color: #fff; }
         
        .btn-primary { background-color: #0d6efd; color: white; border: none; padding: 10px 20px; border-radius: 6px; cursor: pointer; font-weight: bold; transition: background 0.2s; }
        .btn-primary:hover { background-color: #0b5ed7; }
        .btn-success { background-color: #198754; color: white; border: none; padding: 12px 24px; border-radius: 6px; cursor: pointer; font-weight: bold; transition: background 0.2s; }
        .btn-success:hover { background-color: #146c43; }
        .btn-warning { background-color: #ffc107; color: #212529; border: none; padding: 10px 20px; border-radius: 6px; cursor: pointer; font-weight: bold; transition: background 0.2s; }
        .btn-warning:hover { background-color: #e0a800; }
        .btn-secondary { background-color: #6c757d; color: white; border: none; padding: 10px 15px; border-radius: 5px; cursor: pointer; margin-bottom: 20px; font-weight: bold; }
         
        .table-custom { width: 100%; border-collapse: collapse; margin-top: 15px; background-color: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.01); }
        .table-custom th, .table-custom td { padding: 12px 15px; text-align: left; font-size: 0.9rem; border-bottom: 1px solid #eaeded; }
        .table-custom th { background-color: #343a40; color: white; font-weight: 600; text-transform: uppercase; font-size: 0.78rem; letter-spacing: 0.5px; }
        .table-custom tr:nth-child(even) { background-color: #fafbfc; }
        .table-custom tr:hover { background-color: #f2f4f4; }
         
        .badge-active { background-color: #d1e7dd; color: #0f5132; padding: 5px 10px; border-radius: 20px; font-size: 0.8rem; font-weight: 700; display: inline-block; }
        .badge-inactive { background-color: #f8d7da; color: #842029; padding: 5px 10px; border-radius: 20px; font-size: 0.8rem; font-weight: 700; display: inline-block; }
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
                <a href="AddOrder.aspx" class="nav-menu-link active">
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
                <a href="UserManagement.aspx" class="nav-menu-link">
                    <div class="nav-link-left"><i class="fas fa-user-cog"></i><span>User Management</span></div>
                </a>
            </asp:PlaceHolder>

          <div class="sidebar-footer">
    <div class="d-flex align-items-center gap-2">
        <asp:Label ID="lblUserInitials" runat="server" CssClass="bg-secondary text-white rounded-circle d-flex align-items-center justify-content-center" style="width:32px; height:32px; font-size:0.8rem; font-weight:bold;"></asp:Label>
        <asp:Label ID="lblUserRole" runat="server" style="font-size:0.85rem; font-weight:600;"></asp:Label>
    </div>
    <a href="Logout.aspx" style="text-decoration: none; color: inherit; display: inline-block; width: auto; cursor: pointer;">
        <i class="fas fa-sign-out-alt" style="opacity: 0.7;"></i>
    </a>
</div>
        </div>

        <div class="main-content">
             
            <div class="top-header">
                <div class="header-title">
                    <h1>Clients & Ordering Center</h1>
                    <p>Configure product catalog values, assign order queues, and audit systemic log records.</p>
                </div>
                <div class="header-actions">
                   
                    <i class="fas fa-bell notification-bell"></i>
                </div>
            </div>

            <div class="card">
                <h2>1. Manage Product Catalog</h2>
                <asp:Label ID="lblProdMsg" runat="server" Font-Bold="true" style="display:block; margin-bottom:15px;"></asp:Label>
                <asp:HiddenField ID="hfEditingProductID" runat="server" Value="" />
                 
                <div class="form-row">
                    <div class="form-group"><label>Product Name:</label><asp:TextBox ID="txtProdName" runat="server" CssClass="form-control" placeholder="e.g. Mug, Tote Bag"></asp:TextBox></div>
                    <div class="form-group"><label>Standard Colors (Use Comma separation):</label><asp:TextBox ID="txtProdColor" runat="server" CssClass="form-control" placeholder="e.g. Red, Blue, Pink, White"></asp:TextBox></div>
                    <div class="form-group"><label>Standard Sizes (Use Comma separation):</label><asp:TextBox ID="txtProdSize" runat="server" CssClass="form-control" placeholder="e.g. Asian Size"></asp:TextBox></div>
                </div>
                <div class="form-row">
                    <div class="form-group"><label>Base Selling Price ($):</label><asp:TextBox ID="txtProdPrice" runat="server" CssClass="form-control" TextMode="Number" step="0.01"></asp:TextBox></div>
                    <div class="form-group"><label>Material Cost ($):</label><asp:TextBox ID="txtMaterialCost" runat="server" CssClass="form-control" TextMode="Number" step="0.01"></asp:TextBox></div>
                </div>
                <div class="form-row">
                    <div class="form-group">
                        <asp:Button ID="btnSaveProduct" runat="server" Text="+ Add to Catalog" CssClass="btn-warning" OnClick="btnSaveProduct_Click" style="font-weight:bold;" />
                        <asp:LinkButton ID="btnCancelProductEdit" runat="server" Text="Cancel Edit" OnClick="btnCancelProductEdit_Click" Visible="false" style="margin-left: 15px; color: #6c757d; font-weight: bold; text-decoration: none;" />
                    </div>
                </div>
                 
                <asp:GridView ID="gvProducts" runat="server" AutoGenerateColumns="False" DataKeyNames="ProductID" OnRowCommand="gvProducts_RowCommand" OnRowDeleting="gvProducts_RowDeleting" CssClass="table-custom" GridLines="None">
                    <Columns>
                        <asp:BoundField DataField="ProductName" HeaderText="Item Name" />
                        <asp:BoundField DataField="StandardColor" HeaderText="Available Colors" />
                        <asp:BoundField DataField="StandardSize" HeaderText="Available Sizes" />
                        <asp:BoundField DataField="UnitPrice" HeaderText="Selling Price" DataFormatString="{0:C}" />
                        <asp:BoundField DataField="MaterialCost" HeaderText="Material Cost" DataFormatString="{0:C}" />
                        <asp:TemplateField HeaderText="Actions">
                            <ItemTemplate>
                                <asp:LinkButton ID="btnEditProduct" runat="server" CommandName="SelectProductForEdit" CommandArgument='<%# Container.DataItemIndex %>' Text="Edit" style="text-decoration:none; font-weight:bold; margin-right:8px; color:#0d6efd;" />
                                <asp:LinkButton ID="btnDeleteProduct" runat="server" CommandName="Delete" Text="Remove" OnClientClick="return confirm('Remove from catalog?');" style="text-decoration:none; font-weight:bold; color:#dc3545;" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>

            <div class="card card-clients">
                <h2>2. Manage Clients</h2>
                <asp:Label ID="lblClientMsg" runat="server" Font-Bold="true" style="display:block; margin-bottom:15px;"></asp:Label>
                <asp:HiddenField ID="hfEditingClientID" runat="server" Value="" />
                <div class="form-row">
                    <div class="form-group"><label>Company Name:</label><asp:TextBox ID="txtCompany" runat="server" CssClass="form-control"></asp:TextBox></div>
                    <div class="form-group"><label>Contact Person:</label><asp:TextBox ID="txtContact" runat="server" CssClass="form-control"></asp:TextBox></div>
                    <div class="form-group"><label>Email:</label><asp:TextBox ID="txtEmail" runat="server" CssClass="form-control"></asp:TextBox></div>
                </div>
                <div class="form-row">
                    <div class="form-group"><label>Phone:</label><asp:TextBox ID="txtPhone" runat="server" CssClass="form-control"></asp:TextBox></div>
                    <div class="form-group">
    <label>Payment Terms:</label>
    <asp:DropDownList ID="ddlTerms" runat="server" CssClass="form-control" onchange="toggleCustomTerms()">
        <asp:ListItem Value="Full Payment">Full Payment</asp:ListItem>
        <asp:ListItem Value="Weekly">Weekly Installments</asp:ListItem>
        <asp:ListItem Value="Monthly">Monthly Installments</asp:ListItem>
        <asp:ListItem Value="Custom">Custom Amount...</asp:ListItem>
    </asp:DropDownList>
    
    <!-- Hidden by default, shows up if 'Custom' is selected -->
    <asp:TextBox ID="txtCustomTerms" runat="server" CssClass="form-control mt-2" style="display:none;" placeholder="Enter specific ₱ amount per payment"></asp:TextBox>
</div>
                    <div class="form-group"><label>Status:</label>
                        <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-control">
                            <asp:ListItem Text="Active" Value="Active"></asp:ListItem>
                            <asp:ListItem Text="Inactive" Value="Inactive"></asp:ListItem>
                        </asp:DropDownList>
                    </div>
                </div>
                <div class="form-row">
                    <div class="form-group">
                        <asp:Button ID="btnAddClient" runat="server" Text="+ Add Client" CssClass="btn-primary" OnClick="btnAddClient_Click" />
                        <asp:LinkButton ID="btnCancelEdit" runat="server" Text="Cancel Edit" OnClick="btnCancelEdit_Click" Visible="false" style="margin-left: 15px; color: #6c757d; font-weight: bold; text-decoration: none;" />
                    </div>
                </div>
                <asp:GridView ID="gvClients" runat="server" AutoGenerateColumns="False" DataKeyNames="ClientID" OnRowCommand="gvClients_RowCommand" OnRowDeleting="gvClients_RowDeleting" CssClass="table-custom" GridLines="None">
                    <Columns>
                        <asp:BoundField DataField="CompanyName" HeaderText="Company Name" />
                        <asp:BoundField DataField="ContactPerson" HeaderText="Contact Person" />
                        <asp:BoundField DataField="Email" HeaderText="Email" />
                        <asp:TemplateField HeaderText="Status"><ItemTemplate><asp:Label ID="lblGridStatus" runat="server" Text='<%# Eval("Status") %>' CssClass='<%# Eval("Status").ToString() == "Active" ? "badge-active" : "badge-inactive" %>'></asp:Label></ItemTemplate></asp:TemplateField>
                        <asp:TemplateField HeaderText="Actions">
                            <ItemTemplate>
                                <asp:LinkButton ID="btnEdit" runat="server" CommandName="SelectForEdit" CommandArgument='<%# Container.DataItemIndex %>' Text="Edit" style="text-decoration:none; font-weight:bold; margin-right:8px; color:#0d6efd;" />
                                <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" Text="Delete" OnClientClick="return confirm('Are you sure?');" style="text-decoration:none; font-weight:bold; color:#dc3545;" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>

            <div class="card card-orders">
                <h2>3. Client Ordering System</h2>
                
                <div class="form-group" style="margin-bottom: 20px; max-width: 400px;">
                    <label style="color:#198754; font-size:1.1rem; font-weight: 700;">Select a Client to View/Add Orders:</label>
                    <asp:DropDownList ID="ddlSelectClient" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="ddlSelectClient_SelectedIndexChanged"></asp:DropDownList>
                </div>

                <div class="form-group" style="margin-bottom: 20px; max-width: 400px;">
                    <label style="color:#198754; font-size:1.1rem; font-weight: 700;">Select Target Quotation/Invoice:</label>
                    <asp:DropDownList ID="ddlSelectQuotation" runat="server" CssClass="form-control"></asp:DropDownList>
                </div>

                <asp:Label ID="lblOrderMsg" runat="server" Font-Bold="true" style="display:block; margin-bottom:15px;"></asp:Label>
                 
                <fieldset style="border: 1px solid #ced4da; padding: 20px; border-radius: 8px; margin-bottom: 20px; background-color: #fff;">
                    <legend style="font-size: 1rem; font-weight: bold; padding: 0 10px; color: #198754;">Item Line Details</legend>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Select Item:</label>
                            <asp:DropDownList ID="ddlCatalogItems" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlCatalogItems_SelectedIndexChanged" CssClass="form-control"></asp:DropDownList>
                        </div>
                        <div class="form-group">
                            <label>Select Color Variant:</label>
                            <asp:DropDownList ID="ddlColors" runat="server" CssClass="form-control"></asp:DropDownList>
                        </div>
                        <div class="form-group">
                            <label>Select Size Variant:</label>
                            <asp:DropDownList ID="ddlSizes" runat="server" CssClass="form-control"></asp:DropDownList>
                        </div>
                    </div>
                    <div class="form-row">
                        <div class="form-group"><label>Customize Text:</label><asp:TextBox ID="txtCustomize" runat="server" CssClass="form-control"></asp:TextBox></div>
                        <div class="form-group"><label>Design Details:</label><asp:TextBox ID="txtDesign" runat="server" CssClass="form-control"></asp:TextBox></div>
                        <div class="form-group"><label>Quantity (Pcs):</label><asp:TextBox ID="txtQty" runat="server" CssClass="form-control" TextMode="Number" Text="1" oninput="calculateLiveTotalCost();"></asp:TextBox></div>
                        <div class="form-group"><label>Base Unit Price ($):</label><asp:TextBox ID="txtCost" runat="server" CssClass="form-control" Enabled="false" oninput="calculateLiveTotalCost();"></asp:TextBox></div>
                    </div>
                    <div class="form-row">
                        <div class="form-group" style="max-width: 33%;"><label>Design Fee ($):</label><asp:TextBox ID="txtDesignFee" runat="server" CssClass="form-control" TextMode="Number" step="0.01" Text="0.00" oninput="calculateLiveTotalCost();"></asp:TextBox></div>
                    </div>
                     
                    <div class="p-3 mb-2 bg-light text-dark rounded d-flex justify-content-between align-items-center" style="border-left: 5px solid #ffc107;">
                        <span class="fw-bold" style="font-size: 0.9rem; color: #566573;">Live Order Item Value Estimation:</span>
                        <span id="lblLiveTotalDisplay" class="fw-bold text-success" style="font-size: 1.2rem;">₱0.00</span>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <asp:Button ID="btnAddItemToList" runat="server" Text="+ Add Item to List" CssClass="btn-warning" OnClick="btnAddItemToList_Click" />
                        </div>
                    </div>
                </fieldset>

                <h3 style="font-size: 1.1rem; font-weight: 700; color: var(--text-dark); margin-top: 20px;">Current Items in this Order Bundle</h3>
                <asp:GridView ID="gvTempItems" runat="server" AutoGenerateColumns="False" CssClass="table-custom" GridLines="None">
                    <Columns>
                        <asp:BoundField DataField="Item" HeaderText="Item Name" />
                        <asp:BoundField DataField="Color" HeaderText="Selected Color" />
                        <asp:BoundField DataField="Size" HeaderText="Selected Size" />
                        <asp:BoundField DataField="Customize" HeaderText="Design Fee ($)" />
                        <asp:BoundField DataField="Design" HeaderText="Design Description" />
                        <asp:BoundField DataField="Qty" HeaderText="Qty" />
                        <asp:BoundField DataField="Cost" HeaderText="Base Unit Price" DataFormatString="{0:C}" />
                    </Columns>
                </asp:GridView>
                <br />
                <div class="form-row">
                    <div class="form-group">
                        <asp:Button ID="btnSubmitOrder" runat="server" Text="✔ Save and Commit Complete Order Bundle" CssClass="btn-success" OnClick="btnSubmitOrder_Click" style="width: 100%; padding: 15px; font-size: 1.1rem;" Visible="false" />
                    </div>
                </div>
            </div>

            <div class="card card-history">
                <h2>4. Completed Order History Log</h2>
                <asp:GridView ID="gvOrderHistory" runat="server" AutoGenerateColumns="False" DataKeyNames="OrderID" OnRowDeleting="gvOrderHistory_RowDeleting" CssClass="table-custom" GridLines="None">
                    <Columns>
                        <asp:BoundField DataField="OrderID" HeaderText="Order Ref #" />
                        <asp:BoundField DataField="CompanyName" HeaderText="Client Name" ItemStyle-Font-Bold="true" />
                        <asp:BoundField DataField="Item" HeaderText="Item" />
                        <asp:BoundField DataField="Qty" HeaderText="Qty" />
                        <asp:BoundField DataField="Cost" HeaderText="Unit Price" DataFormatString="{0:C}" />
                        <asp:BoundField DataField="LineTotal" HeaderText="Line Total" DataFormatString="{0:C}" ItemStyle-ForeColor="Green" ItemStyle-Font-Bold="true" />
                        <asp:TemplateField HeaderText="Actions">
                            <ItemTemplate>
                                <asp:LinkButton ID="btnDeleteOrder" runat="server" CommandName="Delete" Text="Void" OnClientClick="return confirm('Void this client line item order completely? This will also wipe its related production records.');" style="text-decoration:none; font-weight:bold; color:#dc3545;" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>
         
        <script type="text/javascript">
            function calculateLiveTotalCost() {
                var txtQty = document.getElementById('<%= txtQty.ClientID %>');
                var txtCost = document.getElementById('<%= txtCost.ClientID %>');
                var txtFee = document.getElementById('<%= txtDesignFee.ClientID %>');
                var displayElement = document.getElementById('lblLiveTotalDisplay');

                if (txtQty && txtCost && displayElement) {
                    var qty = parseInt(txtQty.value) || 0;
                    var unitCost = parseFloat(txtCost.value) || 0.00;
                    var designFee = parseFloat(txtFee.value) || 0.00;

                    var overallTotal = (unitCost * qty) + designFee;
                    displayElement.innerText = "₱" + overallTotal.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                }
            }
            window.onload = function () { calculateLiveTotalCost(); };
        </script>
    </form>
   <script type="text/javascript">
       window.onpageshow = function (event) {
           if (event.persisted || (window.performance && window.performance.navigation.type === 2)) {
               window.location.replace("Login.aspx");
           }
       };
   </script>
    <script>
        function toggleCustomTerms() {
            var ddl = document.getElementById('<%= ddlTerms.ClientID %>');
        var txt = document.getElementById('<%= txtCustomTerms.ClientID %>');
            if (ddl.value === "Custom") {
                txt.style.display = "block";
                txt.focus();
            } else {
                txt.style.display = "none";
                txt.value = "";
            }
        }
</script>
</body>
</html>