<Query Kind="FSharpProgram">
  <Reference Relative="fsdl\bin\Debug\fsdl.dll">E:\Src\fsdl\fsdl\bin\Debug\fsdl.dll</Reference>
  <Namespace>fsdl</Namespace>
</Query>

let commonColumns = [NotNull("CommonDate", DATE, NOW)
                     NotNull("CommonFKID", INT, VAL(1))]
    
let commonConstraints = [ForeignKey("CommonFKID", "tCommonFKTable", "ID")]

let testTable = {
    stype=CREATE
    name="tCreatedTable"
    dtoname = "CreatedTable"
    dtonamespace = "fsdl.test"
    dtobase = Some("IDTO")
    cols = [NotNull("ID", GUID, NEWGUID)
            Identity("IDX", INT, 1, 1)
            Null("Name", CHR(16))
            NotNull("Index", INT, VAL(100))
            NotNull("Active", BIT, FALSE)
            Null("Price", MONEY)
            Null("Description", TEXT)
            NotNull("FKID", INT, NONE)] 
    constraints = [PrimaryKey("ID")
                   ForeignKey("FKID", "tFKTable", "ID")]
    indexes = []
    dapperext = true
}

fsdl.generateDTOClassDefinitions [testTable] commonColumns |> Dump |> ignore

fsdl.generateTableDefinitions [testTable] commonColumns |> Dump |> ignore

fsdl.generateConstraintDefinitions [testTable] commonConstraints |> Dump |> ignore

fsdl.generateTableAndConstraintDefinitions [testTable] commonColumns commonConstraints |> Dump |> ignore