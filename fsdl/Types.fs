namespace fsdl

open System

module Types =
    type DataType = INT | BIT | DATE | MONEY | TEXT | GUID | CHR of int | ENUM of Type

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
        tableName: string
        columnSpecifications: ColSpec list
        constraintSpecifications: ConstraintSpec list
        indexSpecifications: IndexSpec list
    }

    type DtoSpec = {
        dtoNamespace: string
        baseClassName: string option
        accessModifier: AccessModifier
        partial: bool
        generateConstructor: bool
        baseConstructorParameters: bool
        setters: PropertySetters
        addDapperAttributes: bool
    }

    type Dto = {
        className: string
        spec: DtoSpec
    }

    type Entity = {
        table: TableSpec
        dto: Dto
    }
