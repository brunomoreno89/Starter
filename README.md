# Starter

Projeto **Starter** – uma base completa para aplicações modernas em **.NET 8 (WebAPI)** com **MySQL**, contendo **backend** e **frontend** no mesmo projeto.

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
 ├── Auth/            # Autenticação e geração de tokens
 ├── Controllers/     # Controllers da API
 ├── Data/            # DbContext e Migrations
 ├── DTOs/            # Data Transfer Objects
 ├── Models/          # Modelos de domínio
 ├── Security/        # Policies, Claims e configuração de segurança
 ├── Services/        # Serviços de negócio
 ├── Validators/      # Validações (FluentValidation)
 ├── wwwroot/         # Frontend (HTML, CSS, JS)
 └── Program.cs       # Startup da aplicação
```

---

## ⚙️ Configuração do Backend
1. Configure a connection string em `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "server=localhost;port=3306;database=Starter;user=root;password=suasenha"
   }
   ```
2. Crie o banco de dados:
   ```bash
   dotnet ef database update
   ```
3. Execute a aplicação:
   ```bash
   dotnet run --project Starter.Api
   ```

A API será exposta em:  
👉 `https://localhost:5001/api/...`

---

## 🖥️ Frontend
O frontend fica dentro de **`wwwroot/`** e é servido automaticamente junto com a aplicação.  
- Para acessar, basta abrir:  
👉 `https://localhost:5001/`

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
Este projeto é open-source e pode ser utilizado como base para novos sistemas.  
