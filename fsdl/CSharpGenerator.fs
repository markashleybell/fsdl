namespace fsdl

open System
open FSharp.Data.Runtime.NameUtils
open System.Text.RegularExpressions

module CSharpGenerator = 
    let indent = sprintf "    %s"
    let indent2 = indent >> indent 
    let indent3 = indent >> indent2
    let br = Environment.NewLine
    let brbr = br + br
    let scbr = sprintf ";%s" br
    
    let isMatch rx opts input =
        Regex.IsMatch (input, rx, opts)

    let isMatchCi rx input =
        isMatch rx RegexOptions.IgnoreCase input

    let replace rx (rep: string) opts input =
        Regex.Replace (input, rx, rep, opts)

    let replaceCi rx rep input =
        replace rx rep RegexOptions.IgnoreCase input

    let addNewLineIfNotEmpty s =
        match s with
        | "" -> ""
        | _ -> s + br

    let getDataType col =
        match col with
        | Null (_, dataType) -> dataType
        | NotNull (_, dataType, _) -> dataType
        | Identity (_, dataType, _, _) -> dataType

    let isNonPrimitiveType t =
        match t with
        | DATE -> true
        | GUID -> true
        | _ -> false

    let isCharType t =
        match t with
        | CHR _ -> true
        | _ -> false

    let camelName s = 
        let n = niceCamelName s
        let idSuffix = n <> "id" && n |> isMatchCi "(?<!GU)ID$"
        match idSuffix with
        | true -> (replaceCi @"ID$" "ID" n)
        | false -> n

    let attributes isPrimaryKey isExplicitKey addDapperAttributes attributeList  =
        let keyAttribute = 
            match addDapperAttributes with
            | false -> "[Key]"
            | true -> match isExplicitKey with 
                      | true -> "[d.ExplicitKey]"
                      | false -> "[d.Key]"

        let attributeStrings = 
            attributeList |> List.map (fun a -> sprintf "%s" (indent2 a))

        let attributeStrings' = 
            match isPrimaryKey with
            | true -> (indent2 keyAttribute)::attributeStrings
            | false -> attributeStrings

        attributeStrings' 
        |> String.concat br 
        |> addNewLineIfNotEmpty
    
    let nullableDataType isNullable propertyName =
        match isNullable with
        | false -> propertyName
        | true -> sprintf "%s?" propertyName

    let propertyDefinition setters addDapperAttributes isPrimaryKey column = 
        let (propertyName, isPrimaryKey', isExplicitKey, isNullable, isNonKeyIdentity, dataType) = 
            match column with
            | Null (columnName, dataType) -> 
                (columnName, false, false, true, false, dataType)
            | NotNull (columnName, dataType, _) -> 
                (columnName, (isPrimaryKey columnName), true, false, false, dataType)
            | Identity (columnName, dataType, _, _) -> 
                (columnName, (isPrimaryKey columnName), false, false, (not (isPrimaryKey columnName)), dataType)
                                   
        let (validationAttributes, cSharpDataType) = 
            match dataType with
            | INT -> ((if addDapperAttributes && isNonKeyIdentity then ["[d.Computed]"] else []), "int" |> nullableDataType isNullable)
            | BIT -> ([], "bool" |> nullableDataType isNullable)
            | MONEY -> ([], "decimal" |> nullableDataType isNullable)
            | DATE -> ([], "DateTime" |> nullableDataType isNullable)
            | CHR l -> ([sprintf "[StringLength(%i)]" l], "string")
            | TEXT -> ([], "string")
            | GUID -> ([], "Guid" |> nullableDataType isNullable)

        let setter = 
            match setters with
            | NoSetter -> ""
            | PublicSetter -> "set; "
            | PrivateSetter -> "private set; "

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
        let columns = 
            match table.dtoBaseClassName with
            | Some s -> []
            | None -> commonColumns

        columns
        |> List.append table.columnSpecifications
        |> List.map (propertyDefinition table.setters table.addDapperAttributes (isPrimaryKey table.constraintSpecifications))
        |> String.concat brbr
        |> (fun s -> sprintf "%s" s)

    let namespaces table =
        let usings = []

        let tableDataTypes = table.columnSpecifications |> List.map getDataType

        let classUsesNonPrimitiveSystemTypes = tableDataTypes |> List.exists isNonPrimitiveType
        let classUsesCharTypes = tableDataTypes |> List.exists isCharType

        let usings = 
            match classUsesNonPrimitiveSystemTypes with
            | true -> usings @ ["using System;"] 
            | false -> usings

        let usings = 
            match classUsesCharTypes with
            | true -> usings @ ["using System.ComponentModel.DataAnnotations;"] 
            | false -> usings

        let usings = 
            match table.addDapperAttributes with
            | true -> usings @ ["using d = Dapper.Contrib.Extensions;"]
            | false -> usings

        let code = [
            sprintf "namespace %s" table.dtoNamespace
            "{"
            "%s"
            "}"
            ""
        ] 

        let code = 
            match (usings.Length > 0) with
            | true -> ((usings @ [""]) |> String.concat br) :: code 
            | false -> code

        Printf.StringFormat<string->string>((code |> String.concat br))

    let constructorParam column = 
        let (propertyName, isNullable, dataType) = 
            match column with
            | Null (columnName, dataType) -> (columnName, true, dataType)
            | NotNull (columnName, dataType, _) -> (columnName, false, dataType)
            | Identity (columnName, dataType, _, _) -> (columnName, false, dataType)
                                   
        let cSharpDataType = 
            match dataType with
            | INT -> ("int" |> nullableDataType isNullable)
            | BIT -> ("bool" |> nullableDataType isNullable)
            | MONEY -> ("decimal" |> nullableDataType isNullable)
            | DATE -> ("DateTime" |> nullableDataType isNullable)
            | CHR _ -> ("string")
            | TEXT -> ("string")
            | GUID -> ("Guid" |> nullableDataType isNullable)
                      
        sprintf "%s %s" (indent3 cSharpDataType) (camelName propertyName)

    let assignment column = 
        let propertyName = 
            match column with
            | Null (columnName, _) -> columnName
            | NotNull (columnName, _, _) -> columnName
            | Identity (columnName, _, _, _) -> columnName

        sprintf "%s = %s;" (indent3 propertyName) (camelName propertyName)

    let constructorDefinition commonColumns table = 
        let columns = 
            match table.baseConstructorParameters with
            | true -> commonColumns
            | false -> []

        let c = 
            columns
            |> List.append table.columnSpecifications
            |> List.map constructorParam
            |> String.concat (sprintf ",%s" br)
        
        let a = 
            columns
            |> List.append table.columnSpecifications
            |> List.map assignment
            |> String.concat br

        sprintf "%s %s(%s%s)%s%s%s%s%s%s%s" 
            (indent2 "public") table.dtoClassName br c br (indent2 "{") br a br (indent2 "}") br

    let classDefinition commonColumns table = 
        let classattr dap arr = 
            match dap with
            | true -> (indent (sprintf "[d.Table(\"%s\")]" table.tableName))::arr
            | false -> arr

        let basecls = 
            match table.dtoBaseClassName with
            | Some s -> sprintf " : %s" s
            | None -> ""

        let constructor = 
            match table.generateConstructor with
            | true -> sprintf "%s%s" br (constructorDefinition commonColumns table)
            | false -> ""

        let partial =
            match table.partial with
            | true -> " partial"
            | false -> ""

        let cls = indent (sprintf "public%s class %s%s%s%s%s" partial table.dtoClassName basecls br (indent "{") constructor) 

        let def = 
            [cls; (properties commonColumns table); (indent "}")] 
            |> (classattr table.addDapperAttributes)
            |> String.concat br

        (table.dtoClassName, sprintf (namespaces table) def)

    let classDefinitions tableList commonColumns =
        tableList |> List.map (classDefinition commonColumns)

    let generateDTOClassDefinitionList tables commonColumns = 
        (classDefinitions tables commonColumns)

    let generateDTOClassDefinitions tables commonColumns = 
        generateDTOClassDefinitionList tables commonColumns 
        |> List.map (fun (_, def) -> sprintf "%s" def) 
        |> String.concat brbr
