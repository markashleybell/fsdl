namespace fsdl

open System

module internal CSharpGenerator = 
    let indent = sprintf "    %s"
    let indent2 = indent >> indent 
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

    let propertyDefinition addDapperAttributes isPrimaryKey column = 
        let (propertyName, isPrimaryKey', isExplicitKey, isNullable, dataType) = 
            match column with
            | Null (columnName, dataType) -> (columnName, false, false, true, dataType)
            | NotNull (columnName, dataType, d) -> (columnName, (isPrimaryKey columnName), true, false, dataType)
            | Identity (columnName, dataType, initialValue, increment) -> (columnName, (isPrimaryKey columnName), false, false, dataType)
                                   
        let (validationAttributes, cSharpDataType) = match dataType with
                                                     | INT -> ([], "int" |> nullableDataType isNullable)
                                                     | BIT -> ([], "bool" |> nullableDataType isNullable)
                                                     | MONEY -> ([], "decimal" |> nullableDataType isNullable)
                                                     | DATE -> ([], "DateTime" |> nullableDataType isNullable)
                                                     | CHR l -> ([sprintf "[StringLength(%i)]" l], "string")
                                                     | TEXT -> ([], "string")
                                                     | GUID -> ([], "Guid" |> nullableDataType isNullable)
                      
        sprintf "%s%s %s %s { get; set; }" 
                    (attributes isPrimaryKey' isExplicitKey addDapperAttributes validationAttributes) (indent2 "public") cSharpDataType propertyName
        
    let isPrimaryKey constraints columnName = 
        let primaryKeys = constraints 
                          |> List.choose (fun c -> match c with
                                                   | PrimaryKey columnName' -> Some(columnName')
                                                   | ForeignKey (columnName', fkTable, fkColumn) -> None)
        primaryKeys |> List.contains columnName

    let properties commonColumns table = 
        // If a base class is being used, don't add the
        // common columns to the generated DTO classes
        let columns = match table.dtobase with
                      | Some s -> []
                      | None -> commonColumns
        columns
        |> List.append table.cols
        |> List.map (propertyDefinition table.dapperext (isPrimaryKey table.constraints))
        |> String.concat br
        |> (fun s -> sprintf "%s" s)

    let namespaces table =
        let usings = [
            "using System;"
            "using System.ComponentModel.DataAnnotations;"
        ]
        let allusings = match table.dapperext with
                        | false -> usings
                        | true -> usings @ ["using d = Dapper.Contrib.Extensions;"]
        let code = [
            allusings |> String.concat br
            ""
            sprintf "namespace %s" table.dtonamespace
            "{"
            "%s"
            "}"
            ""
        ] 
        let tmp = code |> String.concat br
        Printf.StringFormat<string->string>(tmp)

    let classDefinition commonColumns table = 
        let classattr dap arr = 
            match dap with
                  | false -> arr
                  | true -> (indent (sprintf "[d.Table(\"%s\")]" table.name))::arr
        let basecls = match table.dtobase with
                      | Some s -> sprintf " : %s" s
                      | None -> ""
        let cls = indent (sprintf "public class %s%s%s%s" table.dtoname basecls br (indent "{"))
        let def = [cls; (properties commonColumns table); (indent "}")] 
                  |> (classattr table.dapperext)
                  |> String.concat br
        (table.dtoname, sprintf (namespaces table) def)

    let classDefinitions tableList commonColumns =
        tableList |> List.map (classDefinition commonColumns)

    let generateDTOClassDefinitionList tables commonColumns = 
        (classDefinitions tables commonColumns)

    let generateDTOClassDefinitions tables commonColumns = 
        generateDTOClassDefinitionList tables commonColumns 
        |> List.map (fun (name, def) -> sprintf "%s" def) 
        |> String.concat brbr