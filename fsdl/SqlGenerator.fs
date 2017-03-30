namespace fsdl

open System

module internal SqlGenerator = 
    let indent = sprintf "    %s" 
    let indent2 = indent >> indent 
    let br = Environment.NewLine // Shorthand for newline
    let brbr = br + br
    let commabr = sprintf ",%s" br // Shorthand for comma followed by newline

    let coltype t = 
        match t with
        | INT -> "INT"
        | BIT -> "BIT"
        | MONEY -> "DECIMAL(18,2)"
        | DATE -> "DATETIME"
        | CHR l -> sprintf "NVARCHAR(%i)" l
        | TEXT -> "NVARCHAR(MAX)"
        | GUID -> "UNIQUEIDENTIFIER"
    
    let col n t = sprintf "%s %s" (indent (sprintf "[%s]" n)) (coltype t)

    let def tblname n d =
        let cn = sprintf " CONSTRAINT DF_%s_%s DEFAULT %s" tblname n 
        match d with
        | NONE -> ""
        | NULL -> cn "NULL"
        | TRUE -> cn "1"
        | FALSE -> cn "0"
        | NOW -> cn "GETDATE()"
        | NEWGUID -> cn "NEWID()"
        | VAL i -> cn (sprintf "%i" i)

    // Column definition statement
    let coldef tbl c = 
        match c with
        | Null (n, t) -> sprintf "%s NULL" (col n t)
        | NotNull (n, t, d) -> sprintf "%s NOT NULL%s" (col n t) (def tbl.name n d)
        | Identity (n, t, a, b) -> sprintf "%s IDENTITY(%i,%i) NOT NULL" (col n t) a b
    
    // All column definitions for a table
    let cols ccols tbl = 
        ccols
        |> List.append tbl.cols
        |> List.map (coldef tbl)
        |> String.concat commabr
        |> (fun s -> match tbl.stype with 
                     | CREATE -> sprintf "%s," s
                     | ALTER -> sprintf "%s" s)

    // PK constraint field
    let cnstr c = 
        let cf col dir = indent2 (sprintf "[%s] %s" col dir)
        match c with 
        | ASC s -> cf s "ASC"
        | DESC s -> cf s "DESC"

    // List of PK constraint fields
    let cnstrs cnstrlist = 
        cnstrlist
        |> List.map cnstr 
        |> String.concat commabr

    // FK constraint statement
    let fk tblname fk =
        let fkd = sprintf "ALTER TABLE [%s] WITH CHECK ADD CONSTRAINT FK_%s_%s_%s%sFOREIGN KEY ([%s]) REFERENCES [%s] ([%s])" 
        match fk with
        | ForeignKey (c, kt, kc) -> fkd tblname tblname kt c br c kt kc

    // List of foreign key statements for a table
    let fks cfks tbl =
        let com = sprintf "-- Create %s foreign keys" tbl.name
        let fklist = cfks
                     |> List.append tbl.fks
                     |> List.map (fk tbl.name)

        match fklist with
        | [] -> ""
        | lst -> sprintf "%s%s" (com::lst |> String.concat br) br

    // CREATE and ALTER statement generators
    let create commoncols tbl = 
        let com = sprintf "-- Create %s" tbl.name
        let create = sprintf "CREATE TABLE [%s] (" tbl.name
        let constr = sprintf "    CONSTRAINT PK_%s PRIMARY KEY CLUSTERED (" tbl.name
        let wth = sprintf "    )%s)" br

        [com; create; (cols commoncols tbl); constr; (cnstrs tbl.constraints); wth] 
        |> String.concat br

    let alter commoncols tbl = 
        let com = sprintf "-- Alter %s" tbl.name
        let alter = sprintf "ALTER TABLE [%s] ADD" tbl.name

        [com; alter; (cols commoncols tbl)] 
        |> String.concat br

    let tabledef commoncols tbl =
        match tbl.stype with
        | CREATE -> create commoncols tbl
        | ALTER -> alter commoncols tbl

    let tabledefs tbllist commoncols =
        tbllist
        |> List.map (tabledef commoncols)
        |> String.concat brbr

    let fkdefs tbllist commonfks =
        tbllist
        |> List.map (fks commonfks)
        |> List.filter (fun s -> s <> "")
        |> String.concat br

    let generateTableDefinitions tables commoncols = 
        sprintf "%s%sGO%s%s%s" 
            (tabledefs tables commoncols) brbr brbr "PRINT 'Tables Created'" brbr

    let generateKeyDefinitions tables commonfks = 
        sprintf "%s%sGO%s%s%s" 
            (fkdefs tables commonfks) br brbr "PRINT 'Foreign Keys Created'" brbr

    let generateTableAndKeyDefinitions tables commoncols commonfks = 
        sprintf "%s%s" 
            (generateTableDefinitions tables commoncols) 
            (generateKeyDefinitions tables commonfks)
