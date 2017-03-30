namespace fsdl

open System

module internal CSharpGenerator = 
    let indent = sprintf "    %s"
    let indent2 = indent >> indent 
    let br = Environment.NewLine // Shorthand for newline
    let brbr = br + br
    let scbr = sprintf ";%s" br // Shorthand for semicolon followed by newline
    
    let attrs isID explicit dap alist  =
        let keyattr = match dap with
                      | false -> "[Key]"
                      | true -> match explicit with 
                                | false -> "[d.Key]"
                                | true -> "[d.ExplicitKey]"
        let astrs = alist |> List.map (fun a -> sprintf "%s" (indent2 a))
        let astrs2 = match isID with
                     | false -> astrs
                     | true -> (indent2 keyattr)::astrs
        astrs2 |> String.concat br |> (fun s -> match s with
                                                | "" -> ""
                                                | _ -> s + br)
    
    let nprop nullable prop =
        match nullable with
        | false -> prop
        | true -> sprintf "%s?" prop

    let propdef dap isKey col = 
        let (name, key, explicit, nullable, dt) = match col with
                                                  | Null (n, t) -> (n, false, false, true, t)
                                                  | NotNull (n, t, d) -> (n, (isKey n), true, false, t)
                                                  | Identity (n, t, a, b) -> (n, (isKey n), false, false, t)
                                   
        let (valAttrs, dataType) = match dt with
                                   | INT -> ([], "int" |> nprop nullable)
                                   | BIT -> ([], "bool" |> nprop nullable)
                                   | MONEY -> ([], "decimal" |> nprop nullable)
                                   | DATE -> ([], "DateTime" |> nprop nullable)
                                   | CHR l -> ([sprintf "[StringLength(%i)]" l], "string")
                                   | TEXT -> ([], "string")
                                   | GUID -> ([], "Guid" |> nprop nullable)
                      
        sprintf "%s%s %s %s { get; set; }" 
                    (attrs key explicit dap valAttrs) (indent2 "public") dataType name
        
    let isKey constraints colname = 
        let keys = constraints |> List.map (fun c -> match c with
                                                     | ASC col -> col
                                                     | DESC col -> col)
        keys |> List.contains colname

    let props ccols tbl = 
        // If a base class is being used, don't add the
        // common columns to the generated DTO classes
        let ccolumns = match tbl.dtobase with
                       | Some s -> []
                       | None -> ccols
        ccolumns
        |> List.append tbl.cols
        |> List.map (propdef tbl.dapperext (isKey tbl.constraints))
        |> String.concat br
        |> (fun s -> sprintf "%s" s)

    let ns tbl =
        let usings = [
            "using System;"
            "using System.ComponentModel.DataAnnotations;"
        ]
        let allusings = match tbl.dapperext with
                        | false -> usings
                        | true -> usings @ ["using d = Dapper.Contrib.Extensions;"]
        let code = [
            allusings |> String.concat br
            ""
            sprintf "namespace %s" tbl.dtonamespace
            "{"
            "%s"
            "}"
        ] 
        let tmp = code |> String.concat br
        Printf.StringFormat<string->string>(tmp)

    let classdef commoncols tbl = 
        let classattr dap arr = 
            match dap with
                  | false -> arr
                  | true -> (indent (sprintf "[d.Table(\"%s\")]" tbl.name))::arr
        let basecls = match tbl.dtobase with
                      | Some s -> sprintf " : %s" s
                      | None -> ""
        let cls = indent (sprintf "public class %s%s%s%s" tbl.dtoname basecls br (indent "{"))
        let def = [cls; (props commoncols tbl); (indent "}")] 
                  |> (classattr tbl.dapperext)
                  |> String.concat br
        let nmspc = ns tbl
        (tbl.dtoname, sprintf nmspc def)

    let classdefs tbllist commoncols =
        tbllist |> List.map (classdef commoncols)

    let generateDTOClassDefinitionList tables commoncols = 
        (classdefs tables commoncols)

    let generateDTOClassDefinitions tables commoncols = 
        generateDTOClassDefinitionList tables commoncols 
        |> List.map (fun (name, def) -> sprintf "%s" def) 
        |> String.concat brbr