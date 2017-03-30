namespace fsdl

open System

module fsdl = 
    let generateTableDefinitions = SqlGenerator.generateTableDefinitions
    let generateConstraintDefinitions = SqlGenerator.generateConstraintDefinitions
    let generateTableAndConstraintDefinitions = SqlGenerator.generateTableAndConstraintDefinitions
    let generateDTOClassDefinitions = CSharpGenerator.generateDTOClassDefinitions
    let generateDTOClassDefinitionList = CSharpGenerator.generateDTOClassDefinitionList