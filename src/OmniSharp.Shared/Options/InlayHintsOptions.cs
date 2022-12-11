namespace OmniSharp.Options;

public record struct InlayHintsOptions(
    bool EnableForParameters,
    bool ForLiteralParameters,
    bool ForIndexerParameters,
    bool ForObjectCreationParameters,
    bool ForOtherParameters,
    bool SuppressForParametersThatDifferOnlyBySuffix,
    bool SuppressForParametersThatMatchMethodIntent,
    bool SuppressForParametersThatMatchArgumentName,
    bool EnableForTypes,
    bool ForImplicitVariableTypes,
    bool ForLambdaParameterTypes,
    bool ForImplicitObjectCreation)
{
    public static readonly InlayHintsOptions AllOn = new(
        EnableForParameters: true,
        ForLiteralParameters: true,
        ForIndexerParameters: true,
        ForObjectCreationParameters: true,
        ForOtherParameters: true,
        SuppressForParametersThatDifferOnlyBySuffix: true,
        SuppressForParametersThatMatchMethodIntent: true,
        SuppressForParametersThatMatchArgumentName: true,
        EnableForTypes: true,
        ForImplicitVariableTypes: true,
        ForLambdaParameterTypes: true,
        ForImplicitObjectCreation: true
    );
}
