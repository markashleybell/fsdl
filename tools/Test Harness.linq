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
    dtoNamespace = "fsdl.test"
    baseClassName = Some "IDTO"
    accessModifier = Public
    partial = true
    generateConstructor = true
    baseConstructorParameters = true
    setters = NoSetters
    addDapperAttributes = true
}

let table1 = {
    tableName = "tEntity1"
    columnSpecifications = 
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
    constraintSpecifications = 
        [
            PrimaryKey(["ID"])
            ForeignKey("FKID", "tFKTable", "ID")
        ]
    indexSpecifications = 
        [
            ClusteredUnique(["IDX"])
        ]
}

let table2 = {
    tableName = "tEntity2"
    columnSpecifications = 
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
    constraintSpecifications = 
        [
            PrimaryKey(["ID"])
            //ForeignKey("FKID", "tFKTable", "ID")
        ]
    indexSpecifications = 
        [
            //ClusteredUnique(["IDX"])
        ]
}

let entity1 = { table = table1; dto = { className = "Entity1"; spec = dtoSpec } }
let entity2 = { table = table2; dto = { className = "Entity2"; spec = dtoSpec } }

let entities = [entity1; entity2]

generateDTOClassDefinitions entities commonColumns |> Console.Write |> ignore

generateTableDefinitions entities commonColumns |> Console.Write |> ignore

generateIndexDefinitions entities |> Console.Write |> ignore

generateConstraintDefinitions entities commonConstraints |> Console.Write |> ignore
