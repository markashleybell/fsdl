namespace fsdl

open System
open Utils

module internal CSharpGenerator = 
    let indent = sprintf "    %s"
    let indent2 = indent >> indent 
    let indent3 = indent >> indent2
    let br = Environment.NewLine // Shorthand for newline
    let brbr = br + br
    let scbr = sprintf ";%s" br // Shorthand for semicolon followed by newline
    
    let attributes isPrimaryKey isExplicitKey addDapperAttributes attributeList  =
        let keyAttribute = match addDapperAttributes with
                           | false -> "[Key]"
                           | true -> match isExplicitKey with 
                                     | false -> "[d.Key]"
                                     | true -> "[d.ExplicitKey]"
        let attributeStrings = attributeList 
                               |> List.map (fun a -> sprintf "%s" (indent2 a))
        let attributeStrings' = match isPrimaryKey with
                                | false -> attributeStrings
                                | true -> (indent2 keyAttribute)::attributeStrings
        attributeStrings' |> String.concat br |> (fun s -> match s with
                                                           | "" -> ""
                                                           | _ -> s + br)
    
    let nullableDataType isNullable propertyName =
        match isNullable with
        | false -> propertyName
        | true -> sprintf "%s?" propertyName

    let propertyDefinition immutable addDapperAttributes isPrimaryKey column = 
        let (propertyName, isPrimaryKey', isExplicitKey, isNullable, isNonKeyIdentity, dataType) = 
            match column with
            | Null (columnName, dataType) -> (columnName, false, false, true, false, dataType)
            | NotNull (columnName, dataType, d) -> (columnName, (isPrimaryKey columnName), true, false, false, dataType)
            | Identity (columnName, dataType, initialValue, increment) -> (columnName, (isPrimaryKey columnName), false, false, (not (isPrimaryKey columnName)), dataType)
                                   
        let (validationAttributes, cSharpDataType) = match dataType with
                                                     | INT -> ((if addDapperAttributes && isNonKeyIdentity then ["[d.Computed]"] else []), "int" |> nullableDataType isNullable)
                                                     | BIT -> ([], "bool" |> nullableDataType isNullable)
                                                     | MONEY -> ([], "decimal" |> nullableDataType isNullable)
                                                     | DATE -> ([], "DateTime" |> nullableDataType isNullable)
                                                     | CHR l -> ([sprintf "[StringLength(%i)]" l], "string")
                                                     | TEXT -> ([], "string")
                                                     | GUID -> ([], "Guid" |> nullableDataType isNullable)
        let setter = match immutable with
                     | true -> ""
                     | false -> "set; "

        sprintf "%s%s %s %s { get; %s}" 
                    (attributes isPrimaryKey' isExplicitKey addDapperAttributes validationAttributes) (indent2 "public") cSharpDataType propertyName setter
        
    let isPrimaryKey constraints columnName = 
        let primaryKeys = constraints 
                          |> List.map (fun c -> match c with
                                                | PrimaryKey columnList -> columnList |> List.contains columnName
                                                | ForeignKey _ -> false)
        primaryKeys |> List.contains true

    let properties commonColumns table = 
        // If a base class is being used, don't add the
        // common columns to the generated DTO classes
        let columns = match table.dtoBaseClassName with
                      | Some s -> []
                      | None -> commonColumns
        columns
        |> List.append table.columnSpecifications
        |> List.map (propertyDefinition table.immutable table.addDapperAttributes (isPrimaryKey table.constraintSpecifications))
        |> String.concat br
        |> (fun s -> sprintf "%s" s)

    let namespaces table =
        let usings = [
            "using System;"
            "using System.ComponentModel.DataAnnotations;"
        ]
        let allusings = match table.addDapperAttributes with
                        | false -> usings
                        | true -> usings @ ["using d = Dapper.Contrib.Extensions;"]
        let code = [
            allusings |> String.concat br
            ""
            sprintf "namespace %s" table.dtoNamespace
            "{"
            "%s"
            "}"
            ""
        ] 
        let tmp = code |> String.concat br
        Printf.StringFormat<string->string>(tmp)

    let constructorParam column = 
        let (propertyName, isNullable, dataType) = 
            match column with
            | Null (columnName, dataType) -> (columnName, true, dataType)
            | NotNull (columnName, dataType, d) -> (columnName, false, dataType)
            | Identity (columnName, dataType, initialValue, increment) -> (columnName, false, dataType)
                                   
        let cSharpDataType = match dataType with
                             | INT -> ("int" |> nullableDataType isNullable)
                             | BIT -> ("bool" |> nullableDataType isNullable)
                             | MONEY -> ("decimal" |> nullableDataType isNullable)
                             | DATE -> ("DateTime" |> nullableDataType isNullable)
                             | CHR l -> ("string")
                             | TEXT -> ("string")
                             | GUID -> ("Guid" |> nullableDataType isNullable)
                      
        sprintf "%s %s" cSharpDataType (niceCamelName propertyName)

    let assignment column = 
        let propertyName = 
            match column with
            | Null (columnName, _) -> columnName
            | NotNull (columnName, dataType, d) -> columnName
            | Identity (columnName, dataType, initialValue, increment) -> columnName
        sprintf "%s = %s;" (indent3 propertyName) (niceCamelName propertyName)

    let constructorDefinition commonColumns table = 
        // If a base class is being used, don't add the
        // common columns to the generated DTO classes
        let columns = match table.dtoBaseClassName with
                      | Some s -> []
                      | None -> commonColumns

        let c = columns
                |> List.append table.columnSpecifications
                |> List.map constructorParam
                |> String.concat ", "
        
        let a = columns
                |> List.append table.columnSpecifications
                |> List.map assignment
                |> String.concat br

        sprintf "%s %s(%s)%s%s%s%s%s%s%s" (indent2 "public") table.dtoClassName c br (indent2 "{") br a br (indent2 "}") br

    let classDefinition commonColumns table = 
        let classattr dap arr = 
            match dap with
                  | false -> arr
                  | true -> (indent (sprintf "[d.Table(\"%s\")]" table.tableName))::arr
        let basecls = match table.dtoBaseClassName with
                      | Some s -> sprintf " : %s" s
                      | None -> ""
        let constructor = match table.immutable with
                          | false -> ""
                          | true -> sprintf "%s%s" br (constructorDefinition commonColumns table)
        let cls = indent (sprintf "public class %s%s%s%s%s" table.dtoClassName basecls br (indent "{") constructor) 
        let def = [cls; (properties commonColumns table); (indent "}")] 
                  |> (classattr table.addDapperAttributes)
                  |> String.concat br
        (table.dtoClassName, sprintf (namespaces table) def)

    let classDefinitions tableList commonColumns =
        tableList |> List.map (classDefinition commonColumns)

    let generateDTOClassDefinitionList tables commonColumns = 
        (classDefinitions tables commonColumns)

    let generateDTOClassDefinitions tables commonColumns = 
        generateDTOClassDefinitionList tables commonColumns 
        |> List.map (fun (name, def) -> sprintf "%s" def) 
        |> String.concat brbr