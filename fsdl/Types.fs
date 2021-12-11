namespace fsdl

open System

module Types =
    type DataType = INT | BIGINT | BIT | DATE | MONEY | TEXT | GUID | CHR of int | ENUM of Type

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

    type PropertySetters = PublicSetters | PrivateSetters | NoSetters

    type AccessModifier = Public | Private | Internal

    type TableSpec = {
        name: string
        columns: ColSpec list
        constraints: ConstraintSpec list
        indexes: IndexSpec list
    }

    type DtoSpec = {
        ns: string
        inheritFrom: string option
        interfaces: string list option
        accessModifier: AccessModifier
        partial: bool
        constructor: bool
        setters: PropertySetters
        dapperAttributes: bool
    }

    type Dto = {
        name: string
        spec: DtoSpec
    }

    type Entity = {
        table: TableSpec
        dto: Dto
    }
