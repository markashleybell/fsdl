namespace fsdl

module CodeGeneration =
    let generateTableDefinitions = SqlGenerator.generateTableDefinitions
    let generateConstraintDefinitions = SqlGenerator.generateConstraintDefinitions
    let generateIndexDefinitions = SqlGenerator.generateIndexDefinitions
    let generateDTOClassDefinitions = CSharpGenerator.generateDTOClassDefinitions
