USE [master]
GO

/* =========================================================
   OLAPDW v1 - Bootstrap de base destino
   Crea la base DW separada si aun no existe.
   ========================================================= */

IF DB_ID(N'TesisDW_Extensible') IS NULL
BEGIN
    CREATE DATABASE [TesisDW_Extensible];
END
GO

SELECT
    name AS DatabaseName,
    create_date AS CreatedAt,
    compatibility_level AS CompatibilityLevel,
    state_desc AS StateDescription
FROM sys.databases
WHERE name = N'TesisDW_Extensible';
GO
