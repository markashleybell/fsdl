namespace fsdl.test

open NUnit.Framework
open FsUnit
open System
open fsdl

module TestCSharpData = 
    let commonColumns = [NotNull("CommonDate", DATE, NOW)
                         NotNull("CommonFKID", INT, VAL(1))]
    
    let commonConstraints = [ForeignKey("CommonFKID", "tCommonFKTable", "ID")]

    let testTable = {
        sqlStatementType = CREATE
        tableName = "tCreatedTable"
        dtoClassName = "CreatedTable"
        dtoNamespace = "test.com.DTO"
        dtoBaseClassName = Some "DTOBase"
        columnSpecifications = [Identity("ID", INT, 1, 1)
                                Null("Name", CHR(16))
                                NotNull("GUID", GUID, NEWGUID)
                                NotNull("Date", DATE, NOW)
                                NotNull("Index", INT, VAL(100))
                                NotNull("Active", BIT, FALSE)
                                Null("Price", MONEY)
                                Null("Description", TEXT)
                                Null("FKID", INT)] 
        constraintSpecifications = [PrimaryKey(["ID"])
                                    ForeignKey("FKID", "tFKTable", "ID")]
        indexSpecifications = []
        addDapperAttributes = true
        generateConstructor = true
        baseConstructorParameters = true
        immutable = true
    }

    let expectedClassDefinitions = """using System;
using System.ComponentModel.DataAnnotations;
using d = Dapper.Contrib.Extensions;

namespace test.com.DTO
{
    [d.Table("tCreatedTable")]
    public class CreatedTable : DTOBase
    {
        public CreatedTable(int id, string name, Guid guid, DateTime date, int index, bool active, decimal? price, string description, int? fkID, DateTime commonDate, int commonFkID)
        {
            ID = id;
            Name = name;
            GUID = guid;
            Date = date;
            Index = index;
            Active = active;
            Price = price;
            Description = description;
            FKID = fkID;
            CommonDate = commonDate;
            CommonFKID = commonFkID;
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

[<TestFixture>]
type ``Basic C# output tests`` () =
    [<Test>] 
    member test.``Check DTO class output against reference`` () =
        let code = fsdl.generateDTOClassDefinitions 
                    [TestCSharpData.testTable] TestCSharpData.commonColumns

        code |> Console.WriteLine |> ignore
        code |> should equal TestCSharpData.expectedClassDefinitions

    [<Test>] 
    member test.``Check DTO class list output against reference`` () =
        let list = fsdl.generateDTOClassDefinitionList 
                        [TestCSharpData.testTable] TestCSharpData.commonColumns

        let (name, code) = list.Head

        code |> Console.WriteLine |> ignore
        name |> should equal TestCSharpData.testTable.dtoClassName
        code |> should equal TestCSharpData.expectedClassDefinitions