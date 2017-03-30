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
        constraintSpecifications = [PrimaryKey("ID")
                                    ForeignKey("FKID", "tFKTable", "ID")]
        indexSpecifications = []
        addDapperAttributes = true
    }

    let expectedClassDefinitions = """using System;
using System.ComponentModel.DataAnnotations;
using d = Dapper.Contrib.Extensions;

namespace test.com.DTO
{
    [d.Table("tCreatedTable")]
    public class CreatedTable : DTOBase
    {
        [d.Key]
        public int ID { get; set; }
        [StringLength(16)]
        public string Name { get; set; }
        public Guid GUID { get; set; }
        public DateTime Date { get; set; }
        public int Index { get; set; }
        public bool Active { get; set; }
        public decimal? Price { get; set; }
        public string Description { get; set; }
        public int? FKID { get; set; }
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