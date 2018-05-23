namespace fsdl

open System

module internal SqlGenerator = 
    let indent = sprintf "    %s" 
    let indent2 = indent >> indent 
    let br = Environment.NewLine // Shorthand for newline
    let brbr = br + br
    let commabr = sprintf ",%s" br // Shorthand for comma followed by newline

    let columnDataType t = 
        match t with
        | INT -> "INT"
        | BIT -> "BIT"
        | MONEY -> "DECIMAL(18,2)"
        | DATE -> "DATETIME"
        | CHR l -> sprintf "NVARCHAR(%i)" l
        | TEXT -> "NVARCHAR(MAX)"
        | GUID -> "UNIQUEIDENTIFIER"
    
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
    let columnDefinition table columnSpec = 
        match columnSpec with
        | Null (columnName, dataType) -> sprintf "%s NULL" (column columnName dataType)
        | NotNull (columnName, dataType, columnDefault') -> sprintf "%s NOT NULL%s" (column columnName dataType) (columnDefault table.tableName columnName columnDefault')
        | Identity (columnName, dataType, initialValue, increment) -> sprintf "%s IDENTITY(%i,%i) NOT NULL" (column columnName dataType) initialValue increment
    
    // All column definitions for a table
    let columnDefinitions columnSpecifications table = 
        columnSpecifications
        |> List.append table.columnSpecifications
        |> List.map (columnDefinition table)
        |> String.concat commabr
        |> (fun s -> match table.sqlStatementType with 
                     | CREATE -> sprintf "%s" s
                     | ALTER -> sprintf "%s" s)

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
    let constraintStatements commonConstraints table =
        let comment = sprintf "-- Create %s constraints" table.tableName
        
        let constraintList = commonConstraints
                             |> List.append table.constraintSpecifications
                             |> List.map (constraintStatement table.tableName)

        match constraintList with
        | [] -> ""
        | constraints -> sprintf "%s%s" (comment::constraints |> String.concat br) br

    let indexStatement tableName indexSpecification =
        let indexType, columnList = match indexSpecification with
                                    | NonClustered cols -> ("", cols)
                                    | NonClusteredUnique cols -> (" UNIQUE", cols)
                                    | Clustered cols -> (" CLUSTERED", cols)
                                    | ClusteredUnique cols -> (" UNIQUE CLUSTERED", cols)

        sprintf "CREATE%s INDEX IX_%s_%s ON [%s] (%s)" 
            indexType tableName (buildColumnList "%s" "_" columnList) tableName (buildColumnList "[%s]" ", " columnList)

    let indexStatements table = 
        let comment = sprintf "-- Create %s indexes" table.tableName

        let indexList = table.indexSpecifications
                        |> List.map (indexStatement table.tableName)

        match indexList with
        | [] -> ""
        | indexes -> sprintf "%s%s" (comment::indexes |> String.concat br) br

    // CREATE and ALTER statement generators
    let createStatement commonColumns table = 
        let comment = sprintf "-- Create %s" table.tableName
        let create = sprintf "CREATE TABLE [%s] (" table.tableName

        [comment; create; (columnDefinitions commonColumns table); ")"] 
        |> String.concat br

    let alterStatement commonColumns table = 
        let comment = sprintf "-- Alter %s" table.tableName
        let alter = sprintf "ALTER TABLE [%s] ADD" table.tableName

        [comment; alter; (columnDefinitions commonColumns table)] 
        |> String.concat br

    let tableDefinition commonColumns table =
        match table.sqlStatementType with
        | CREATE -> createStatement commonColumns table
        | ALTER -> alterStatement commonColumns table

    let tableDefinitions tableList commonColumns =
        tableList
        |> List.map (tableDefinition commonColumns)
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

    let generateTableDefinitions tableList commonColumns = 
        sprintf "%s%sGO%s%s%s" 
            (tableDefinitions tableList commonColumns) brbr brbr "PRINT 'Tables Created'" brbr

    let generateConstraintDefinitions tableList commonConstraints = 
        sprintf "%s%sGO%s%s%s" 
            (constraintDefinitions tableList commonConstraints) br brbr "PRINT 'Constraints Created'" brbr

    let generateIndexDefinitions tableList = 
        sprintf "%s%sGO%s%s%s" 
            (indexDefinitions tableList) br brbr "PRINT 'Indexes Created'" brbr
