namespace fsdl

open System

module fsdl = 
    let generateTableDefinitions = SqlGenerator.generateTableDefinitions
    let generateKeyDefinitions = SqlGenerator.generateKeyDefinitions
    let generateTableAndKeyDefinitions = SqlGenerator.generateTableAndKeyDefinitions
    let generateDTOClassDefinitions = CSharpGenerator.generateDTOClassDefinitions
    let generateDTOClassDefinitionList = CSharpGenerator.generateDTOClassDefinitionList