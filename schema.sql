-- MySQL dump 10.13  Distrib 9.3.0, for macos15.2 (arm64)
--
-- Host: 127.0.0.1    Database: starterdb
-- ------------------------------------------------------
-- Server version	9.3.0

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Current Database: `starterdb`
--

/*!40000 DROP DATABASE IF EXISTS `starterdb`*/;

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `starterdb` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `starterdb`;

--
-- Table structure for table `Items`
--

DROP TABLE IF EXISTS `Items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Items` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(120) NOT NULL,
  `Description` longtext,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `CreatedByUserId` int DEFAULT NULL,
  `UpdatedByUserId` int DEFAULT NULL,
  `Active` varchar(3) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Logs`
--

DROP TABLE IF EXISTS `Logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Logs` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `RoleId` int DEFAULT NULL,
  `PermissionId` int DEFAULT NULL,
  `ExecDate` datetime(6) NOT NULL,
  `Description` varchar(250) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `fk_lg_user` (`UserId`),
  KEY `fk_lg_role` (`RoleId`),
  KEY `fk_lg_permission` (`PermissionId`),
  CONSTRAINT `fk_lg_permission` FOREIGN KEY (`PermissionId`) REFERENCES `Permissions` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `fk_lg_role` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `fk_lg_user` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=44 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Permissions`
--

DROP TABLE IF EXISTS `Permissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Permissions` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(128) NOT NULL,
  `Description` varchar(255) DEFAULT NULL,
  `CreationDt` datetime DEFAULT NULL,
  `CreatedBy` int DEFAULT NULL,
  `UpdateDt` datetime DEFAULT NULL,
  `UpdatedBy` int DEFAULT NULL,
  `Active` varchar(3) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Name` (`Name`)
) ENGINE=InnoDB AUTO_INCREMENT=56 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `RefreshTokens`
--

DROP TABLE IF EXISTS `RefreshTokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `RefreshTokens` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `TokenHash` varchar(64) NOT NULL,
  `ExpiresAt` datetime NOT NULL,
  `CreatedAt` datetime NOT NULL,
  `CreatedByIp` varchar(64) NOT NULL,
  `RevokedAt` datetime DEFAULT NULL,
  `RevokedByIp` varchar(64) DEFAULT NULL,
  `ReplacedByTokenHash` varchar(64) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_User_TokenHash` (`UserId`,`TokenHash`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `RevokedAccessTokens`
--

DROP TABLE IF EXISTS `RevokedAccessTokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `RevokedAccessTokens` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Jti` varchar(64) NOT NULL,
  `ExpiresAt` datetime NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Jti` (`Jti`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `RolePermissions`
--

DROP TABLE IF EXISTS `RolePermissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `RolePermissions` (
  `RoleId` int NOT NULL,
  `PermissionId` int NOT NULL,
  PRIMARY KEY (`RoleId`,`PermissionId`),
  KEY `fk_rp_perm` (`PermissionId`),
  CONSTRAINT `fk_rp_perm` FOREIGN KEY (`PermissionId`) REFERENCES `Permissions` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `fk_rp_role` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Roles`
--

DROP TABLE IF EXISTS `Roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Roles` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(64) NOT NULL,
  `Description` varchar(255) DEFAULT NULL,
  `CreationDt` datetime DEFAULT NULL,
  `CreatedBy` int DEFAULT NULL,
  `UpdateDt` datetime DEFAULT NULL,
  `UpdatedBy` int DEFAULT NULL,
  `Active` varchar(3) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Name` (`Name`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `SysDates`
--

DROP TABLE IF EXISTS `SysDates`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `SysDates` (
  `SysCurrentDate` datetime DEFAULT NULL,
  `SysClosedDate` datetime DEFAULT NULL,
  `SysName` varchar(50) DEFAULT NULL,
  `Id` int NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `UserRoles`
--

DROP TABLE IF EXISTS `UserRoles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UserRoles` (
  `UserId` int NOT NULL,
  `RoleId` int NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `fk_ur_role` (`RoleId`),
  CONSTRAINT `fk_ur_role` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `fk_ur_user` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Users`
--

DROP TABLE IF EXISTS `Users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Users` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(255) NOT NULL,
  `Email` varchar(255) NOT NULL,
  `PasswordHash` longtext NOT NULL,
  `Role` longtext NOT NULL,
  `Active` varchar(3) NOT NULL,
  `CreationDt` datetime DEFAULT NULL,
  `UpdatedDt` datetime DEFAULT NULL,
  `UpdatedBy` int DEFAULT NULL,
  `CreatedBy` int DEFAULT NULL,
  `Name` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Users_Email` (`Email`),
  UNIQUE KEY `IX_Users_Username` (`Username`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping events for database 'starterdb'
--

--
-- Dumping routines for database 'starterdb'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-09-07 22:41:05

--
-- Creating Admin user to start
--

use starterdb;

insert into `Users` (`Username`, `Email`, `PasswordHash`,`Role`,`Name`) values('Admin', 'starterdb;','$2a$11$s7FupNonsbCMnHZ8ftQVkuWLFPjB8sFuYU7L04fuKiNmJ0oa/QGXu', 'Admin', 'Admin User');
