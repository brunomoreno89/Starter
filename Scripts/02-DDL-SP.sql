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

CREATE OR ALTER PROCEDURE dbo.SP_STARTER_USERS_LIST
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        u.Id
        , u.Username
        , u.Name
        , u.Email
        , u.PasswordHash
        , u.Role
        , u.CreationDt
        , u.UpdatedDt
        , u.CreatedBy
        , cu.Name AS CreatedByName
        , u.UpdatedBy
        , uu.Name AS UpdatedByName
        , u.Active
    FROM Users u
    LEFT JOIN Users cu ON cu.Id = u.CreatedBy
    LEFT JOIN Users uu ON uu.Id = u.UpdatedBy
    ORDER BY u.Id;
END
GO