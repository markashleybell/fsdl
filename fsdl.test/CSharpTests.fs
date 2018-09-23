namespace fsdl.test

open NUnit.Framework
open FsUnit
open System
open fsdl.Types
open fsdl.CodeGeneration

module TestCSharpData = 
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
            indexSpecifications = []
        }
        dto = {
            className = "CreatedTable"
            spec = dtoSpec
        }
    }

    let expectedClassDefinitions = """using System;
using System.ComponentModel.DataAnnotations;
using d = Dapper.Contrib.Extensions;

namespace test.com.DTO
{
    [d.Table("tCreatedTable")]
    public partial class CreatedTable : DTOBase
    {
        public CreatedTable(
            int id,
            string name,
            Guid guid,
            DateTime date,
            int index,
            bool active,
            decimal? price,
            string description,
            int? fkId,
            DateTime commonDate,
            int commonFkId)
        {
            ID = id;
            Name = name;
            GUID = guid;
            Date = date;
            Index = index;
            Active = active;
            Price = price;
            Description = description;
            FKID = fkId;
            CommonDate = commonDate;
            CommonFKID = commonFkId;
        }

        [d.Key]
        public int ID { get; }

        [StringLength(16)]
        public string Name { get; }

        public Guid GUID { get; }

        public DateTime Date { get; }

        public int Index { get; }

        public bool Active { get; }

        public decimal? Price { get; }

        public string Description { get; }

        public int? FKID { get; }
    }
}
"""


    let toString classDefinitionList = 
        classDefinitionList 
        |> List.map (fun (_, def) -> sprintf "%s" def) 
        |> String.concat (Environment.NewLine + Environment.NewLine)

[<TestFixture>]
type ``Basic C# output tests`` () =
    [<Test>] 
    member test.``Check DTO class output against reference`` () =
        let code = generateDTOClassDefinitions 
                        [TestCSharpData.testEntity] TestCSharpData.commonColumns

        code |> TestCSharpData.toString |> Console.WriteLine |> ignore
        code |> TestCSharpData.toString |> should equal TestCSharpData.expectedClassDefinitions

    [<Test>] 
    member test.``Check DTO class list output against reference`` () =
        let list = generateDTOClassDefinitions
                        [TestCSharpData.testEntity] TestCSharpData.commonColumns

        let (name, code) = list.Head

        code |> Console.WriteLine |> ignore
        name |> should equal TestCSharpData.testEntity.dto.className
        code |> should equal TestCSharpData.expectedClassDefinitions

    [<Test>]
    member test.``Check unnecessary using statements are not emitted`` () =
        let tbl = {
            table = {
                tableName = "tCreatedTable"
                columnSpecifications = 
                    [
                        Identity("ID", INT, 1, 1)
                        Null("Price", MONEY)
                    ] 
                constraintSpecifications = 
                    [
                        PrimaryKey(["ID"])
                    ]
                indexSpecifications = []
            }
            dto = {
                className = "CreatedTable"
                spec = {
                    dtoNamespace = "test.com.DTO"
                    baseClassName = Some "DTOBase"
                    accessModifier = Public
                    partial = true
                    generateConstructor = true
                    baseConstructorParameters = true
                    setters = NoSetters
                    addDapperAttributes = false
                }
            }
        }

        let list = generateDTOClassDefinitions [tbl] []

        let (_, code) = list.Head

        code |> Console.WriteLine |> ignore
        code |> should not' (contain (sprintf "using System;"))
        code |> should not' (contain (sprintf "using System.ComponentModel.DataAnnotations;"))
        code |> should not' (contain (sprintf "using d = Dapper.Contrib.Extensions;"))
