# Starter

Projeto **Starter** – uma base completa para aplicações modernas em **.NET 8 (WebAPI)** com **SQL**, contendo **backend** e **frontend** no mesmo projeto.

---

## 🚀 Tecnologias
- .NET 8 / C#  
- Entity Framework Core  
- MySQL  
- JWT Authentication  
- FluentValidation  
- HTML, CSS e JavaScript (ES6) – servido via **wwwroot**  
- Bootstrap  

---

## 📂 Estrutura do Projeto
```
Starter.Api/
 ├── Config/                  # Ini Settings para a conexao com base de dados
 ├── Auth/                    # Autenticação e geração de tokens
 ├── Controllers/             # Controllers da API
 ├── Data/                    # DbContext e Migrations
 ├── DTOs/                    # Data Transfer Objects
 ├── Models/                  # Modelos de domínio
 ├── Middleware/              # JWT Revocation
 ├── Security/                # Policies, Claims e configuração de segurança
 ├── Services/                # Serviços de negócio / Autdit Logs
 ├── Validators/              # Validações (FluentValidation)
 ├── wwwroot/                 # Frontend (HTML, CSS, JS)
 ├── Program.cs               # Startup da aplicação
 └── appSettings.config       # Dados iniciais da aplicacao
```

---

## ⚙️ Configuração do Backend
1. Configure a connection string em `appsettings.json`:
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
2. Crie o banco de dados: 
   Utilize os scripts presentes no diretório:

   ```
   Starter.Api/
    ├── Config/                  # Ini Settings para a conexao com base de dados
   ```

3. Execute a aplicação:
   ```bash
   dotnet run --project Starter.Api
   ```

A API será exposta em:  
👉 `https://localhost:5073/api/...`

---

## 🖥️ Frontend
O frontend fica dentro de **`wwwroot/`** e é servido automaticamente junto com a aplicação.  
- Para acessar, basta abrir:  
👉 `https://localhost:5073/`

---

## 🔑 Funcionalidades
- Login e autenticação via **JWT**  
- Controle de permissões por usuário/role/policy  
- Cadastro e gerenciamento de usuários, roles e permissões  
- Logs de auditoria e histórico de ações  
- Frontend integrado servido via **wwwroot**  
- Estrutura pronta para paginação e listagens em grid  

---

## 📜 Licença
Este projeto é um produto licenciado pela **Moreno Capital**.
É permitido o uso para fins educacionais ou como base para novos sistemas, mantendo esta referência.
