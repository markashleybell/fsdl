<Query Kind="FSharpProgram">
  <Reference Relative="..\fsdl\bin\Debug\net462\fsdl.dll">C:\Src\fsdl\fsdl\bin\Debug\net462\fsdl.dll</Reference>
  <Reference Relative="..\fsdl.test\bin\Debug\net462\fsdl.test.dll">C:\Src\fsdl\fsdl.test\bin\Debug\net462\fsdl.test.dll</Reference>
  <NuGetReference>FSharp.Data</NuGetReference>
  <Namespace>fsdl.CodeGeneration</Namespace>
  <Namespace>fsdl.test</Namespace>
  <Namespace>fsdl.Types</Namespace>
  <Namespace>FSharp.Data</Namespace>
  <Namespace>FSharp.Data.Runtime</Namespace>
</Query>

let baseColumns = 
    [
        NotNull("CommonDate", DATE, NOW)
        NotNull("CommonFKID", INT, VAL(1))
    ]
    
let baseConstraints = 
    [
        ForeignKey("CommonFKID", "tCommonFKTable", "ID")
    ]

let dtoSpec = {
    ns = "fsdl.test"
    inheritFrom = Some "DTOBase"
    interfaces = None
    accessModifier = Public
    constructor = true
    setters = NoSetters
    partial = true
    dapperAttributes = true
}

let table1 = {
    name = "tEntity1"
    columns = 
        [
            NotNull("ID", GUID, NEWGUID)
            Identity("IDX", INT, 1, 1)
            Null("Name", CHR(16))
            //NotNull("Index", INT, VAL(100))
            NotNull("IsActive", BIT, FALSE)
            //Null("TotalPrice", MONEY)
            Null("Description", TEXT)
            NotNull("FKID", INT, NONE)
            //NotNull("EnumProp", ENUM(typeof<TestEnum>), VAL(int TestEnum.A))
        ] 
    constraints = 
        [
            PrimaryKey(["ID"])
            ForeignKey("FKID", "tFKTable", "ID")
        ]
    indexes = 
        [
            ClusteredUnique(["IDX"])
        ]
}

let table2 = {
    name = "tEntity2"
    columns = 
        [
            NotNull("ID", GUID, NEWGUID)
            //Identity("IDX", INT, 1, 1)
            //Null("Name", CHR(16))
            NotNull("Index", INT, VAL(100))
            //NotNull("IsActive", BIT, FALSE)
            Null("TotalPrice", MONEY)
            //Null("Description", TEXT)
            //NotNull("FKID", INT, NONE)
            NotNull("EnumProp", ENUM(typeof<TestEnum>), VAL(int TestEnum.A))
        ] 
    constraints = 
        [
            PrimaryKey(["ID"])
            //ForeignKey("FKID", "tFKTable", "ID")
        ]
    indexes = 
        [
            //ClusteredUnique(["IDX"])
        ]
}

let entity1 = { table = table1; dto = { name = "Entity1"; spec = dtoSpec } }
let entity2 = { table = table2; dto = { name = "Entity2"; spec = dtoSpec } }

let entities = [entity1; entity2]

generateDTOClassDefinitions entities baseColumns |> Console.Write |> ignore
generateTableDefinitions entities baseColumns |> Console.Write |> ignore
generateIndexDefinitions entities |> Console.Write |> ignore
generateConstraintDefinitions entities baseConstraints |> Console.Write |> ignore
