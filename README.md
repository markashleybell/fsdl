# fsdl

[![NuGet](https://img.shields.io/nuget/v/fsdl.svg)](https://www.nuget.org/packages/fsdl)

`fsdl` is a code generation library for creating Dapper-based C# persistence layer classes with backing SQL table definitions. It's not comprehensive by any means, but I've found it to be a real time saver during initial development of small, data-driven .NET projects.

`fsdl` works from entity specifications written in F#. You specify a set of database columns and the library generates `CREATE` SQL and a C# DTO class which maps to the table, all with the correct column sizes, nullable constraints, optional Dapper attributes etc.

## Reference

You can specify most basic column data, constraint and index types. As the F# type syntax is so readable, I'll just directly paste the source code for now:

    type DataType = INT | BIT | DATE | MONEY | TEXT | GUID | CHR of int | ENUM of Type

    type Default = NONE | NULL | TRUE | FALSE | NOW | NEWGUID | VAL of int

    type ColSpec =
        | Null of string * DataType
        | NotNull of string * DataType * Default
        | Identity of string * DataType * int * int

    type ConstraintSpec =
        | PrimaryKey of string list
        | ForeignKey of string * string * string

    type IndexSpec =
        | Clustered of string list
        | ClusteredUnique of string list
        | NonClustered of string list
        | NonClusteredUnique of string list

    type PropertySetters = PublicSetters | PrivateSetters | NoSetters

    type AccessModifier = Public | Private | Internal

See the example below for... well, an example.

## Examples

Here's an example specification, along with the code it generates.

### Specification

        // Common columns for all tables and DTO classes (e.g. auditing data)
    let baseColumns = [
        NotNull("Updated", DATE, NOW)
        NotNull("UserID", INT, VAL(1))
    ]

    // As above, but for table constraints
    let baseConstraints = [
        ForeignKey("UserID", "Users", "ID")
    ]

    // Specification for the generated C# DTO classes
    let dtoSpec = {
        ns = "fsdl.test"                    // Namespace for classes
        inheritFrom = Some "EntityBase"     // Classes can optionally inherit from a base class...
        interfaces = Some ["IDTO"]          // ... and optionally one or more interfaces
        accessModifier = Public             // Access modifier for the generated classes
        constructor = true                  // Classes can be generated with or without constructors
        setters = NoSetters                 // Generate public or private property setters (or omit them entirely)
        partial = true                      // Partial class generation
        dapperAttributes = true             // If true, DTOs are decorated with Dapper annotations
    }

    // Defines an entity, specifying table data types and constraints
    let testEntity = {
        table = {
            name = "TestEntities"
            columns = [
                NotNull("ID", GUID, NEWGUID)
                Identity("IDX", INT, 1, 1)
                Null("Name", CHR(16))
                NotNull("Index", INT, VAL(100))
                NotNull("IsActive", BIT, FALSE)
                Null("TotalPrice", MONEY)
                Null("Description", TEXT)
                NotNull("FKID", INT, NONE)
                NotNull("EnumProp", ENUM(typeof<TestEnum>), VAL(int TestEnum.A))
            ] 
            constraints = [
                PrimaryKey(["ID"])
                ForeignKey("FKID", "FKEntities", "ID")
            ]
            indexes = [
                ClusteredUnique(["IDX"])
            ]
        }
        dto = { 
            name = "TestEntity"
            spec = dtoSpec 
        }
    }

    let entities = [testEntity]

    // Generate the code and do whatever you like with it (save to files, concatenate etc)

    generateDTOClassDefinitions entities baseColumns |> Console.Write |> ignore
    generateTableDefinitions entities baseColumns |> Console.Write |> ignore
    generateIndexDefinitions entities |> Console.Write |> ignore
    generateConstraintDefinitions entities baseConstraints |> Console.Write |> ignore

### Generated SQL

    -- Create TestEntities
    CREATE TABLE [TestEntities] (
        [ID] UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_TestEntities_ID DEFAULT NEWID(),
        [IDX] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(16) NULL,
        [Index] INT NOT NULL CONSTRAINT DF_TestEntities_Index DEFAULT 100,
        [IsActive] BIT NOT NULL CONSTRAINT DF_TestEntities_IsActive DEFAULT 0,
        [TotalPrice] DECIMAL(18,2) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [FKID] INT NOT NULL,
        [EnumProp] INT NOT NULL CONSTRAINT DF_TestEntities_EnumProp DEFAULT 1,
        [Updated] DATETIME NOT NULL CONSTRAINT DF_TestEntities_Updated DEFAULT GETDATE(),
        [UserID] INT NOT NULL CONSTRAINT DF_TestEntities_UserID DEFAULT 1
    )

    GO

    PRINT 'Tables Created'

    -- Create TestEntities indexes

    CREATE UNIQUE CLUSTERED INDEX IX_TestEntities_IDX ON [TestEntities] ([IDX])

    GO

    PRINT 'Indexes Created'

    -- Create TestEntities constraints

    ALTER TABLE [TestEntities] WITH CHECK ADD CONSTRAINT PK_TestEntities
    PRIMARY KEY ([ID])

    ALTER TABLE [TestEntities] WITH CHECK ADD CONSTRAINT FK_TestEntities_FKID_FKEntities_ID
    FOREIGN KEY ([FKID]) REFERENCES [FKEntities] ([ID])

    ALTER TABLE [TestEntities] WITH CHECK ADD CONSTRAINT FK_TestEntities_UserID_Users_ID
    FOREIGN KEY ([UserID]) REFERENCES [Users] ([ID])

    GO

    PRINT 'Constraints Created'

### Generated C#

    using System;
    using System.ComponentModel.DataAnnotations;
    using d = Dapper.Contrib.Extensions;

    namespace fsdl.test
    {
        [d.Table("TestEntities")]
        public partial class TestEntity : EntityBase, IDTO
        {
            public TestEntity(
                Guid id,
                int idx,
                string name,
                int index,
                bool isActive,
                decimal? totalPrice,
                string description,
                int fkId,
                TestEnum enumProp,
                DateTime updated,
                int userId)
            {
                ID = id;
                IDX = idx;
                Name = name;
                Index = index;
                IsActive = isActive;
                TotalPrice = totalPrice;
                Description = description;
                FKID = fkId;
                EnumProp = enumProp;
                Updated = updated;
                UserID = userId;
            }

            [d.ExplicitKey]
            public Guid ID { get; }

            [d.Computed]
            public int IDX { get; }

            [StringLength(16)]
            public string Name { get; }

            public int Index { get; }

            public bool IsActive { get; }

            public decimal? TotalPrice { get; }

            public string Description { get; }

            public int FKID { get; }

            public TestEnum EnumProp { get; }
        }
    }

If you use [LINQPad](https://www.linqpad.net/) (and if you don't, you should...), there is a [test harness](https://github.com/markashleybell/fsdl/blob/master/tools/test-harness.linq) in this repository which allows you to play around and see what will be generated when you alter the specification.
