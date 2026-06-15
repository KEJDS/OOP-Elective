using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SQPMS
{
    public partial class AddOrder : System.Web.UI.Page
    {
        private string connString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
        protected global::System.Web.UI.WebControls.Label lblUserInitials;
        protected global::System.Web.UI.WebControls.Label lblUserRole;

        protected void Page_Load(object sender, EventArgs e)
        {
            EnforceSecurityAndCache();

            if (!IsPostBack)
            {
                InitializePageData();
            }

            if (Session["UserRole"] != null)
            {
                string role = Session["UserRole"].ToString().Trim();
                string displayRole = char.ToUpper(role[0]) + role.Substring(1);

                lblUserRole.Text = displayRole;
                lblUserInitials.Text = role.Length >= 2 ? role.Substring(0, 2).ToUpper() : role.ToUpper();
            }
        }

        private void EnforceSecurityAndCache()
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

            SetSidebarVisibility(role);
        }

        private void SetSidebarVisibility(string role)
        {
            phSalesMenu.Visible = (role == "admin" || role == "sales");
            phOperationsMenu.Visible = (role == "admin" || role == "operation");
            phOwnerMenu.Visible = (role == "admin");
        }

        private void InitializePageData()
        {
            BindClientGrid();
            BindClientDropdown();
            BindCatalogDropdown();
            BindProductsGrid();
            CreateTempTableStructure();
            BindOrderHistory();

            // Initialize the new dropdown as empty
            ddlSelectQuotation.Items.Clear();
            ddlSelectQuotation.Items.Insert(0, new ListItem("-- Select a Client First --", ""));
        }

        protected void btnGoBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("Dashboard.aspx");
        }

        // 1. PRODUCT CATALOG SYSTEMS HANDLERS
        private void BindProductsGrid()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "SELECT ProductID, ProductName, StandardColor, StandardSize, UnitPrice, MaterialCost FROM Products ORDER BY ProductID DESC";
                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable(); sda.Fill(dt);
                        gvProducts.DataSource = dt; gvProducts.DataBind();
                    }
                }
            }
            catch (Exception ex) { lblProdMsg.ForeColor = System.Drawing.Color.Red; lblProdMsg.Text = "Error loading product catalog: " + ex.Message; }
        }

        private void BindCatalogDropdown()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "SELECT ProductID, ProductName FROM Products ORDER BY ProductName ASC";
                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable(); sda.Fill(dt);
                        ddlCatalogItems.DataSource = dt;
                        ddlCatalogItems.DataTextField = "ProductName";
                        ddlCatalogItems.DataValueField = "ProductID";
                        ddlCatalogItems.DataBind();
                    }
                }
                ddlCatalogItems.Items.Insert(0, new ListItem("-- Choose Pre-defined Product Item --", ""));
            }
            catch (Exception ex) { lblOrderMsg.Text = "Error binding catalogue options dropdown: " + ex.Message; }
        }

        protected void ddlCatalogItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            ddlColors.Items.Clear();
            ddlSizes.Items.Clear();

            if (string.IsNullOrEmpty(ddlCatalogItems.SelectedValue))
            {
                txtCost.Text = ""; return;
            }
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "SELECT UnitPrice, StandardColor, StandardSize FROM Products WHERE ProductID = @ProductID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ProductID", Convert.ToInt32(ddlCatalogItems.SelectedValue));
                        con.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            if (sdr.Read())
                            {
                                txtCost.Text = Convert.ToDecimal(sdr["UnitPrice"]).ToString("0.00");

                                string rawColors = sdr["StandardColor"] != DBNull.Value ? sdr["StandardColor"].ToString() : "";
                                if (!string.IsNullOrWhiteSpace(rawColors))
                                {
                                    string[] colorArray = rawColors.Split(',');
                                    foreach (string color in colorArray)
                                    {
                                        if (!string.IsNullOrWhiteSpace(color))
                                            ddlColors.Items.Add(new ListItem(color.Trim(), color.Trim()));
                                    }
                                }

                                string rawSizes = sdr["StandardSize"] != DBNull.Value ? sdr["StandardSize"].ToString() : "";
                                if (!string.IsNullOrWhiteSpace(rawSizes))
                                {
                                    string[] sizeArray = rawSizes.Split(',');
                                    foreach (string size in sizeArray)
                                    {
                                        if (!string.IsNullOrWhiteSpace(size))
                                            ddlSizes.Items.Add(new ListItem(size.Trim(), size.Trim()));
                                    }
                                }
                            }
                        }
                    }
                }

                if (ddlColors.Items.Count == 0) ddlColors.Items.Add(new ListItem("Standard", "Standard"));
                if (ddlSizes.Items.Count == 0) ddlSizes.Items.Add(new ListItem("Standard", "Standard"));

                ScriptManager.RegisterStartupScript(this, GetType(), "recalc", "calculateLiveTotalCost();", true);
            }
            catch (Exception ex) { lblOrderMsg.Text = "Error pulling variant configurations: " + ex.Message; }
        }

        protected void btnSaveProduct_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProdName.Text)) return;
            decimal price = 0; decimal.TryParse(txtProdPrice.Text, out price);
            decimal matCost = 0; decimal.TryParse(txtMaterialCost.Text, out matCost);

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = !string.IsNullOrEmpty(hfEditingProductID.Value) ?
                        "UPDATE Products SET ProductName=@Name, StandardColor=@Color, StandardSize=@Size, UnitPrice=@Price, MaterialCost=@MatCost WHERE ProductID=@ProductID" :
                        "INSERT INTO Products (ProductName, StandardColor, StandardSize, UnitPrice, MaterialCost) VALUES (@Name, @Color, @Size, @Price, @MatCost)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        if (!string.IsNullOrEmpty(hfEditingProductID.Value))
                            cmd.Parameters.AddWithValue("@ProductID", hfEditingProductID.Value);

                        cmd.Parameters.AddWithValue("@Name", txtProdName.Text.Trim());
                        cmd.Parameters.AddWithValue("@Color", string.IsNullOrWhiteSpace(txtProdColor.Text) ? "Standard" : txtProdColor.Text.Trim());
                        cmd.Parameters.AddWithValue("@Size", string.IsNullOrWhiteSpace(txtProdSize.Text) ? "Standard" : txtProdSize.Text.Trim());
                        cmd.Parameters.AddWithValue("@Price", price);
                        cmd.Parameters.AddWithValue("@MatCost", matCost);
                        con.Open(); cmd.ExecuteNonQuery();
                    }
                }
                ResetProductForm(); BindProductsGrid(); BindCatalogDropdown();
                lblProdMsg.ForeColor = System.Drawing.Color.Green;
                lblProdMsg.Text = "Catalog details saved successfully.";
            }
            catch (Exception ex) { lblProdMsg.ForeColor = System.Drawing.Color.Red; lblProdMsg.Text = "Error recording catalog item: " + ex.Message; }
        }

        protected void gvProducts_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "SelectProductForEdit")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                GridViewRow row = gvProducts.Rows[index];

                hfEditingProductID.Value = gvProducts.DataKeys[index].Value.ToString();
                txtProdName.Text = Server.HtmlDecode(row.Cells[0].Text).Trim();
                txtProdColor.Text = Server.HtmlDecode(row.Cells[1].Text).Trim();
                txtProdSize.Text = Server.HtmlDecode(row.Cells[2].Text).Trim();

                string cleanPrice = Server.HtmlDecode(row.Cells[3].Text).Trim().Replace("$", "").Replace("₱", "").Replace(",", "");
                txtProdPrice.Text = cleanPrice;

                string cleanCost = Server.HtmlDecode(row.Cells[4].Text).Trim().Replace("$", "").Replace("₱", "").Replace(",", "");
                txtMaterialCost.Text = cleanCost;

                btnSaveProduct.Text = "Update Product Details";
                btnSaveProduct.CssClass = "btn-success";
                btnCancelProductEdit.Visible = true;
            }
        }

        protected void btnCancelProductEdit_Click(object sender, EventArgs e) { ResetProductForm(); }

        private void ResetProductForm()
        {
            hfEditingProductID.Value = ""; txtProdName.Text = ""; txtProdColor.Text = ""; txtProdSize.Text = ""; txtProdPrice.Text = ""; txtMaterialCost.Text = "";
            btnSaveProduct.Text = "+ Add to Catalog"; btnSaveProduct.CssClass = "btn-warning"; btnCancelProductEdit.Visible = false;
        }

        protected void gvProducts_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            try
            {
                int pId = Convert.ToInt32(gvProducts.DataKeys[e.RowIndex].Value);
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "DELETE FROM Products WHERE ProductID = @ID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ID", pId);
                        con.Open(); cmd.ExecuteNonQuery();
                    }
                }
                ResetProductForm(); BindProductsGrid(); BindCatalogDropdown();
            }
            catch (Exception ex) { lblProdMsg.ForeColor = System.Drawing.Color.Red; lblProdMsg.Text = "Error clearing item: " + ex.Message; }
        }

        // 2. CLIENT MANAGEMENT SECTION
        private void BindClientGrid()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "SELECT ClientID, CompanyName, ContactPerson, Email, Phone, PaymentTerms, Status FROM Clients ORDER BY ClientID DESC";
                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable(); sda.Fill(dt);
                        gvClients.DataSource = dt; gvClients.DataBind();
                    }
                }
            }
            catch (Exception ex) { lblClientMsg.Text = "Error loading clients: " + ex.Message; }
        }

        protected void btnAddClient_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCompany.Text) || string.IsNullOrWhiteSpace(txtContact.Text)) return;

            string finalTerms = ddlTerms.SelectedValue;
            if (finalTerms == "Custom" && !string.IsNullOrWhiteSpace(txtCustomTerms.Text))
            {
                finalTerms = txtCustomTerms.Text.Trim();
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = !string.IsNullOrEmpty(hfEditingClientID.Value) ?
                        "UPDATE Clients SET CompanyName=@Company, ContactPerson=@Contact, Email=@Email, Phone=@Phone, PaymentTerms=@Terms, Status=@Status WHERE ClientID=@ClientID" :
                        "INSERT INTO Clients (CompanyName, ContactPerson, Email, Phone, PaymentTerms, Status) VALUES (@Company, @Contact, @Email, @Phone, @Terms, @Status)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        if (!string.IsNullOrEmpty(hfEditingClientID.Value)) cmd.Parameters.AddWithValue("@ClientID", hfEditingClientID.Value);
                        cmd.Parameters.AddWithValue("@Company", txtCompany.Text.Trim());
                        cmd.Parameters.AddWithValue("@Contact", txtContact.Text.Trim());
                        cmd.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@Phone", txtPhone.Text.Trim());
                        cmd.Parameters.AddWithValue("@Terms", finalTerms);
                        cmd.Parameters.AddWithValue("@Status", ddlStatus.SelectedValue);
                        con.Open(); cmd.ExecuteNonQuery();
                    }
                }
                ResetClientForm(); BindClientGrid(); BindClientDropdown();
            }
            catch (Exception ex) { lblClientMsg.Text = "Error saving client details: " + ex.Message; }
        }

        protected void gvClients_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "SelectForEdit")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                GridViewRow row = gvClients.Rows[index];
                hfEditingClientID.Value = gvClients.DataKeys[index].Value.ToString();
                txtCompany.Text = Server.HtmlDecode(row.Cells[0].Text).Trim();
                txtContact.Text = Server.HtmlDecode(row.Cells[1].Text).Trim();
                txtEmail.Text = Server.HtmlDecode(row.Cells[2].Text).Trim();
                btnAddClient.Text = "Update Client Details"; btnCancelEdit.Visible = true;
            }
        }

        protected void btnCancelEdit_Click(object sender, EventArgs e) { ResetClientForm(); }

        private void ResetClientForm()
        {
            hfEditingClientID.Value = ""; txtCompany.Text = ""; txtContact.Text = ""; txtEmail.Text = ""; txtPhone.Text = "";
            ddlTerms.SelectedIndex = 0; txtCustomTerms.Text = ""; txtCustomTerms.Style["display"] = "none";
            ddlStatus.SelectedIndex = 0; btnAddClient.Text = "+ Add Client"; btnCancelEdit.Visible = false;
        }

        protected void gvClients_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            try
            {
                int clientId = Convert.ToInt32(gvClients.DataKeys[e.RowIndex].Value);
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "DELETE FROM Clients WHERE ClientID = @ClientID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ClientID", clientId);
                        con.Open(); cmd.ExecuteNonQuery();
                    }
                }
                ResetClientForm(); BindClientGrid(); BindClientDropdown();
            }
            catch (Exception ex) { lblClientMsg.Text = "Dependency error: " + ex.Message; }
        }

        // 3. ORDER TRANSACTION SYSTEM HANDLERS
        private void BindClientDropdown()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "SELECT ClientID, CompanyName FROM Clients WHERE Status='Active' ORDER BY CompanyName ASC";
                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable(); sda.Fill(dt);
                        ddlSelectClient.DataSource = dt; ddlSelectClient.DataTextField = "CompanyName"; ddlSelectClient.DataValueField = "ClientID"; ddlSelectClient.DataBind();
                    }
                }
                ddlSelectClient.Items.Insert(0, new ListItem("-- Select an Active Client --", ""));
            }
            catch (Exception ex) { lblOrderMsg.Text = "Error loading client dropdown: " + ex.Message; }
        }

        private void BindOrderHistory()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    // Group by QuotationID to show the whole bundle as one row
                    // We temporarily alias QuotationID as 'OrderID' so your GridView doesn't break!
                    string query = @"
                SELECT 
                    o.QuotationID AS OrderID, 
                    c.CompanyName, 
                    STUFF((SELECT ', ' + o2.Item FROM Orders o2 WHERE o2.QuotationID = o.QuotationID FOR XML PATH('')), 1, 2, '') AS Item, 
                    SUM(o.Qty) AS Qty, 
                    SUM(CAST(o.Cost AS DECIMAL(18,2))) AS Cost, 
                    SUM((CAST(o.Cost AS DECIMAL(18,2)) * CAST(o.Qty AS DECIMAL(18,2))) + ISNULL(TRY_CAST(o.Customize AS DECIMAL(18,2)), 0)) AS LineTotal 
                FROM Orders o 
                INNER JOIN Clients c ON o.ClientID = c.ClientID 
                GROUP BY o.QuotationID, c.CompanyName
                ORDER BY o.QuotationID DESC";

                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        gvOrderHistory.DataSource = dt;
                        gvOrderHistory.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                lblOrderMsg.ForeColor = System.Drawing.Color.Red;
                lblOrderMsg.Text = "Error binding history log: " + ex.Message;
            }
        }

        protected void gvOrderHistory_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            try
            {
                // Because we aliased it in the query above, this actually holds the QuotationID now
                int quotationId = Convert.ToInt32(gvOrderHistory.DataKeys[e.RowIndex].Values["OrderID"]);

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    // Delete all production lines, payments, and orders tied to this Quotation bundle
                    string query = @"
                DELETE FROM Production WHERE OrderID IN (SELECT OrderID FROM Orders WHERE QuotationID = @QuotationID);
                DELETE FROM Payments WHERE OrderID IN (SELECT OrderID FROM Orders WHERE QuotationID = @QuotationID);
                DELETE FROM Orders WHERE QuotationID = @QuotationID;";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@QuotationID", quotationId);
                        cmd.ExecuteNonQuery();
                    }
                }
                BindOrderHistory();
                lblOrderMsg.ForeColor = System.Drawing.Color.Green;
                lblOrderMsg.Text = "Order bundle voided and removed cleanly.";
            }
            catch (Exception ex)
            {
                lblOrderMsg.ForeColor = System.Drawing.Color.Red;
                lblOrderMsg.Text = "Error executing order line clear processing: " + ex.Message;
            }
        }

        private void CreateTempTableStructure()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Item", typeof(string));
            dt.Columns.Add("Color", typeof(string));
            dt.Columns.Add("Size", typeof(string));
            dt.Columns.Add("Customize", typeof(string));
            dt.Columns.Add("Design", typeof(string));
            dt.Columns.Add("Qty", typeof(int));
            dt.Columns.Add("Cost", typeof(decimal));
            Session["OrderBasketTable"] = dt;
        }

        
        protected void ddlSelectClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblOrderMsg.Text = "";
            CreateTempTableStructure();
            gvTempItems.DataSource = null;
            gvTempItems.DataBind();
            btnSubmitOrder.Visible = false;

            ddlSelectQuotation.Items.Clear();

            if (string.IsNullOrEmpty(ddlSelectClient.SelectedValue))
            {
                ddlSelectQuotation.Items.Insert(0, new ListItem("-- Select a Client First --", ""));
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    string query = "SELECT QuotationID, ProjectName FROM Quotations WHERE ClientID = @ClientID ORDER BY QuotationID DESC";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ClientID", Convert.ToInt32(ddlSelectClient.SelectedValue));
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            sda.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                ddlSelectQuotation.DataSource = dt;
                                ddlSelectQuotation.DataTextField = "ProjectName";
                                ddlSelectQuotation.DataValueField = "QuotationID";
                                ddlSelectQuotation.DataBind();
                                ddlSelectQuotation.Items.Insert(0, new ListItem("-- Select Quotation/Invoice --", ""));
                            }
                            else
                            {
                                ddlSelectQuotation.Items.Insert(0, new ListItem("-- No Active Quotations Found --", ""));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblOrderMsg.Text = "Error loading client quotations: " + ex.Message;
            }
        }

        protected void btnAddItemToList_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddlSelectClient.SelectedValue) || string.IsNullOrEmpty(ddlCatalogItems.SelectedValue))
            {
                lblOrderMsg.ForeColor = System.Drawing.Color.Red; lblOrderMsg.Text = "Select an active client and catalog item."; return;
            }

      
            if (string.IsNullOrEmpty(ddlSelectQuotation.SelectedValue))
            {
                lblOrderMsg.ForeColor = System.Drawing.Color.Red; lblOrderMsg.Text = "You must select a Quotation/Invoice to attach this item to."; return;
            }

            decimal cost = 0; decimal.TryParse(txtCost.Text, out cost);
            int qty = 1; int.TryParse(txtQty.Text, out qty);
            decimal designFee = 0; decimal.TryParse(txtDesignFee.Text, out designFee);

            DataTable dt = (DataTable)Session["OrderBasketTable"] ?? new DataTable();
            if (dt.Columns.Count == 0) CreateTempTableStructure();

            dt = (DataTable)Session["OrderBasketTable"];
            DataRow dr = dt.NewRow();

            dr["Item"] = ddlCatalogItems.SelectedItem.Text.Trim();
            dr["Color"] = ddlColors.SelectedItem != null ? ddlColors.SelectedItem.Text : "Standard";
            dr["Size"] = ddlSizes.SelectedItem != null ? ddlSizes.SelectedItem.Text : "Standard";
            dr["Customize"] = designFee.ToString("0.00");
            dr["Design"] = txtDesign.Text.Trim();
            dr["Qty"] = qty;
            dr["Cost"] = cost;
            dt.Rows.Add(dr);

            Session["OrderBasketTable"] = dt; gvTempItems.DataSource = dt; gvTempItems.DataBind();

            txtDesign.Text = ""; txtCost.Text = ""; txtQty.Text = "1"; txtDesignFee.Text = "0.00"; ddlCatalogItems.SelectedIndex = 0; ddlColors.Items.Clear(); ddlSizes.Items.Clear();
            btnSubmitOrder.Visible = true;

            ScriptManager.RegisterStartupScript(this, GetType(), "resetPrice", "document.getElementById('lblLiveTotalDisplay').innerText = '₱0.00';", true);
        }

        protected void btnSubmitOrder_Click(object sender, EventArgs e)
        {
            DataTable dt = (DataTable)Session["OrderBasketTable"];
            if (dt == null || dt.Rows.Count == 0) return;

            int targetedClientID = Convert.ToInt32(ddlSelectClient.SelectedValue);
            int targetedQuotationID = Convert.ToInt32(ddlSelectQuotation.SelectedValue);

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    
                    string detailQuery = @"INSERT INTO Orders (ClientID, QuotationID, Status, Item, Color, Size, Customize, Design, Cost, Qty) 
                                           VALUES (@ClientID, @QuotationID, 'Pending', @Item, @Color, @Size, @Customize, @Design, @Cost, @Qty)";

                    foreach (DataRow row in dt.Rows)
                    {
                        using (SqlCommand cmdDetail = new SqlCommand(detailQuery, con))
                        {
                            cmdDetail.Parameters.AddWithValue("@ClientID", targetedClientID);
                            cmdDetail.Parameters.AddWithValue("@QuotationID", targetedQuotationID);
                            cmdDetail.Parameters.AddWithValue("@Item", row["Item"]);
                            cmdDetail.Parameters.AddWithValue("@Color", row["Color"]);
                            cmdDetail.Parameters.AddWithValue("@Size", row["Size"]);
                            cmdDetail.Parameters.AddWithValue("@Customize", row["Customize"]);
                            cmdDetail.Parameters.AddWithValue("@Design", row["Design"]);
                            cmdDetail.Parameters.AddWithValue("@Cost", row["Cost"]);
                            cmdDetail.Parameters.AddWithValue("@Qty", row["Qty"]);
                            cmdDetail.ExecuteNonQuery();
                        }
                    }
                }
                CreateTempTableStructure(); gvTempItems.DataSource = null; gvTempItems.DataBind();
                btnSubmitOrder.Visible = false;
                ddlSelectClient.SelectedIndex = 0;
                ddlSelectQuotation.Items.Clear();
                BindOrderHistory();
                lblOrderMsg.ForeColor = System.Drawing.Color.Green; lblOrderMsg.Text = "Order bundle successfully attached to the Quotation!";
            }
            catch (Exception ex) { lblOrderMsg.ForeColor = System.Drawing.Color.Red; lblOrderMsg.Text = "Transaction processing error: " + ex.Message; }
        }
    }
}