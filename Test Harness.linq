<Query Kind="FSharpProgram">
  <Reference Relative="fsdl\bin\Debug\fsdl.dll">C:\Src\fsdl\fsdl\bin\Debug\fsdl.dll</Reference>
  <Namespace>fsdl</Namespace>
</Query>

let commoncols = [NotNull("CommonDate", DATE, NOW)
                  NotNull("CommonFKID", INT, VAL(1))]
    
let commonfks = [ForeignKey("CommonFKID", "tCommonFKTable", "ID")]

let testTable = {
        stype=CREATE
        name="tCreatedTable"
        cols = [Identity("ID", INT, 1, 1)
                Null("Name", CHR(16))
                NotNull("Index", INT, VAL(100))
                NotNull("Active", BIT, FALSE)
                Null("Price", MONEY)
                Null("Description", TEXT)
                NotNull("FKID", INT, NONE)] 
        constraints = [ASC("ID")]
        fks = [ForeignKey("FKID", "tFKTable", "ID")] 
    }
    
let testTable2 = {
        stype=CREATE
        name="tCreatedTable2"
        cols = [Identity("ID", INT, 1, 1)
                Null("Name", CHR(16))
                NotNull("Index", INT, VAL(100))
                NotNull("Active", BIT, FALSE)
                Null("Price", MONEY)
                Null("Description", TEXT)
                NotNull("FKID", INT, NONE)] 
        constraints = [ASC("ID")]
        fks = [ForeignKey("FKID", "tFKTable", "ID")] 
    }

module CSharpGenerator = 
    let indent = sprintf "    %s"
    let br = Environment.NewLine // Shorthand for newline
    let brbr = br + br
    let scbr = sprintf ";%s" br // Shorthand for semicolon followed by newline
    
    let attrs isID alist =
        let astrs = alist |> List.map (fun a -> sprintf "%s" (indent a))
        let astrs2 = match isID with
                     | false -> astrs
                     | true -> (indent "[Key]")::astrs
        astrs2 |> String.concat br |> (fun s -> match s with
                                                | "" -> ""
                                                | _ -> s + br)
    
    let propdef col = 
        let (name, id, nullable, dt) = match col with
                                         | Null (n, t) -> (n, false, true, t)
                                         | NotNull (n, t, d) -> (n, false, false, t)
                                         | Identity (n, t, a, b) -> (n, true, false, t)
                                   
        let (valAttrs, dataType) = match dt with
                                   | INT -> ([], "int")
                                   | BIT -> ([], "bool")
                                   | MONEY -> ([], "decimal")
                                   | DATE -> ([], "DateTime")
                                   | CHR l -> ([sprintf "[StringLength(%i)]" l], "string")
                                   | TEXT -> ([], "string")
                      
        sprintf "%s%s %s %s { get; set; }" 
                    (attrs id valAttrs) (indent "public") dataType name
        
    let props ccols tbl = 
        ccols
        |> List.append tbl.cols
        |> List.map propdef
        |> String.concat br
        |> (fun s -> sprintf "%s" s)

    let classdef commoncols tbl = 
        let cls = sprintf "public class %s%s{" (tbl.name.Substring 1) br
        [cls; (props commoncols tbl); "}"] |> String.concat br

    let classdefs tbllist commoncols =
        tbllist
        |> List.map (classdef commoncols)
        |> String.concat brbr

    let generateDTOClasses tables commoncols = 
        sprintf "%s" (classdefs tables commoncols)
            
CSharpGenerator.generateDTOClasses [testTable; testTable2] commoncols |> Dump |> ignore