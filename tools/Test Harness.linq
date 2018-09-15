<Query Kind="FSharpProgram">
  <Reference Relative="..\fsdl\bin\Debug\net462\fsdl.dll">C:\Src\fsdl\fsdl\bin\Debug\net462\fsdl.dll</Reference>
  <NuGetReference>FSharp.Data</NuGetReference>
  <Namespace>fsdl.CodeGeneration</Namespace>
  <Namespace>fsdl.Types</Namespace>
  <Namespace>FSharp.Data</Namespace>
  <Namespace>FSharp.Data.Runtime</Namespace>
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

generateDTOClassDefinitions [testTable] commonColumns |> Dump |> ignore

generateTableDefinitions [testTable] commonColumns |> Dump |> ignore

generateIndexDefinitions [testTable] |> Dump |> ignore

generateConstraintDefinitions [testTable] commonConstraints |> Dump |> ignore
