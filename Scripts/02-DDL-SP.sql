use STARTERAPI
GO

-- select * from sys.objects where type = 'P' 

CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ITEMS_LIST
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        i.Id,
        i.Name,
        i.Description,
        i.Active,
        i.CreatedAt,
        i.UpdatedAt,
        i.CreatedByUserId,
        cu.Name AS CreatedByName,
        i.UpdatedByUserId,
        uu.Name AS UpdatedByName
    FROM Items i
    LEFT JOIN Users cu ON cu.Id = i.CreatedByUserId
    LEFT JOIN Users uu ON uu.Id = i.UpdatedByUserId
    ORDER BY i.Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ITEMS_GETBYID
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        i.Id,
        i.Name,
        i.Description,
        i.Active,
        i.CreatedAt,
        i.UpdatedAt,
        i.CreatedByUserId,
        i.UpdatedByUserId
    FROM Items i
    WHERE i.Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ITEMS_CREATE
    @Name NVARCHAR(120),
    @Description NVARCHAR(MAX) = NULL,
    @CreatedByUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Items (Name, Description, Active, CreatedAt, CreatedByUserId)
    VALUES (@Name, @Description, 'Yes', GETDATE(), @CreatedByUserId);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ITEMS_UPDATE
    @Id INT,
    @Name NVARCHAR(120),
    @Description NVARCHAR(MAX) = NULL,
    @Active VARCHAR(3),
    @UpdatedByUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Items
    SET 
        Name            = @Name,
        Description     = @Description,
        Active          = @Active,
        UpdatedAt       = GETDATE(),
        UpdatedByUserId = @UpdatedByUserId
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO


CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ITEMS_SOFTDELETE
    @Id INT,
    @UpdatedByUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Items
    SET 
        Active          = 'No',
        UpdatedAt       = GETDATE(),
        UpdatedByUserId = @UpdatedByUserId
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO


CREATE OR ALTER PROCEDURE dbo.SP_STARTER_LOGS_LIST
    @UserTerm   NVARCHAR(100) = NULL,
    @StartLocal DATETIME,
    @EndLocal   DATETIME,
    @MaxRows    INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@MaxRows)
        l.Id,
        l.ExecDate,
        l.UserId,
        u.Username,
        u.Name,
        r.Id  AS RoleId,
        r.Name AS RoleName,
        p.Id  AS PermissionId,
        p.Name AS PermissionName,
        l.Description
    FROM Logs l
    LEFT JOIN Users u       ON u.Id = l.UserId
    LEFT JOIN Roles r       ON r.Id = l.RoleId
    LEFT JOIN Permissions p ON p.Id = l.PermissionId
    WHERE l.ExecDate >= @StartLocal
      AND l.ExecDate <  @EndLocal
      AND (
            @UserTerm IS NULL
         OR @UserTerm = ''
         OR (u.Username IS NOT NULL AND u.Username LIKE '%' + @UserTerm + '%')
         OR (u.Name     IS NOT NULL AND u.Name     LIKE '%' + @UserTerm + '%')
      )
    ORDER BY l.ExecDate DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_STARTER_AUDIT_LOG_INSERT
    @UserId INT,
    @RoleId INT = NULL,
    @PermissionId INT = NULL,
    @Description VARCHAR(250)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Logs (UserId, RoleId, PermissionId, ExecDate, Description)
    VALUES (@UserId, @RoleId, @PermissionId, GETDATE(), @Description);
END
GO

/* ============================================================
   USERS – STORED PROCEDURES STARTER
   Tabela: dbo.Users
   Colunas (sp_help Users):
     Id           INT (identity)
     Username     VARCHAR(255) NOT NULL
     Name         VARCHAR(50)  NOT NULL
     Email        VARCHAR(255) NOT NULL
     PasswordHash VARCHAR(255) NOT NULL
     Role         VARCHAR(255) NOT NULL
     CreationDt   SMALLDATETIME NOT NULL
     UpdatedDt    SMALLDATETIME NULL
     CreatedBy    INT NULL
     UpdatedBy    INT NULL
     Active       VARCHAR(3) NULL  (Yes/No)
   ============================================================ */

---------------------------------------------------------------
-- 1) LISTAR TODOS OS USUÁRIOS
--    Usado por: UserService.ListAsync()
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_USERS_LIST
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.Id,
        u.Username,
        u.Name,
        u.Email,
        u.Role,
        u.Active,
        u.CreationDt,
        u.CreatedBy,
        u.UpdatedDt,
        u.UpdatedBy,
        cb.Name AS CreatedByName,
        ub.Name AS UpdatedByName
    FROM Users u
    LEFT JOIN Users cb ON cb.Id = u.CreatedBy
    LEFT JOIN Users ub ON ub.Id = u.UpdatedBy
    ORDER BY u.Id;
END
GO


---------------------------------------------------------------
-- 2) OBTER USUÁRIO POR ID
--    Usado por: UserService.GetByIdAsync()
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_USERS_GETBYID
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.Id,
        u.Username,
        u.Name,
        u.Email,
        u.Role,
        u.Active,
        u.CreationDt,
        u.CreatedBy,
        u.UpdatedDt,
        u.UpdatedBy,
        cb.Name AS CreatedByName,
        ub.Name AS UpdatedByName
    FROM Users u
    LEFT JOIN Users cb ON cb.Id = u.CreatedBy
    LEFT JOIN Users ub ON ub.Id = u.UpdatedBy
    WHERE u.Id = @Id;
END
GO


---------------------------------------------------------------
-- 3) CRIAR USUÁRIO
--    Usado por: UserService.CreateAsync()
--    Role: default 'User' (ajuste se quiser Admin etc.)
--    Retorna: SCOPE_IDENTITY() AS NewId
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_USERS_CREATE
    @Username     VARCHAR(255),
    @Name         VARCHAR(50),
    @Email        VARCHAR(255),
    @PasswordHash VARCHAR(255),
    @Active       VARCHAR(3),
    @CreationDt   DATETIME,
    @CreatedBy    INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Users
    (
        Username,
        Name,
        Email,
        PasswordHash,
        Role,
        Active,
        CreationDt,
        CreatedBy
    )
    VALUES
    (
        @Username,
        @Name,
        @Email,
        @PasswordHash,
        'User',      -- default de Role
        @Active,
        @CreationDt,
        @CreatedBy
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId;
END
GO


---------------------------------------------------------------
-- 4) ATUALIZAR USUÁRIO
--    Usado por: UserService.UpdateAsync()
--    @PasswordHash pode vir NULL → não altera a senha
--    Role NÃO é alterado aqui (mantém valor atual)
--    Retorna: @@ROWCOUNT AS RowsAffected
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_USERS_UPDATE
    @Id           INT,
    @Username     VARCHAR(255),
    @Name         VARCHAR(50),
    @Email        VARCHAR(255),
    @Active       VARCHAR(3),
    @UpdatedBy    INT = NULL,
    @UpdatedDt    DATETIME,
    @PasswordHash VARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Atualiza campos básicos (exceto senha/role)
    UPDATE Users
    SET
        Username  = @Username,
        Name      = @Name,
        Email     = @Email,
        Active    = @Active,
        UpdatedBy = @UpdatedBy,
        UpdatedDt = @UpdatedDt
    WHERE Id = @Id;

    -- Se foi informada nova senha, atualiza em separado
    IF (@PasswordHash IS NOT NULL)
    BEGIN
        UPDATE Users
        SET PasswordHash = @PasswordHash
        WHERE Id = @Id;
    END

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO


---------------------------------------------------------------
-- 5) SOFT DELETE (Active = 'No')
--    Usado por: UserService.SoftDeleteAsync()
--    Retorna: @@ROWCOUNT AS RowsAffected
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_USERS_SOFTDELETE
    @Id        INT,
    @UpdatedBy INT = NULL,
    @UpdatedDt DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Users
    SET
        Active    = 'No',
        UpdatedBy = @UpdatedBy,
        UpdatedDt = @UpdatedDt
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO


---------------------------------------------------------------
-- 6) EXISTE USERNAME?
--    Usado por: UserService.UsernameExistsAsync()
--    @IgnoreId → desconsidera o próprio Id em updates
--    Retorna: COUNT(*) AS Cnt
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_USERS_EXISTS_USERNAME
    @Username VARCHAR(255),
    @IgnoreId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS Cnt
    FROM Users
    WHERE Username = @Username
      AND (@IgnoreId IS NULL OR Id <> @IgnoreId);
END
GO


---------------------------------------------------------------
-- 7) EXISTE EMAIL?
--    Usado por: UserService.EmailExistsAsync()
--    @IgnoreId → desconsidera o próprio Id em updates
--    Retorna: COUNT(*) AS Cnt
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_USERS_EXISTS_EMAIL
    @Email    VARCHAR(255),
    @IgnoreId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS Cnt
    FROM Users
    WHERE Email = @Email
      AND (@IgnoreId IS NULL OR Id <> @IgnoreId);
END
GO


/* ============================================================
      STORED PROCEDURES – STARTER – PERMISSIONS
      Tabela: dbo.Permissions
      Colunas:
        Id          INT IDENTITY(1,1) NOT NULL
        Name        VARCHAR(150) NOT NULL
        Description VARCHAR(255) NULL
        CreationDt  SMALLDATETIME NOT NULL
        UpdateDt    SMALLDATETIME NULL
        CreatedBy   INT NULL
        UpdatedBy   INT NULL
        Active      VARCHAR(3) NULL  -- Yes / No
   ============================================================ */

---------------------------------------------------------------
-- 1) LISTAR TODAS AS PERMISSÕES
--    Retorna nomes de CreatedBy/UpdatedBy (join Users)
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_PERMISSIONS_LIST
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.Name,
        p.Description,
        p.Active,
        p.CreationDt,
        p.CreatedBy,
        p.UpdateDt,
        p.UpdatedBy,
        cb.Name AS CreatedByName,
        ub.Name AS UpdatedByName
    FROM Permissions p
    LEFT JOIN Users cb ON cb.Id = p.CreatedBy
    LEFT JOIN Users ub ON ub.Id = p.UpdatedBy
    ORDER BY p.Id;
END
GO


---------------------------------------------------------------
-- 2) BUSCAR UMA PERMISSÃO POR ID
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_PERMISSIONS_GETBYID
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.Name,
        p.Description,
        p.Active,
        p.CreationDt,
        p.CreatedBy,
        p.UpdateDt,
        p.UpdatedBy,
        cb.Name AS CreatedByName,
        ub.Name AS UpdatedByName
    FROM Permissions p
    LEFT JOIN Users cb ON cb.Id = p.CreatedBy
    LEFT JOIN Users ub ON ub.Id = p.UpdatedBy
    WHERE p.Id = @Id;
END
GO


---------------------------------------------------------------
-- 3) CRIAR PERMISSÃO
--    Retorna SCOPE_IDENTITY() como NewId
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_PERMISSIONS_CREATE
    @Name        VARCHAR(150),
    @Description VARCHAR(255) = NULL,
    @Active      VARCHAR(3),
    @CreationDt  SMALLDATETIME,
    @CreatedBy   INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Permissions
    (
        Name,
        Description,
        CreationDt,
        CreatedBy,
        UpdateDt,
        UpdatedBy,
        Active
    )
    VALUES
    (
        @Name,
        @Description,
        @CreationDt,
        @CreatedBy,
        NULL,
        NULL,
        @Active
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId;
END
GO


---------------------------------------------------------------
-- 4) ATUALIZAR PERMISSÃO
--    Retorna @@ROWCOUNT como RowsAffected
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_PERMISSIONS_UPDATE
    @Id          INT,
    @Name        VARCHAR(150),
    @Description VARCHAR(255) = NULL,
    @Active      VARCHAR(3),
    @UpdatedBy   INT = NULL,
    @UpdateDt    SMALLDATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Permissions
    SET
        Name        = @Name,
        Description = @Description,
        Active      = @Active,
        UpdatedBy   = @UpdatedBy,
        UpdateDt    = @UpdateDt
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO


---------------------------------------------------------------
-- 5) SOFT DELETE (Active = 'No')
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_PERMISSIONS_SOFTDELETE
    @Id        INT,
    @UpdatedBy INT = NULL,
    @UpdateDt  SMALLDATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Permissions
    SET
        Active    = 'No',
        UpdatedBy = @UpdatedBy,
        UpdateDt  = @UpdateDt
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO


---------------------------------------------------------------
-- 6) VERIFICAR SE NOME JÁ EXISTE
--    Usado no Create/Update para validar duplicidade
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_PERMISSIONS_EXISTS_NAME
    @Name     VARCHAR(150),
    @IgnoreId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS Cnt
    FROM Permissions
    WHERE Name = @Name
      AND (@IgnoreId IS NULL OR Id <> @IgnoreId);
END
GO

/* ============================================================
      STORED PROCEDURES – STARTER – ROLES
      Tabela: dbo.Roles
      Colunas:
        Id          INT IDENTITY(1,1) NOT NULL
        Name        VARCHAR(150) NOT NULL
        Description VARCHAR(255) NULL
        CreationDt  SMALLDATETIME NOT NULL
        UpdateDt    SMALLDATETIME NULL
        CreatedBy   INT NULL
        UpdatedBy   INT NULL
        Active      VARCHAR(3) NULL  -- Yes / No
   ============================================================ */

---------------------------------------------------------------
-- 1) LISTAR TODAS AS ROLES
--    Retorna nomes de CreatedBy/UpdatedBy (join Users)
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ROLES_LIST
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id,
        r.Name,
        r.Description,
        r.Active,
        r.CreationDt,
        r.CreatedBy,
        r.UpdateDt,
        r.UpdatedBy,
        cb.Name AS CreatedByName,
        ub.Name AS UpdatedByName
    FROM Roles r
    LEFT JOIN Users cb ON cb.Id = r.CreatedBy
    LEFT JOIN Users ub ON ub.Id = r.UpdatedBy
    ORDER BY r.Id;
END
GO

---------------------------------------------------------------
-- 2) BUSCAR UMA ROLE POR ID
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ROLES_GETBYID
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id,
        r.Name,
        r.Description,
        r.Active,
        r.CreationDt,
        r.CreatedBy,
        r.UpdateDt,
        r.UpdatedBy,
        cb.Name AS CreatedByName,
        ub.Name AS UpdatedByName
    FROM Roles r
    LEFT JOIN Users cb ON cb.Id = r.CreatedBy
    LEFT JOIN Users ub ON ub.Id = r.UpdatedBy
    WHERE r.Id = @Id;
END
GO

---------------------------------------------------------------
-- 3) CRIAR ROLE
--    Retorna SCOPE_IDENTITY() como NewId
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ROLES_CREATE
    @Name        VARCHAR(150),
    @Description VARCHAR(255) = NULL,
    @Active      VARCHAR(3),
    @CreationDt  SMALLDATETIME,
    @CreatedBy   INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Roles
    (
        Name,
        Description,
        CreationDt,
        CreatedBy,
        UpdateDt,
        UpdatedBy,
        Active
    )
    VALUES
    (
        @Name,
        @Description,
        @CreationDt,
        @CreatedBy,
        NULL,
        NULL,
        @Active
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId;
END
GO

---------------------------------------------------------------
-- 4) ATUALIZAR ROLE
--    Retorna @@ROWCOUNT como RowsAffected
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ROLES_UPDATE
    @Id          INT,
    @Name        VARCHAR(150),
    @Description VARCHAR(255) = NULL,
    @Active      VARCHAR(3),
    @UpdatedBy   INT = NULL,
    @UpdateDt    SMALLDATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Roles
    SET
        Name        = @Name,
        Description = @Description,
        Active      = @Active,
        UpdatedBy   = @UpdatedBy,
        UpdateDt    = @UpdateDt
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

---------------------------------------------------------------
-- 5) SOFT DELETE (Active = 'No')
--    Retorna @@ROWCOUNT como RowsAffected
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ROLES_SOFTDELETE
    @Id        INT,
    @UpdatedBy INT = NULL,
    @UpdateDt  SMALLDATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Roles
    SET
        Active    = 'No',
        UpdatedBy = @UpdatedBy,
        UpdateDt  = @UpdateDt
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

---------------------------------------------------------------
-- 6) VERIFICAR SE NOME JÁ EXISTE
--    Usado no Create/Update para validar duplicidade
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ROLES_EXISTS_NAME
    @Name     VARCHAR(150),
    @IgnoreId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS Cnt
    FROM Roles
    WHERE Name = @Name
      AND (@IgnoreId IS NULL OR Id <> @IgnoreId);
END
GO

/* ============================================================
      STORED PROCEDURES – STARTER – ROLEPERMISSIONS
      Tabela: dbo.RolePermissions
      Colunas:
        RoleId       INT NOT NULL
        PermissionId INT NOT NULL
      (recomendado: PRIMARY KEY (RoleId, PermissionId))
   ============================================================ */

---------------------------------------------------------------
-- 1) LISTAR PERMISSÕES POR ROLE
--    Usado em GET /api/rolepermissions/{roleId}
--    Retorna dados da tabela Permissions (Id, Name, Description)
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ROLEPERMISSIONS_GETBYROLE
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.Name,
        p.Description
    FROM RolePermissions rp
    INNER JOIN Permissions p
        ON p.Id = rp.PermissionId
    WHERE rp.RoleId = @RoleId
    ORDER BY p.Name;
END
GO

---------------------------------------------------------------
-- 2) LIMPAR TODAS AS PERMISSÕES DE UM ROLE
--    Estratégia "replace-all":
--    1) Chama esta SP para remover tudo do RoleId
--    2) Depois insere novamente a lista de PermissionIds válidos
--    Retorna @@ROWCOUNT como DeletedCount (qtde antiga)
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ROLEPERMISSIONS_CLEAR
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM RolePermissions
    WHERE RoleId = @RoleId;

    SELECT @@ROWCOUNT AS DeletedCount;
END
GO

---------------------------------------------------------------
-- 3) INSERIR UMA PERMISSÃO PARA UM ROLE
--    Usado em loop na camada de serviço
--    (Pressupõe que RoleId e PermissionId já foram validados)
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_ROLEPERMISSIONS_INSERT
    @RoleId       INT,
    @PermissionId INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO RolePermissions (RoleId, PermissionId)
    VALUES (@RoleId, @PermissionId);
END
GO

/* ============================================================
      STORED PROCEDURES – STARTER – USERROLES
      Tabela: dbo.UserRoles
      Colunas:
        UserId INT NOT NULL
        RoleId INT NOT NULL
      (recomendado: PRIMARY KEY (UserId, RoleId))
   ============================================================ */

---------------------------------------------------------------
-- 1) LISTAR ROLES POR USUÁRIO
--    Usado em GET /api/userroles/{userId}
--    Retorna dados da tabela Roles (Id, Name, Description)
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_USERROLES_GETBYUSER
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id,
        r.Name,
        r.Description
    FROM UserRoles ur
    INNER JOIN Roles r
        ON r.Id = ur.RoleId
    WHERE ur.UserId = @UserId
    ORDER BY r.Name;
END
GO

---------------------------------------------------------------
-- 2) LIMPAR TODAS AS ROLES DE UM USUÁRIO
--    Estratégia "replace-all":
--    1) Chama esta SP para remover tudo do UserId
--    2) Depois insere novamente a lista de RoleIds válidos
--    Retorna @@ROWCOUNT como DeletedCount (qtde antiga)
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_USERROLES_CLEAR
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM UserRoles
    WHERE UserId = @UserId;

    SELECT @@ROWCOUNT AS DeletedCount;
END
GO

---------------------------------------------------------------
-- 3) INSERIR UMA ROLE PARA UM USUÁRIO
--    Usado em loop na camada de serviço
--    (Pressupõe que UserId e RoleId já foram validados)
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_USERROLES_INSERT
    @UserId INT,
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO UserRoles (UserId, RoleId)
    VALUES (@UserId, @RoleId);
END
GO

/* ============================================================
      STORED PROCEDURES – STARTER – HOLIDAYS
      Tabela: dbo.Holidays
      Colunas:
        Id              INT IDENTITY(1,1) NOT NULL
        HolidayDate     SMALLDATETIME NOT NULL
        Description     VARCHAR(255) NULL
        BranchId        INT NOT NULL
        CreatedAt       SMALLDATETIME NULL
        CreatedByUserId INT NULL
        UpdatedAt       SMALLDATETIME NULL
        UpdatedByUserId INT NULL
        Active          VARCHAR(3) NULL  -- Yes / No
   ============================================================ */

---------------------------------------------------------------
-- 1) LISTAR TODOS OS FERIADOS
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_HOLIDAYS_LIST
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        h.Id,
        h.HolidayDate,
        h.Description,
        h.BranchId,
        bc.Description as BranchDescription,
        h.CreatedAt,
        h.CreatedByUserId,
        h.UpdatedAt,
        h.UpdatedByUserId,
        h.Active,
        cb.Name AS CreatedByName,
        ub.Name AS UpdatedByName
    FROM Holidays h
    LEFT JOIN Users cb ON cb.Id = h.CreatedByUserId
    LEFT JOIN Users ub ON ub.Id = h.UpdatedByUserId
    left join Branches bc on h.BranchId = bc.Id
    ORDER BY h.HolidayDate, h.Id;
END
GO

---------------------------------------------------------------
-- 2) BUSCAR FERIADO POR ID
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_HOLIDAYS_GETBYID
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        h.Id,
        h.HolidayDate,
        h.Description,
        h.BranchId,
        bc.Description as BranchDescription,
        h.CreatedAt,
        h.CreatedByUserId,
        h.UpdatedAt,
        h.UpdatedByUserId,
        h.Active,
        cb.Name AS CreatedByName,
        ub.Name AS UpdatedByName
    FROM Holidays h
    LEFT JOIN Users cb ON cb.Id = h.CreatedByUserId
    LEFT JOIN Users ub ON ub.Id = h.UpdatedByUserId
    left join Branches bc on h.BranchId = bc.Id
    WHERE h.Id = @Id;
END
GO

---------------------------------------------------------------
-- 3) CRIAR FERIADO
--    Retorna SCOPE_IDENTITY() como NewId
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_HOLIDAYS_CREATE
    @HolidayDate     SMALLDATETIME,
    @Description     VARCHAR(255) = NULL,
    @BranchId        INT,
    @CreatedAt       SMALLDATETIME,
    @CreatedByUserId INT = NULL,
    @Active          VARCHAR(3)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Holidays
    (
        HolidayDate,
        Description,
        BranchId,
        CreatedAt,
        CreatedByUserId,
        UpdatedAt,
        UpdatedByUserId,
        Active
    )
    VALUES
    (
        @HolidayDate,
        @Description,
        @BranchId,
        @CreatedAt,
        @CreatedByUserId,
        NULL,
        NULL,
        @Active
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId;
END
GO

---------------------------------------------------------------
-- 4) ATUALIZAR FERIADO
--    Retorna @@ROWCOUNT como RowsAffected
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_HOLIDAYS_UPDATE
    @Id              INT,
    @HolidayDate     SMALLDATETIME,
    @Description     VARCHAR(255) = NULL,
    @BranchId        INT,
    @UpdatedAt       SMALLDATETIME,
    @UpdatedByUserId INT = NULL,
    @Active          VARCHAR(3)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Holidays
    SET
        HolidayDate     = @HolidayDate,
        Description     = @Description,
        BranchId        = @BranchId,
        UpdatedAt       = @UpdatedAt,
        UpdatedByUserId = @UpdatedByUserId,
        Active          = @Active
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

---------------------------------------------------------------
-- 5) SOFT DELETE (Active = 'No')
--    Retorna @@ROWCOUNT como RowsAffected
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_HOLIDAYS_SOFTDELETE
    @Id              INT,
    @UpdatedAt       SMALLDATETIME,
    @UpdatedByUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Holidays
    SET
        Active          = 'No',
        UpdatedAt       = @UpdatedAt,
        UpdatedByUserId = @UpdatedByUserId
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
/* ============================================================
      STORED PROCEDURES – STARTER – BRANCHES
      Tabela: dbo.Branches
      Colunas:
        Id              INT IDENTITY(1,1) NOT NULL
        BranchCode      VARCHAR(6) NOT NULL
        Description     VARCHAR(255) NULL
        CreatedAt       SMALLDATETIME NULL
        CreatedByUserId INT NULL
        UpdatedAt       SMALLDATETIME NULL
        UpdatedByUserId INT NULL
        Active          VARCHAR(3) NULL  -- Yes / No
        RegionId        INT NOT NULL
   ============================================================ */

---------------------------------------------------------------
-- 1) LISTAR TODAS AS AGÊNCIAS (BRANCHES)
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_BRANCHES_LIST
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        b.Id,
        b.BranchCode,
        b.Description,
        b.CreatedAt,
        b.CreatedByUserId,
        b.UpdatedAt,
        b.UpdatedByUserId,
        b.Active,
        b.RegionId,
        cb.Name AS CreatedByName,
        ub.Name AS UpdatedByName
    FROM Branches b
    LEFT JOIN Users cb ON cb.Id = b.CreatedByUserId
    LEFT JOIN Users ub ON ub.Id = b.UpdatedByUserId
    ORDER BY b.Id;
END
GO

---------------------------------------------------------------
-- 2) BUSCAR BRANCH POR ID
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_BRANCHES_GETBYID
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        b.Id,
        b.BranchCode,
        b.Description,
        b.CreatedAt,
        b.CreatedByUserId,
        b.UpdatedAt,
        b.UpdatedByUserId,
        b.Active,
        b.RegionId,
        cb.Name AS CreatedByName,
        ub.Name AS UpdatedByName
    FROM Branches b
    LEFT JOIN Users cb ON cb.Id = b.CreatedByUserId
    LEFT JOIN Users ub ON ub.Id = b.UpdatedByUserId
    WHERE b.Id = @Id;
END
GO

---------------------------------------------------------------
-- 3) CRIAR BRANCH
--    Retorna SCOPE_IDENTITY() como NewId
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_BRANCHES_CREATE
    @BranchCode      VARCHAR(6),
    @Description     VARCHAR(255) = NULL,
    @RegionId        INT,
    @CreatedAt       SMALLDATETIME,
    @CreatedByUserId INT = NULL,
    @Active          VARCHAR(3)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Branches
    (
        BranchCode,
        Description,
        CreatedAt,
        CreatedByUserId,
        UpdatedAt,
        UpdatedByUserId,
        Active,
        RegionId
    )
    VALUES
    (
        @BranchCode,
        @Description,
        @CreatedAt,
        @CreatedByUserId,
        NULL,
        NULL,
        @Active,
        @RegionId
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId;
END
GO

---------------------------------------------------------------
-- 4) ATUALIZAR BRANCH
--    Retorna @@ROWCOUNT como RowsAffected
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_BRANCHES_UPDATE
    @Id              INT,
    @BranchCode      VARCHAR(6),
    @Description     VARCHAR(255) = NULL,
    @RegionId        INT,
    @UpdatedAt       SMALLDATETIME,
    @UpdatedByUserId INT = NULL,
    @Active          VARCHAR(3)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Branches
    SET
        BranchCode      = @BranchCode,
        Description     = @Description,
        RegionId        = @RegionId,
        UpdatedAt       = @UpdatedAt,
        UpdatedByUserId = @UpdatedByUserId,
        Active          = @Active
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

---------------------------------------------------------------
-- 5) SOFT DELETE (Active = 'No')
--    Retorna @@ROWCOUNT como RowsAffected
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_BRANCHES_SOFTDELETE
    @Id              INT,
    @UpdatedAt       SMALLDATETIME,
    @UpdatedByUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Branches
    SET
        Active          = 'No',
        UpdatedAt       = @UpdatedAt,
        UpdatedByUserId = @UpdatedByUserId
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

/* ============================================================
      STORED PROCEDURES – STARTER – REGIONS
      Tabela: dbo.Regions
      Colunas:
        Id              INT IDENTITY(1,1) NOT NULL
        Description     VARCHAR(255) NULL
        CreatedAt       SMALLDATETIME NULL
        CreatedByUserId INT NULL
        UpdatedAt       SMALLDATETIME NULL
        UpdatedByUserId INT NULL
        Active          VARCHAR(3) NULL  -- Yes / No
   ============================================================ */

---------------------------------------------------------------
-- 1) LISTAR TODAS AS REGIÕES
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_REGIONS_LIST
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id,
        r.Description,
        r.CreatedAt,
        r.CreatedByUserId,
        r.UpdatedAt,
        r.UpdatedByUserId,
        r.Active,
        cb.Name AS CreatedByName,
        ub.Name AS UpdatedByName
    FROM Regions r
    LEFT JOIN Users cb ON cb.Id = r.CreatedByUserId
    LEFT JOIN Users ub ON ub.Id = r.UpdatedByUserId
    ORDER BY r.Id;
END
GO

---------------------------------------------------------------
-- 2) BUSCAR REGIÃO POR ID
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_REGIONS_GETBYID
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id,
        r.Description,
        r.CreatedAt,
        r.CreatedByUserId,
        r.UpdatedAt,
        r.UpdatedByUserId,
        r.Active,
        cb.Name AS CreatedByName,
        ub.Name AS UpdatedByName
    FROM Regions r
    LEFT JOIN Users cb ON cb.Id = r.CreatedByUserId
    LEFT JOIN Users ub ON ub.Id = r.UpdatedByUserId
    WHERE r.Id = @Id;
END
GO

---------------------------------------------------------------
-- 3) CRIAR REGIÃO
--    Retorna SCOPE_IDENTITY() como NewId
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_REGIONS_CREATE
    @Description     VARCHAR(255) = NULL,
    @CreatedAt       SMALLDATETIME,
    @CreatedByUserId INT = NULL,
    @Active          VARCHAR(3)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Regions
    (
        Description,
        CreatedAt,
        CreatedByUserId,
        UpdatedAt,
        UpdatedByUserId,
        Active
    )
    VALUES
    (
        @Description,
        @CreatedAt,
        @CreatedByUserId,
        NULL,
        NULL,
        @Active
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId;
END
GO

---------------------------------------------------------------
-- 4) ATUALIZAR REGIÃO
--    Retorna @@ROWCOUNT como RowsAffected
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_REGIONS_UPDATE
    @Id              INT,
    @Description     VARCHAR(255) = NULL,
    @UpdatedAt       SMALLDATETIME,
    @UpdatedByUserId INT = NULL,
    @Active          VARCHAR(3)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Regions
    SET
        Description     = @Description,
        UpdatedAt       = @UpdatedAt,
        UpdatedByUserId = @UpdatedByUserId,
        Active          = @Active
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

---------------------------------------------------------------
-- 5) SOFT DELETE (Active = 'No')
--    Retorna @@ROWCOUNT como RowsAffected
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_REGIONS_SOFTDELETE
    @Id              INT,
    @UpdatedAt       SMALLDATETIME,
    @UpdatedByUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Regions
    SET
        Active          = 'No',
        UpdatedAt       = @UpdatedAt,
        UpdatedByUserId = @UpdatedByUserId
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

/* ============================================================
      STORED PROCEDURES – STARTER – SYSDATES
      Tabela: dbo.SysDates
      Colunas:
        Id             INT IDENTITY(1,1) NOT NULL
        SysCurrentDate SMALLDATETIME NULL
        sysClosedDate  SMALLDATETIME NULL
        SysName        VARCHAR(50) NULL
   ============================================================ */

---------------------------------------------------------------
-- 1) LISTAR TODAS AS DATAS DE SISTEMA
---------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.SP_STARTER_SYSDATES_LIST
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        SysCurrentDate,
        sysClosedDate AS SysClosedDate,
        SysName
    FROM SysDates
    ORDER BY Id;
END
GO
