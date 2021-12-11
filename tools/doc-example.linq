<Query Kind="FSharpProgram">
  <Reference Relative="..\fsdl.test\bin\Debug\net472\fsdl.dll">C:\Src\fsdl\fsdl.test\bin\Debug\net472\fsdl.dll</Reference>
  <Reference Relative="..\fsdl.test\bin\Debug\net472\fsdl.test.dll">C:\Src\fsdl\fsdl.test\bin\Debug\net472\fsdl.test.dll</Reference>
  <NuGetReference>FSharp.Data</NuGetReference>
  <Namespace>fsdl.CodeGeneration</Namespace>
  <Namespace>fsdl.test</Namespace>
  <Namespace>fsdl.Types</Namespace>
  <Namespace>FSharp.Data</Namespace>
  <Namespace>FSharp.Data.Runtime</Namespace>
</Query>

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