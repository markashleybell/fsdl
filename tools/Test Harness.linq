<Query Kind="FSharpProgram">
  <Reference Relative="..\fsdl\bin\Debug\net462\fsdl.dll">C:\Src\fsdl\fsdl\bin\Debug\net462\fsdl.dll</Reference>
  <Namespace>fsdl</Namespace>
</Query>

let commonColumns = 
    [
        NotNull("CommonDate", DATE, NOW)
        NotNull("CommonFKID", INT, VAL(1))
    ]
    
let commonConstraints = 
    [
        ForeignKey("CommonFKID", "tCommonFKTable", "ID")
    ]

let testTable = {
    sqlStatementType = CREATE
    tableName = "tCreatedTable"
    dtoClassName = "CreatedTable"
    dtoNamespace = "fsdl.test"
    dtoBaseClassName = Some("IDTO")
    columnSpecifications = 
        [
            NotNull("ID", GUID, NEWGUID)
            Identity("IDX", INT, 1, 1)
            Null("Name", CHR(16))
            NotNull("Index", INT, VAL(100))
            NotNull("IsActive", BIT, FALSE)
            Null("TotalPrice", MONEY)
            Null("Description", TEXT)
            NotNull("FKID", INT, NONE)
        ] 
    constraintSpecifications = 
        [
            PrimaryKey(["ID"])
            ForeignKey("FKID", "tFKTable", "ID")
        ]
    indexSpecifications = 
        [
            ClusteredUnique(["IDX"])
        ]
    addDapperAttributes = true
    partial = true
    generateConstructor = true
    baseConstructorParameters = true
    setters = PrivateSetter
}

fsdl.generateDTOClassDefinitions [testTable] commonColumns |> Dump |> ignore

fsdl.generateTableDefinitions [testTable] commonColumns |> Dump |> ignore

fsdl.generateIndexDefinitions [testTable] |> Dump |> ignore

fsdl.generateConstraintDefinitions [testTable] commonConstraints |> Dump |> ignore