-- Objects whose names need quoting, for the schema reader test (SF-033).
-- Run it in an empty test database, for example one named SharpFactoryQuote's (an apostrophe in the
-- database name too). Batches are separated by GO.
CREATE SCHEMA [Sales's]
GO
CREATE TABLE [Sales's].[Region]
(
    [Id] INT NOT NULL CONSTRAINT [PK_Region] PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL CONSTRAINT [UQ_Region_Name] UNIQUE
)
GO
CREATE TABLE [Sales's].[O'Brien]
(
    [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_O'Brien] PRIMARY KEY,
    [Customer's Name] NVARCHAR(100) NOT NULL CONSTRAINT [DF_O'Brien_Name] DEFAULT (N'O''Neil'),
    [RegionId] INT NOT NULL CONSTRAINT [FK_O'Brien_Region] REFERENCES [Sales's].[Region]([Id])
)
GO
CREATE PROCEDURE [Sales's].[Get]]Orders] @RegionId INT
AS
SELECT [Id], [Customer's Name] FROM [Sales's].[O'Brien] WHERE [RegionId] = @RegionId
GO
CREATE FUNCTION [Sales's].[Count's] (@RegionId INT)
RETURNS INT
AS
BEGIN
    RETURN (SELECT COUNT(*) FROM [Sales's].[O'Brien] WHERE [RegionId] = @RegionId)
END
GO
