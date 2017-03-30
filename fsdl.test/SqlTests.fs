namespace fsdl.test

open NUnit.Framework
open FsUnit
open System
open fsdl

module TestSqlData = 
    let commoncols = [NotNull("CommonDate", DATE, NOW)
                      NotNull("CommonFKID", INT, VAL(1))]
    
    let commonfks = [ForeignKey("CommonFKID", "tCommonFKTable", "ID")]

    let testTable = {
            stype = CREATE
            name = "tCreatedTable"
            dtoname = "CreatedTable"
            dtonamespace = "test.com.DTO"
            dtobase = Some "DTOBase"
            cols = [Identity("ID", INT, 1, 1)
                    Null("Name", CHR(16))
                    NotNull("GUID", GUID, NEWGUID)
                    NotNull("Date", DATE, NOW)
                    NotNull("Index", INT, VAL(100))
                    NotNull("Active", BIT, FALSE)
                    Null("Price", MONEY)
                    Null("Description", TEXT)
                    Null("FKID", INT)] 
            constraints = [ASC("ID")
                           ASC("GUID")]
            fks = [ForeignKey("FKID", "tFKTable", "ID")] 
            dapperext = true
        }

    let expectedCreateTableDefinitions = """-- Create tCreatedTable
CREATE TABLE [tCreatedTable] (
    [ID] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(16) NULL,
    [GUID] UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_tCreatedTable_GUID DEFAULT NEWID(),
    [Date] DATETIME NOT NULL CONSTRAINT DF_tCreatedTable_Date DEFAULT GETDATE(),
    [Index] INT NOT NULL CONSTRAINT DF_tCreatedTable_Index DEFAULT 100,
    [Active] BIT NOT NULL CONSTRAINT DF_tCreatedTable_Active DEFAULT 0,
    [Price] DECIMAL(18,2) NULL,
    [Description] NVARCHAR(MAX) NULL,
    [FKID] INT NULL,
    [CommonDate] DATETIME NOT NULL CONSTRAINT DF_tCreatedTable_CommonDate DEFAULT GETDATE(),
    [CommonFKID] INT NOT NULL CONSTRAINT DF_tCreatedTable_CommonFKID DEFAULT 1,
    CONSTRAINT PK_tCreatedTable PRIMARY KEY CLUSTERED (
        [ID] ASC,
        [GUID] ASC
    )
)

GO

PRINT 'Tables Created'

"""

    let expectedKeyDefinitions = """-- Create tCreatedTable foreign keys
ALTER TABLE [tCreatedTable] WITH CHECK ADD CONSTRAINT FK_tCreatedTable_tFKTable_FKID
FOREIGN KEY ([FKID]) REFERENCES [tFKTable] ([ID])
ALTER TABLE [tCreatedTable] WITH CHECK ADD CONSTRAINT FK_tCreatedTable_tCommonFKTable_CommonFKID
FOREIGN KEY ([CommonFKID]) REFERENCES [tCommonFKTable] ([ID])

GO

PRINT 'Foreign Keys Created'

"""

    let expectedCreateTableAndKeyDefinitions = expectedCreateTableDefinitions + expectedKeyDefinitions

[<TestFixture>]
type ``Basic SQL output tests`` () =
    [<Test>] 
    member test.``Check CREATE TABLE output against reference`` () =
        let sql = fsdl.generateTableDefinitions 
                    [TestSqlData.testTable] TestSqlData.commoncols

        sql |> Console.WriteLine |> ignore
        sql |> should equal TestSqlData.expectedCreateTableDefinitions

    [<Test>] 
    member test.``Check FK output against reference`` () =
        let sql = fsdl.generateKeyDefinitions 
                    [TestSqlData.testTable] TestSqlData.commonfks

        sql |> Console.WriteLine |> ignore
        sql |> should equal TestSqlData.expectedKeyDefinitions

    [<Test>] 

    member test.``Check combined CREATE TABLE and FK output against reference`` () =
        let sql = fsdl.generateTableAndKeyDefinitions 
                    [TestSqlData.testTable] TestSqlData.commoncols TestSqlData.commonfks

        sql |> Console.WriteLine |> ignore
        sql |> should equal TestSqlData.expectedCreateTableAndKeyDefinitions