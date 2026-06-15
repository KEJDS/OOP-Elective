<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="SQPMS.Login" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Login - Basic Bee Prints</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700&display=swap" rel="stylesheet">
    <style>
        body { margin: 0; padding: 0; font-family: 'Inter', sans-serif; background-color: #16476d; height: 100vh; display: flex; flex-direction: column; justify-content: center; align-items: center; overflow: hidden; }

   
.bg-container { 
    position: fixed; top: 0; left: 0; 
    width: 100vw; height: 100vh; 
    z-index: 1; pointer-events: none; overflow: hidden; 
}

.spot { 
    position: absolute; 
    border-radius: 50%; 
    background: rgba(255, 255, 255, 0.05); 
}


.spot-1 { width: 550px; height: 550px; top: -10%; left: -10%; }
.spot-2 { width: 450px; height: 450px; bottom: -5%; right: -5%; }
.spot-3 { width: 350px; height: 350px; top: 15%; left: 65%; background: rgba(255, 255, 255, 0.03); }
.spot-4 { width: 250px; height: 250px; bottom: 20%; left: 15%; }
.spot-5 { width: 400px; height: 400px; top: 50%; left: -5%; background: rgba(255, 255, 255, 0.02); }
.spot-6 { width: 200px; height: 200px; top: 60%; left: 80%; }
.spot-7 { width: 300px; height: 300px; bottom: 5%; left: 45%; background: rgba(255, 255, 255, 0.04); }

        
        .brand-section { text-align: center; color: white; margin-bottom: 30px; }
        .brand-logo { background: #ffffff; display: inline-block; padding: 15px; border-radius: 16px; margin-bottom: 15px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); font-size: 2rem; }
        .brand-title { font-size: 2rem; font-weight: 700; margin-bottom: 5px; }
        .brand-sub { opacity: 0.8; font-size: 0.9rem; }

      
        .login-card { background: #ffffff; padding: 40px; width: 400px; border-radius: 20px; box-shadow: 0 10px 25px rgba(0,0,0,0.2); }
        .login-card h2 { color: #1e293b; margin: 0 0 5px 0; font-size: 1.5rem; }
        .login-card p { color: #64748b; font-size: 0.9rem; margin-bottom: 25px; }

        label { display: block; font-size: 0.7rem; font-weight: 700; color: #475569; margin-bottom: 8px; text-transform: uppercase; }
        .input-field { width: 100%; padding: 14px; margin-bottom: 20px; border: 1px solid #e5e7eb; border-radius: 8px; background-color: #fffaf0; box-sizing: border-box; outline: none; font-size: 0.95rem; }
        .input-field:focus { border-color: #16476d; box-shadow: 0 0 0 2px rgba(22, 71, 109, 0.2); }

        .forgot-link { display: block; text-align: right; font-size: 0.8rem; color: #64748b; margin-top: -15px; margin-bottom: 20px; text-decoration: none; }
        .login-btn { width: 100%; padding: 14px; background: #16476d; color: white; border: none; border-radius: 8px; font-weight: 600; cursor: pointer; font-size: 1rem; transition: background 0.3s; }
        .login-btn:hover { background: #0f324d; }
        .error-msg { color: #dc2626; font-size: 0.8rem; margin-bottom: 15px; display: block; font-weight: 600; text-align: center; }
    </style>
</head>
<body>

     <div class="bg-container">

     <div class="spot spot-1"></div>

     <div class="spot spot-2"></div>

     <div class="spot spot-3"></div>

     <div class="spot spot-4"></div>

     <div class="spot spot-5"></div>

 </div>
   
    <form id="form1" runat="server">
        <div class="brand-section">
            <div class="brand-logo">🌸</div>
            <div class="brand-title">Basic Bee Prints</div>
            <div class="brand-sub">Sales & Production Management System for smarter business decisions.</div>
        </div>

        <div class="login-card">
            <h2>Welcome back 👋</h2>
            <p>Sign in to access your dashboard</p>
            
            <asp:Label ID="lblMsg" runat="server" CssClass="error-msg"></asp:Label>
            
            <label>USERNAME</label>
            <asp:TextBox ID="txtUser" runat="server" CssClass="input-field" Placeholder="owner@basicbee.ph"></asp:TextBox>
            
            <label>PASSWORD</label>
            <asp:TextBox ID="txtPass" runat="server" CssClass="input-field" TextMode="Password" Placeholder="•••••••"></asp:TextBox>
            
            <a href="#" class="forgot-link">Forgot password?</a>
            
            <asp:Button ID="btnLogin" runat="server" Text="Sign In →" CssClass="login-btn" OnClick="btnLogin_Click" />
        </div>
    </form>
</body>
</html>