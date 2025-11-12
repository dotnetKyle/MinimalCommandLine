namespace MinimalCli.SourceGeneration;

internal record FromServicesBinding(string OriginalParameterName, string Type)
    : ParameterBinding(OriginalParameterName, Type, DefaultValueConstant:null, IsCollectionType:false)
{ 
}
