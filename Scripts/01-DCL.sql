
/* DCL Script */


/* Creating Login */
CREATE LOGIN STARTERAPI_USER WITH PASSWORD = 'ASNpwr#1989!@';


use STARTERAPI
go
/* Creating user */
CREATE USER STARTERAPI_USER FOR LOGIN STARTERAPI_USER;

/* Adding User to db_owner group */
ALTER ROLE db_owner ADD MEMBER STARTERAPI_USER;
