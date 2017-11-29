namespace fsdl

type StatementType = ALTER | CREATE

type DataType = INT | BIT | DATE | MONEY | TEXT | GUID | CHR of int

type Default = NONE | NULL | TRUE | FALSE | NOW | NEWGUID | VAL of int

type ColSpec = 
    | Null of string * DataType
    | NotNull of string * DataType * Default
    | Identity of string * DataType * int * int

type ConstraintSpec = 
    | PrimaryKey of string list
    | ForeignKey of string * string * string

type IndexSpec = 
    | Clustered of string list
    | ClusteredUnique of string list
    | NonClustered of string list
    | NonClusteredUnique of string list

type Table = {
    tableName:string; 
    dtoClassName:string; 
    dtoNamespace:string;
    dtoBaseClassName:string option;
    sqlStatementType:StatementType; 
    columnSpecifications:ColSpec list; 
    constraintSpecifications:ConstraintSpec list; 
    indexSpecifications:IndexSpec list;
    addDapperAttributes:bool;
    generateConstructor:bool;
    immutable:bool;
}