namespace fsdl.test

open NUnit.Framework
open FsUnit
open System
open fsdl.Types
open fsdl.CodeGeneration

module TestSqlData = 
    let commonColumns = 
        [
            NotNull("CommonDate", DATE, NOW)
            NotNull("CommonFKID", INT, VAL(1))
        ]
    
    let commonConstraints = 
        [
            ForeignKey("CommonFKID", "tCommonFKTable", "ID")
        ]

    let dtoSpec = {
        dtoNamespace = "test.com.DTO"
        baseClassName = Some "DTOBase"
        accessModifier = Public
        partial = true
        generateConstructor = true
        baseConstructorParameters = true
        setters = NoSetters
        addDapperAttributes = true
    }

    let testEntity = {
        table = {
            tableName = "tCreatedTable"
            columnSpecifications = 
                [
                    Identity("ID", INT, 1, 1)
                    Null("Name", CHR(16))
                    NotNull("GUID", GUID, NEWGUID)
                    NotNull("Date", DATE, NOW)
                    NotNull("Index", INT, VAL(100))
                    NotNull("Active", BIT, FALSE)
                    Null("Price", MONEY)
                    Null("Description", TEXT)
                    Null("FKID", INT)
                ] 
            constraintSpecifications = 
                [
                    PrimaryKey(["ID"])
                    ForeignKey("FKID", "tFKTable", "ID")
                ]
            indexSpecifications = 
                [
                    ClusteredUnique(["ID"])
                ]
        }
        dto = {
            className = "CreatedTable"
            spec = dtoSpec
        }
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
    [CommonFKID] INT NOT NULL CONSTRAINT DF_tCreatedTable_CommonFKID DEFAULT 1
)

GO

PRINT 'Tables Created'

"""

    let expectedConstraintDefinitions = """-- Create tCreatedTable constraints

ALTER TABLE [tCreatedTable] WITH CHECK ADD CONSTRAINT PK_tCreatedTable
PRIMARY KEY ([ID])

ALTER TABLE [tCreatedTable] WITH CHECK ADD CONSTRAINT FK_tCreatedTable_FKID_tFKTable_ID
FOREIGN KEY ([FKID]) REFERENCES [tFKTable] ([ID])

ALTER TABLE [tCreatedTable] WITH CHECK ADD CONSTRAINT FK_tCreatedTable_CommonFKID_tCommonFKTable_ID
FOREIGN KEY ([CommonFKID]) REFERENCES [tCommonFKTable] ([ID])

GO

PRINT 'Constraints Created'

"""

    let expectedIndexDefinitions = """-- Create tCreatedTable indexes

CREATE UNIQUE CLUSTERED INDEX IX_tCreatedTable_ID ON [tCreatedTable] ([ID])

GO

PRINT 'Indexes Created'

"""

[<TestFixture>]
type ``Basic SQL output tests`` () =
    [<Test>] 
    member test.``Check CREATE TABLE output against reference`` () =
        let sql = generateTableDefinitions 
                    [TestSqlData.testEntity] TestSqlData.commonColumns

        sql |> Console.WriteLine |> ignore
        sql |> should equal TestSqlData.expectedCreateTableDefinitions

    [<Test>] 
    member test.``Check constraint output against reference`` () =
        let sql = generateConstraintDefinitions 
                    [TestSqlData.testEntity] TestSqlData.commonConstraints

        sql |> Console.WriteLine |> ignore
        sql |> should equal TestSqlData.expectedConstraintDefinitions
    
    [<Test>] 
    member test.``Check index output against reference`` () =
        let sql = generateIndexDefinitions 
                    [TestSqlData.testEntity]

        sql |> Console.WriteLine |> ignore
        sql |> should equal TestSqlData.expectedIndexDefinitions
