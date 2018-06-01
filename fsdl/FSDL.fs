namespace fsdl

module fsdl = 
    let generateTableDefinitions = SqlGenerator.generateTableDefinitions
    let generateConstraintDefinitions = SqlGenerator.generateConstraintDefinitions
    let generateIndexDefinitions = SqlGenerator.generateIndexDefinitions
    let generateDTOClassDefinitions = CSharpGenerator.generateDTOClassDefinitions
    let generateDTOClassDefinitionList = CSharpGenerator.generateDTOClassDefinitionList
