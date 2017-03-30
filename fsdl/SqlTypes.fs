namespace fsdl

type StatementType = ALTER | CREATE

type DataType = INT | BIT | DATE | MONEY | TEXT | GUID | CHR of int

type Default = NONE | NULL | TRUE | FALSE | NOW | NEWGUID | VAL of int

type ColSpec = 
    | Null of string * DataType
    | NotNull of string * DataType * Default
    | Identity of string * DataType * int * int

type ConstraintSpec = 
    | PrimaryKey of string
    | ForeignKey of string * string * string

type IndexSpec = 
    | ClusteredIndex of string
    | ClusteredUniqueIndex of string
    | NonClusteredIndex of string
    | NonClusteredUniqueIndex of string

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
}