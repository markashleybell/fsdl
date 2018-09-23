namespace fsdl

open System
open Types

module SqlGenerator = 
    let indent = sprintf "    %s" 
    let indent2 = indent >> indent 
    let br = Environment.NewLine // Shorthand for newline
    let brbr = br + br
    let commabr = sprintf ",%s" br // Shorthand for comma followed by newline

    let ifNotEmptyAppend suf s =
        match s with
        | "" -> ""
        | s' -> sprintf "%s%s" s' suf

    let columnDataType t = 
        match t with
        | INT -> "INT"
        | BIT -> "BIT"
        | MONEY -> "DECIMAL(18,2)"
        | DATE -> "DATETIME"
        | CHR l -> sprintf "NVARCHAR(%i)" l
        | TEXT -> "NVARCHAR(MAX)"
        | GUID -> "UNIQUEIDENTIFIER"
        | ENUM _ -> "INT"
    
    let column columnName dataType = 
        sprintf "%s %s" (indent (sprintf "[%s]" columnName)) (columnDataType dataType)

    let columnDefault tableName columnName defaultValue =
        let cn = sprintf " CONSTRAINT DF_%s_%s DEFAULT %s" tableName columnName
        match defaultValue with
        | NONE -> ""
        | NULL -> cn "NULL"
        | TRUE -> cn "1"
        | FALSE -> cn "0"
        | NOW -> cn "GETDATE()"
        | NEWGUID -> cn "NEWID()"
        | VAL i -> cn (sprintf "%i" i)

    // Column definition statement
    let columnDefinition tableName columnSpec = 
        match columnSpec with
        | Null (columnName, dataType) -> 
            sprintf "%s NULL" (column columnName dataType)
        | NotNull (columnName, dataType, columnDefault') -> 
            sprintf "%s NOT NULL%s" (column columnName dataType) (columnDefault tableName columnName columnDefault')
        | Identity (columnName, dataType, initialValue, increment) -> 
            sprintf "%s IDENTITY(%i,%i) NOT NULL" (column columnName dataType) initialValue increment
    
    // All column definitions for a table
    let columnDefinitions columnSpecifications table = 
        columnSpecifications
        |> List.append table.columns
        |> List.map (columnDefinition table.name)
        |> String.concat commabr
        |> sprintf "%s"

    let buildColumnList format separator cols = 
        cols 
        |> List.map (fun col -> sprintf (Printf.StringFormat<string->string>(format)) col) 
        |> String.concat separator

    // Constraint statement
    let constraintStatement tableName constraintSpecification = 
        let primaryKeyDefinition = sprintf "ALTER TABLE [%s] WITH CHECK ADD CONSTRAINT PK_%s%sPRIMARY KEY (%s)" 
        let foreignKeyDefinition = sprintf "ALTER TABLE [%s] WITH CHECK ADD CONSTRAINT FK_%s_%s_%s_%s%sFOREIGN KEY ([%s]) REFERENCES [%s] ([%s])" 
        
        match constraintSpecification with 
        | PrimaryKey columnList -> (primaryKeyDefinition tableName tableName br (buildColumnList "[%s]" ", " columnList))
        | ForeignKey (columnName', fkTable, fkColumn) -> (foreignKeyDefinition tableName tableName columnName' fkTable fkColumn br columnName' fkTable fkColumn)

    // List of constraint statements for a table
    let constraintStatements commonConstraints (table: TableSpec) =
        let comment = sprintf "-- Create %s constraints" table.name
        
        let constraintList = 
            commonConstraints
            |> List.append table.constraints
            |> List.map (constraintStatement table.name)

        match constraintList with
        | [] -> ""
        | constraints -> sprintf "%s%s%s%s" comment brbr (constraints |> String.concat brbr) br

    let indexStatement tableName indexSpecification =
        let indexType, columnList = 
            match indexSpecification with
            | NonClustered cols -> ("", cols)
            | NonClusteredUnique cols -> (" UNIQUE", cols)
            | Clustered cols -> (" CLUSTERED", cols)
            | ClusteredUnique cols -> (" UNIQUE CLUSTERED", cols)

        sprintf "CREATE%s INDEX IX_%s_%s ON [%s] (%s)" 
            indexType tableName (buildColumnList "%s" "_" columnList) tableName (buildColumnList "[%s]" ", " columnList)

    let indexStatements (table: TableSpec) = 
        let comment = sprintf "-- Create %s indexes" table.name

        let indexList = 
            table.indexes
            |> List.map (indexStatement table.name)

        match indexList with
        | [] -> ""
        | indexes -> sprintf "%s%s%s%s" comment brbr (indexes |> String.concat brbr) br

    let createStatement commonColumns (table: TableSpec) = 
        let comment = sprintf "-- Create %s" table.name
        let create = sprintf "CREATE TABLE [%s] (" table.name

        [comment; create; (columnDefinitions commonColumns table); ")"] 
        |> String.concat br

    let tableDefinitions tableList commonColumns =
        tableList
        |> List.map (createStatement commonColumns)
        |> String.concat brbr

    let constraintDefinitions tableList commonConstraints =
        tableList
        |> List.map (constraintStatements commonConstraints)
        |> List.filter (fun s -> s <> "")
        |> String.concat br

    let indexDefinitions tableList =
        tableList
        |> List.map indexStatements
        |> List.filter (fun s -> s <> "")
        |> String.concat br

    let generateTableDefinitions entityList commonColumns = 
        (tableDefinitions (entityList |> List.map (fun e -> e.table)) commonColumns) 
        |> ifNotEmptyAppend (sprintf "%sGO%s%s%s" brbr brbr "PRINT 'Tables Created'" brbr)

    let generateConstraintDefinitions entityList commonConstraints = 
        (constraintDefinitions (entityList |> List.map (fun e -> e.table)) commonConstraints) 
        |> ifNotEmptyAppend (sprintf "%sGO%s%s%s" br brbr "PRINT 'Constraints Created'" brbr)

    let generateIndexDefinitions entityList = 
        (indexDefinitions (entityList |> List.map (fun e -> e.table)))
        |> ifNotEmptyAppend (sprintf "%sGO%s%s%s" br brbr "PRINT 'Indexes Created'" brbr)
