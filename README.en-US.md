
---

# 🇺🇸 **README.en-US.md (Inglês)**

```md
# Starter

**Starter** is a complete foundation for modern **.NET 8 (WebAPI)** applications with **SQL**, containing both **backend** and **frontend** within the same project.

---

## 🚀 Technologies
- .NET 8 / C#
- Entity Framework Core
- MySQL
- JWT Authentication
- FluentValidation
- HTML, CSS and JavaScript (ES6) – served from **wwwroot**
- Bootstrap

---

## 📂 Project Structure

```
Starter.Api/
 ├── Config/                  # Ini Settings for db connection
 ├── Auth/                    # Autenticação and tokens
 ├── Controllers/             # API Controllers
 ├── Data/                    # DbContext and Migrations
 ├── DTOs/                    # Data Transfer Objects
 ├── Models/                  # Domain Models
 ├── Middleware/              # JWT Revocation
 ├── Security/                # Policies, Claims and Security configs
 ├── Services/                # Business Services / Audit Logs
 ├── Validators/              # Validations (FluentValidation)
 ├── wwwroot/                 # Frontend (HTML, CSS, JS)
 ├── Program.cs               # App Startup
 └── appSettings.config       # Initial Settings
```

---

## ⚙️ Backend Configuration
1. Configure a connection string on `appsettings.json`:
   ```json
   {
      "Jwt": {
         "Issuer": "Starter.Api",
         "Audience": "Starter.Api.Clients",
         "ExpiresMinutes": 60
      },
      "Serilog": {
         "Using": [ "Serilog.Sinks.Console" ],
         "MinimumLevel": "Information",
         "WriteTo": [ { "Name": "Console" } ]
      },
      "Database": {
         "Server": "localhost,1433",
         "Name": "STARTERAPI"
      },
      "AllowedHosts": "*"
   }

   ```
2. Database Creation:
   Utilize scripts present no path:

   ```
   Starter.Api/
    ├── Scripts/         
   ```

3. App execution:
   ```bash
   dotnet run --project Starter.Api
   ```

API will be exposef at:  
👉 `https://localhost:5073/api/...`

---

## 🖥️ Frontend

The frontend is located in **`wwwroot/`**  and is served automatically with the backend.

- Access at:  
👉 `https://localhost:5073/`

---

## 🔑 Funcionalidades
- **JWT** Authentication
- User/role/policy-based permissions
- User, role and permission management
- Audit logging
- Integrated frontend
- Pre-built structure for grids and pagination

---

## 📜 Licença
This project is a licensed product by **BMO**.
It may be used for educational purposes or as a base for other systems, as long as this notice is preserved.
