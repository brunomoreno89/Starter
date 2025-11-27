use STARTERAPI
GO

/* DDL Script */


if not exists (select 1 from sys.objects where name = 'Users' and type = 'U')
    BEGIN
        print 'Creating table Users'
        create table Users (
            Id int IDENTITY (1,1) not null
            , Username VARCHAR (255) not null UNIQUE
            , Name VARCHAR (50) not null 
            , Email VARCHAR (255) not null UNIQUE
            , PasswordHash VARCHAR (255) not NULL
            , Role VARCHAR (255) not NULL
            , CreationDt smalldatetime not NULL
            , UpdatedDt smalldatetime NULL
            , CreatedBy int NULL
            , UpdatedBy int NULL
            , Active VARCHAR (3)
            , CONSTRAINT PK_Users PRIMARY KEY (Id)
        )
    END 




if not exists (select 1 from sys.objects where name = 'Items' and type = 'U')
    BEGIN
        print 'Creating table Items'
        create table Items (
            Id int IDENTITY (1,1) not null
            , Name VARCHAR (120) not null
            , Description VARCHAR (200) NULL
            , CreatedAt smalldatetime not NULL
            , UpdatedAt smalldatetime NULL
            , CreatedByUserId int NULL
            , UpdatedByUserId int NULL
            , Active VARCHAR (3)
            , CONSTRAINT PK_Items PRIMARY KEY (Id)
        )
    END 

if not exists (select 1 from sys.objects where name = 'Logs' and type = 'U')
    BEGIN
        print 'Creating table Logs'
        create table Logs (
            Id int IDENTITY (1,1) not null
            , UserId int not null
            , RoleId int NULL
            , PermissionId int NULL
            , ExecDate smalldatetime not NULL
            , Description varchar(250) not null
            , CONSTRAINT PK_Logs PRIMARY KEY (Id)
        )
    END 


if not exists (select 1 from sys.objects where name = 'Permissions' and type = 'U')
    BEGIN
        print 'Creating table Permissions'
        create table Permissions (
            Id int IDENTITY (1,1) not null
            , Name VARCHAR (150) not null
            , Description VARCHAR (255) NULL
            , CreationDt smalldatetime not NULL
            , UpdateDt smalldatetime NULL
            , CreatedBy int NULL
            , UpdatedBy int NULL
            , Active VARCHAR (3)
            , CONSTRAINT PK_Permissions PRIMARY KEY (Id)
        )
    END 

if not exists (select 1 from sys.objects where name = 'Roles' and type = 'U')
    BEGIN
        print 'Creating table Roles'
        create table Roles (
            Id int IDENTITY (1,1) not null
            , Name VARCHAR (150) not null
            , Description VARCHAR (255) NULL
            , CreationDt smalldatetime not NULL
            , UpdateDt smalldatetime NULL
            , CreatedBy int NULL
            , UpdatedBy int NULL
            , Active VARCHAR (3)
            , CONSTRAINT PK_Roles PRIMARY KEY (Id)
        )
    END 

 
if not exists (select 1 from sys.objects where name = 'RolePermissions' and type = 'U')
    BEGIN
        print 'Creating table RolePermissions'
        create table RolePermissions (
            
            RoleId int not null
            , PermissionId int not null
            
        )
    END    

if not exists (select 1 from sys.objects where name = 'UserRoles' and type = 'U')
    BEGIN
        print 'Creating table UserRoles'
        create table UserRoles (
            
            RoleId int not null
            , UserId int not null
            
        )
    END    

if not exists (select 1 from sys.objects where name = 'RefreshTokens' and type = 'U')
    BEGIN
        print 'Creating table RefreshTokens'
        create table RefreshTokens (
            Id int IDENTITY (1,1) not null
            , UserId int not null
            , TokenHash VARCHAR (64) not NULL
            , ExpiresAt smalldatetime not NULL
            , CreatedAt smalldatetime not NULL
            , CreatedByIp VARCHAR (64) not NULL
            , RevokedAt smalldatetime NULL
            , RevokedByIp VARCHAR (64) not NULL
            , ReplacedByTokenHash VARCHAR (64) not NULL
            , CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id)
        )
    END 




if not exists (select 1 from sys.objects where name = 'RevokedAccessTokens' and type = 'U')
    BEGIN
        print 'Creating table RevokedAccessTokens'
        create table RevokedAccessTokens (
            Id int IDENTITY (1,1) not null
            , UserId int not null
            , Jti VARCHAR (64) not NULL
            , ExpiresAt smalldatetime not NULL
       
            , CONSTRAINT PK_RevokedAccessTokens PRIMARY KEY (Id)
        )
    END 

if not exists (select 1 from sys.objects where name = 'SysDates' and type = 'U')
    BEGIN
        print 'Creating table SysDates'
        create table SysDates (
            Id int IDENTITY (1,1) not null
            , SysCurrentDate smalldatetime null
            , sysClosedDate smalldatetime null
            , SysName varchar(50) null
            , CONSTRAINT PK_SysDates PRIMARY KEY (Id)
            
        )
    END    