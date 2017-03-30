namespace fsdl

type StatementType = ALTER | CREATE

type ColType = INT | BIT | DATE | MONEY | TEXT | GUID | CHR of int

type Default = NONE | NULL | TRUE | FALSE | NOW | NEWGUID | VAL of int

type ColSpec = 
    | Null of string * ColType
    | NotNull of string * ColType * Default
    | Identity of string * ColType * int * int

type ConstraintSpec = 
    | ASC of string
    | DESC of string

type FKSpec = ForeignKey of string * string * string

type Table = {
    name:string; 
    dtoname:string; 
    dtonamespace:string;
    dtobase:string option;
    stype:StatementType; 
    cols:ColSpec list; 
    constraints:ConstraintSpec list; 
    fks:FKSpec list;
    dapperext:bool;
}