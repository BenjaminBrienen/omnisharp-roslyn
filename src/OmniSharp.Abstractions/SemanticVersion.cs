// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Original source from https://github.com/PowerShell/PowerShell/blob/f8d6b2f9fefe8467061f04986644aa47c2d10038/src/System.Management.Automation/engine/PSVersionInfo.cs
// Modified to remove PSObj and PSTraceSource use. Includes parsing fix from https://github.com/PowerShell/PowerShell/pull/16608

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace OmniSharp;

/// <summary>
/// An implementation of semantic versioning (https://semver.org)
/// </summary>
public sealed record SemanticVersion(uint Major, uint Minor = 0, uint Patch = 0) : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    private const string VersionSansRegEx = @"^(?<major>(0|[1-9]\d*))\.(?<minor>(0|[1-9]\d*))\.(?<patch>(0|[1-9]\d*))$";
    private const string LabelRegEx = @"^((?<preLabel>[0-9A-Za-z][0-9A-Za-z\-\.]*))?(\+(?<buildLabel>[0-9A-Za-z][0-9A-Za-z\-\.]*))?$";
    private const string LabelUnitRegEx = @"^[0-9A-Za-z][0-9A-Za-z\-\.]*$";

    public string? PreReleaseLabel { get; init; }
    public string? BuildLabel { get; init; }

    /// <summary>
    /// Construct a SemanticVersion.
    /// </summary>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version.</param>
    /// <param name="patch">The patch version.</param>
    /// <param name="preReleaseLabel">The pre-release label for the version.</param>
    /// <param name="buildLabel">The build metadata for the version.</param>
    /// <exception cref="FormatException">
    /// If <paramref name="preReleaseLabel"/> don't match 'LabelUnitRegEx'.
    /// If <paramref name="buildLabel"/> don't match 'LabelUnitRegEx'.
    /// </exception>
    public SemanticVersion(uint major, uint minor, uint patch, string? preReleaseLabel, string? buildLabel)
        : this(major, minor, patch)
    {
        if (!string.IsNullOrEmpty(preReleaseLabel))
        {
            PreReleaseLabel = Regex.Match(preReleaseLabel, LabelUnitRegEx).Captures[0].Value ?? throw new FormatException(nameof(preReleaseLabel));
        }
        if (!string.IsNullOrEmpty(buildLabel))
        {
            BuildLabel = Regex.Match(buildLabel, LabelUnitRegEx).Captures[0].Value ?? throw new FormatException(nameof(buildLabel));
        }
    }

    /// <summary>
    /// Construct a SemanticVersion.
    /// </summary>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version.</param>
    /// <param name="patch">The minor version.</param>
    /// <param name="label">The label for the version.</param>
    /// <exception cref="FormatException">
    /// If <paramref name="label"/> doesn't match 'LabelRegEx'.
    /// </exception>
    public SemanticVersion(uint major, uint minor, uint patch, string label)
        : this(major, minor, patch)
    {
        // We presume the SymVer :
        // 1) major.minor.patch-label
        // 2) 'label' starts with letter or digit.
        if (!string.IsNullOrEmpty(label))
        {
            Match match = Regex.Match(label, LabelRegEx);
            if (!match.Success)
                throw new FormatException(nameof(label));

            PreReleaseLabel = match.Groups["preLabel"].Value;
            BuildLabel = match.Groups["buildLabel"].Value;
        }
    }

    /// <summary>
    /// Parse <paramref name="version"/> and return the result if it is a valid <see cref="SemanticVersion"/>, otherwise throws an exception.
    /// </summary>
    /// <param name="version">The string to parse.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FormatException"></exception>
    /// <exception cref="OverflowException"></exception>
    public static SemanticVersion? Parse(string version)
    {
        return string.IsNullOrEmpty(version)
                ? throw new FormatException(nameof(version))
                : TryParse(version, out SemanticVersion? r) ? r : default;
    }

    /// <summary>
    /// Parse <paramref name="version"/> and return true if it is a valid <see cref="SemanticVersion"/>, otherwise return false.
    /// No exceptions are raised.
    /// </summary>
    /// <param name="version">The string to parse.</param>
    /// <param name="result">The return value when the string is a valid <see cref="SemanticVersion"/></param>
    public static bool TryParse(string version, [NotNullWhen(true)] out SemanticVersion? semanticVersion)
    {
        if (version is null || version.EndsWith("-", StringComparison.InvariantCultureIgnoreCase) || version.EndsWith("+", StringComparison.InvariantCultureIgnoreCase) || version.EndsWith(".", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new FormatException("Unexpected seperator character at end of version string.");
        }

        uint minor = 0;
        uint patch = 0;
        string? preLabel = null;
        string? buildLabel = null;

        // We parse the SymVer 'version' string 'major.minor.patch-PreReleaseLabel+BuildLabel'.
        int dashIndex = version.IndexOf('-', StringComparison.InvariantCultureIgnoreCase);
        int plusIndex = version.IndexOf('+', StringComparison.InvariantCultureIgnoreCase);


        string versionSansLabel;
        if (dashIndex > plusIndex)
        {
            // 'PreReleaseLabel' can contains dashes.
            if (plusIndex == -1)
            {
                // No buildLabel: buildLabel is null
                // Format is 'major.minor.patch-PreReleaseLabel'
                preLabel = version[(dashIndex + 1)..];
                versionSansLabel = version[..dashIndex];
            }
            else
            {
                // No PreReleaseLabel: preLabel is null
                // Format is 'major.minor.patch+BuildLabel'
                buildLabel = version[(plusIndex + 1)..];
                versionSansLabel = version[..plusIndex];
                dashIndex = -1;
            }
        }
        else
        {
            if (plusIndex == -1)
            {
                // Here dashIndex == plusIndex == -1
                // No preLabel - preLabel is null;
                // No buildLabel - buildLabel is null;
                // Format is 'major.minor.patch'
                versionSansLabel = version;
            }
            else if (dashIndex == -1)
            {
                // No PreReleaseLabel: preLabel is null
                // Format is 'major.minor.patch+BuildLabel'
                buildLabel = version[(plusIndex + 1)..];
                versionSansLabel = version[..plusIndex];
            }
            else
            {
                // Format is 'major.minor.patch-PreReleaseLabel+BuildLabel'
                preLabel = version.Substring(dashIndex + 1, plusIndex - dashIndex - 1);
                buildLabel = version[(plusIndex + 1)..];
                versionSansLabel = version[..dashIndex];
            }
        }

        if ((dashIndex != -1 && string.IsNullOrEmpty(preLabel)) ||
            (plusIndex != -1 && string.IsNullOrEmpty(buildLabel)) ||
            string.IsNullOrEmpty(versionSansLabel))
        {
            // We have dash and no preReleaseLabel  or
            // we have plus and no buildLabel or
            // we have no main version part (versionSansLabel==null)
            semanticVersion = default;
            return false;
        }
        semanticVersion = default;
        Match match = Regex.Match(versionSansLabel, VersionSansRegEx);
        if (!match.Success)
        {
            return false;
        }

        else if (!uint.TryParse(match.Groups["major"].Value, out uint major))
        {
            return false;
        }

        else if (match.Groups["minor"].Success && !uint.TryParse(match.Groups["minor"].Value, out minor))
        {
            return false;
        }

        else if (match.Groups["patch"].Success && !uint.TryParse(match.Groups["patch"].Value, out patch))
        {
            return false;
        }

        else if ((preLabel is not null && !Regex.IsMatch(preLabel, LabelUnitRegEx))
            || (buildLabel is not null && !Regex.IsMatch(buildLabel, LabelUnitRegEx)))
        {
            return false;
        }
        else
        {
            semanticVersion = new SemanticVersion(major, minor, patch, preLabel, buildLabel);
            return true;
        }
    }

    /// <summary>
    /// Implement ToString() as Major.Minor.Patch(-PreReleaseLabel)(+BuildLabel)
    /// </summary>
    public override string ToString() =>
        $"{Major}.{Minor}.{Patch}{(!string.IsNullOrEmpty(PreReleaseLabel) ? ('-' + PreReleaseLabel) : default)}{(!string.IsNullOrEmpty(BuildLabel) ? ('+' + BuildLabel) : default)}";

    /// <summary>
    /// Implement Compare.
    /// </summary>
    public static int Compare(SemanticVersion versionA, SemanticVersion versionB) =>
        versionA is not null ? versionA.CompareTo(versionB) : throw new ArgumentNullException(nameof(versionA));

    /// <summary>
    /// Implement <see cref="IComparable.CompareTo"/>
    /// </summary>
    public int CompareTo(object? obj) => obj is SemanticVersion semanticVersion ? CompareTo(semanticVersion) : throw new ArgumentException($"{nameof(obj)} is not a SemanticVersion.");

    /// <summary>
    /// Implement <see cref="IComparable{T}.CompareTo"/>.
    /// Meets SymVer 2.0 p.11 https://semver.org/
    /// </summary>
    public int CompareTo(SemanticVersion? obj)
    {
        return obj is null
            ? 1
            : Major != obj.Major
            ? Major > obj.Major ? 1 : -1
            : Minor != obj.Minor
            ? Minor > obj.Minor ? 1 : -1
            : Patch != obj.Patch ? Patch > obj.Patch ? 1 : -1 : ComparePreLabel(PreReleaseLabel, obj.PreReleaseLabel);
    }

    /// <summary>
    /// Implement <see cref="IEquatable{T}.Equals(T)"/>
    /// </summary>
    public bool Equals(SemanticVersion? other) =>
        // SymVer 2.0 standard requires to ignore 'BuildLabel' (Build metadata).
        this == other && string.Equals(PreReleaseLabel, other.PreReleaseLabel, StringComparison.Ordinal);

    /// <summary>
    /// Override <see cref="object.GetHashCode()"/>
    /// </summary>
    public override int GetHashCode() => ToString().GetHashCode(StringComparison.InvariantCultureIgnoreCase);

    /// <summary>
    /// Overloaded &lt; operator.
    /// </summary>
    public static bool operator <(SemanticVersion V1, SemanticVersion V2) => Compare(V1, V2) < 0;

    /// <summary>
    /// Overloaded &lt;= operator.
    /// </summary>
    public static bool operator <=(SemanticVersion V1, SemanticVersion V2) => Compare(V1, V2) <= 0;

    /// <summary>
    /// Overloaded &gt; operator.
    /// </summary>
    public static bool operator >(SemanticVersion V1, SemanticVersion V2) => Compare(V1, V2) > 0;

    /// <summary>
    /// Overloaded &gt;= operator.
    /// </summary>
    public static bool operator >=(SemanticVersion V1, SemanticVersion V2) => Compare(V1, V2) >= 0;

    private static int ComparePreLabel(string? preLabel1, string? preLabel2)
    {
        // Symver 2.0 standard p.9
        // Pre-release versions have a lower precedence than the associated normal version.
        // Comparing each dot separated identifier from left to right
        // until a difference is found as follows:
        //     identifiers consisting of only digits are compared numerically
        //     and identifiers with letters or hyphens are compared lexically in ASCII sort order.
        // Numeric identifiers always have lower precedence than non-numeric identifiers.
        // A larger set of pre-release fields has a higher precedence than a smaller set,
        // if all of the preceding identifiers are equal.
        if (string.IsNullOrEmpty(preLabel1))
        { return string.IsNullOrEmpty(preLabel2) ? 0 : 1; }

        if (string.IsNullOrEmpty(preLabel2))
        { return -1; }

        string[] units1 = preLabel1.Split('.');
        string[] units2 = preLabel2.Split('.');

        int minLength = units1.Length < units2.Length ? units1.Length : units2.Length;

        for (int i = 0; i < minLength; i++)
        {
            string ac = units1[i];
            string bc = units2[i];
            bool isNumber1 = int.TryParse(ac, out int number1);
            bool isNumber2 = int.TryParse(bc, out int number2);

            if (isNumber1 && isNumber2)
            {
                if (number1 != number2)
                { return number1 < number2 ? -1 : 1; }
            }
            else
            {
                if (isNumber1)
                { return -1; }

                if (isNumber2)
                { return 1; }

                int result = string.CompareOrdinal(ac, bc);
                if (result != 0)
                { return result; }
            }
        }

        return units1.Length.CompareTo(units2.Length);
    }
}
