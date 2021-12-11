namespace fsdl

open System
open FSharp.Data.Runtime.NameUtils
open System.Text.RegularExpressions
open Types

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

    let specs entity =
        (entity.table, entity.dto.spec)

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
        | true -> (replaceCi @"ID$" "Id" n)
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
            | BIGINT -> ((if addDapperAttributes && isNonKeyIdentity then ["[d.Computed]"] else []), "long" |> nullableDataType isNullable)
            | BIT -> ([], "bool" |> nullableDataType isNullable)
            | MONEY -> ([], "decimal" |> nullableDataType isNullable)
            | DATE -> ([], "DateTime" |> nullableDataType isNullable)
            | CHR l -> ([sprintf "[StringLength(%i)]" l], "string")
            | TEXT -> ([], "string")
            | GUID -> ([], "Guid" |> nullableDataType isNullable)
            | ENUM t -> ([], t.Name |> nullableDataType isNullable)

        let setter =
            match setters with
            | NoSetters -> ""
            | PublicSetters -> "set; "
            | PrivateSetters -> "private set; "

        sprintf "%s%s %s %s { get; %s}"
            (attributes isPrimaryKey' isExplicitKey addDapperAttributes validationAttributes) (indent2 "public") cSharpDataType propertyName setter

    let isPrimaryKey constraints columnName =
        let primaryKeys = constraints
                          |> List.map (fun c -> match c with
                                                | PrimaryKey columnList -> columnList |> List.contains columnName
                                                | ForeignKey _ -> false)

        primaryKeys |> List.contains true

    let properties commonColumns entity =
        let (tbl, dto) = specs entity
        // If a base class is being used, don't add the
        // common columns to the generated DTO classes
        let columns =
            match dto.inheritFrom with
            | Some s -> []
            | None -> commonColumns

        columns
        |> List.append tbl.columns
        |> List.map (propertyDefinition dto.setters dto.dapperAttributes (isPrimaryKey tbl.constraints))
        |> String.concat brbr
        |> (fun s -> sprintf "%s" s)

    let namespaces entity =
        let usings = []

        let (tbl, dto) = specs entity

        let tableDataTypes = tbl.columns |> List.map getDataType

        let classUsesNonPrimitiveSystemTypes = tableDataTypes |> List.exists isNonPrimitiveType
        let classUsesCharTypes = tableDataTypes |> List.exists isCharType

        let enumTypeNameSpaces =
            tableDataTypes
            |> List.choose (fun dt -> match dt with | ENUM t -> Some t.Namespace | _ -> None)
            |> List.choose (fun ns -> match (ns <> dto.ns) with | true -> Some ns | false -> None)
            |> List.map (fun ns -> sprintf "using %s;" ns)

        let usings =
            match classUsesNonPrimitiveSystemTypes with
            | true -> usings @ ["using System;"]
            | false -> usings

        let usings =
            match classUsesCharTypes with
            | true -> usings @ ["using System.ComponentModel.DataAnnotations;"]
            | false -> usings

        let usings =
            match dto.dapperAttributes with
            | true -> usings @ ["using d = Dapper.Contrib.Extensions;"]
            | false -> usings

        let usings = usings @ enumTypeNameSpaces

        let code = [
            sprintf "namespace %s" dto.ns
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
            | BIGINT -> ("long" |> nullableDataType isNullable)
            | BIT -> ("bool" |> nullableDataType isNullable)
            | MONEY -> ("decimal" |> nullableDataType isNullable)
            | DATE -> ("DateTime" |> nullableDataType isNullable)
            | CHR _ -> ("string")
            | TEXT -> ("string")
            | GUID -> ("Guid" |> nullableDataType isNullable)
            | ENUM t -> (t.Name |> nullableDataType isNullable)

        sprintf "%s %s" (indent3 cSharpDataType) (camelName propertyName)

    let assignment column =
        let propertyName =
            match column with
            | Null (columnName, _) -> columnName
            | NotNull (columnName, _, _) -> columnName
            | Identity (columnName, _, _, _) -> columnName

        sprintf "%s = %s;" (indent3 propertyName) (camelName propertyName)

    let constructorDefinition commonColumns entity =
        let (tbl, dto) = specs entity

        let columns = commonColumns

        let c =
            columns
            |> List.append tbl.columns
            |> List.map constructorParam
            |> String.concat (sprintf ",%s" br)

        let a =
            columns
            |> List.append tbl.columns
            |> List.map assignment
            |> String.concat br

        let am =
            match dto.accessModifier with
            | Public -> "public"
            | Internal -> "internal"
            | Private -> "private"

        sprintf "%s %s(%s%s)%s%s%s%s%s%s%s"
            (indent2 am) entity.dto.name br c br (indent2 "{") br a br (indent2 "}") br

    let classDefinition commonColumns entity =
        let (tbl, dto) = specs entity

        let classattr dap arr =
            match dap with
            | true -> (indent (sprintf "[d.Table(\"%s\")]" tbl.name))::arr
            | false -> arr

        let baseClass =
            match dto.inheritFrom with
            | Some s -> [s]
            | None -> []

        let inherits =
            match dto.interfaces with
            | Some i -> baseClass @ i
            | None -> baseClass

        let inheritFrom =
            match inherits with
            | [] -> ""
            | ih -> sprintf " : %s" (ih |> String.concat ", ")

        let constructor =
            match dto.constructor with
            | true -> sprintf "%s%s" br (constructorDefinition commonColumns entity)
            | false -> ""

        let partial =
            match dto.partial with
            | true -> " partial"
            | false -> ""

        let cls = indent (sprintf "public%s class %s%s%s%s%s" partial entity.dto.name inheritFrom br (indent "{") constructor)

        let def =
            [cls; (properties commonColumns entity); (indent "}")]
            |> (classattr dto.dapperAttributes)
            |> String.concat br

        (entity.dto.name, sprintf (namespaces entity) def)

    let classDefinitions entityList commonColumns =
        entityList |> List.map (classDefinition commonColumns)

    let generateDTOClassDefinitions entities commonColumns =
        (classDefinitions entities commonColumns)

