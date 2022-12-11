namespace OmniSharp;

internal static class Configuration
{
    public static bool UseZeroBasedIndices;

    public const string RoslynVersion = "4.4.0.0";
    public const string RoslynPublicKeyToken = "31bf3856ad364e35";
    private const string MicrosoftCodeAnalysis = "Microsoft.CodeAnalysis.";
    public const string RoslynFeatures = MicrosoftCodeAnalysis + "Features" + FullNameSuffix;
    public const string RoslynCSharpFeatures = MicrosoftCodeAnalysis + "CSharp.Features" + FullNameSuffix;
    public const string RoslynOmniSharpExternalAccess = MicrosoftCodeAnalysis + "ExternalAccess.OmniSharp" + FullNameSuffix;
    public const string RoslynOmniSharpExternalAccessCSharp = MicrosoftCodeAnalysis + "ExternalAccess.OmniSharp.CSharp" + FullNameSuffix;
    public const string RoslynWorkspaces = MicrosoftCodeAnalysis + "Workspaces" + FullNameSuffix;
    public const string OmniSharpMiscProjectName = "OmniSharpMiscellaneousFiles";
    private const string FullNameSuffix = $", Version={RoslynVersion}, Culture=neutral, PublicKeyToken={RoslynPublicKeyToken}";
}
