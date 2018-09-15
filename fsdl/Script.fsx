#r @"bin\Debug\net462\fsdl.dll"
#r @"C:\Users\Mark Bell\.nuget\packages\fsharp.data\2.4.6\lib\net45\FSharp.Data.dll"

open fsdl.CSharpGenerator
open FSharp.Data.Runtime.NameUtils

let a = camelName "TestID"
let b = camelName "TestIDTest"
let c = camelName "TESTID"
let d = camelName "TestGUID"
let e = camelName "ID"
let f = camelName "GUID"
let g = camelName "CommonFKID"
let h = camelName "FKID"

let tmp = niceCamelName "ID"

let n = niceCamelName "ID"
let idSuffix = (not (n = "id")) && n |> isMatchCi "(?<!GU)ID$"
