using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using OmniSharp.Models.TypeLookup;

namespace OmniSharp.Models.V1.TypeLookup;

public record DocumentationComment(
        string SummaryText = "",
        IEnumerable<DocumentationItem>? TypeParamElements = null,
        IEnumerable<DocumentationItem>? ParamElements = null,
        string ReturnsText = "",
        string RemarksText = "",
        string ExampleText = "",
        string ValueText = "",
        IEnumerable<DocumentationItem>? Exception = null)
{
    public static DocumentationComment From(string xmlDocumentation, string lineEnding)
    {
        if (string.IsNullOrEmpty(xmlDocumentation))
            return Empty;

        var reader = new StringReader("<docroot>" + xmlDocumentation + "</docroot>");
        var summaryText = new StringBuilder();
        var typeParamElements = new List<DocumentationItemBuilder>();
        var paramElements = new List<DocumentationItemBuilder>();
        var returnsText = new StringBuilder();
        var remarksText = new StringBuilder();
        var exampleText = new StringBuilder();
        var valueText = new StringBuilder();
        var exception = new List<DocumentationItemBuilder>();

        using var xml = XmlReader.Create(reader);
        xml.Read();
        string? elementName = null;
        StringBuilder? currentSectionBuilder = null;
        do
        {
            if (xml.NodeType == XmlNodeType.Element)
            {
                elementName = xml.Name.ToUpperInvariant();
                switch (elementName)
                {
                    case "filterpriority":
                        xml.Skip();
                        break;
                    case "remarks":
                        currentSectionBuilder = remarksText;
                        break;
                    case "example":
                        currentSectionBuilder = exampleText;
                        break;
                    case "exception":
                        DocumentationItemBuilder exceptionInstance = new(GetCref(xml["cref"]).TrimEnd());
                        currentSectionBuilder = exceptionInstance.Documentation;
                        exception.Add(exceptionInstance);
                        break;
                    case "returns":
                        currentSectionBuilder = returnsText;
                        break;
                    case "summary":
                        currentSectionBuilder = summaryText;
                        break;
                    case "param":
                        DocumentationItemBuilder paramInstance = new(TrimMultiLineString(xml["name"], lineEnding));
                        currentSectionBuilder = paramInstance.Documentation;
                        paramElements.Add(paramInstance);
                        break;
                    case "typeparam":
                        var typeParamInstance = new DocumentationItemBuilder(TrimMultiLineString(xml["name"], lineEnding));
                        currentSectionBuilder = typeParamInstance.Documentation;
                        typeParamElements.Add(typeParamInstance);
                        break;
                    case "value":
                        currentSectionBuilder = valueText;
                        break;
                    case string when currentSectionBuilder is null:
                        break;
                    case "typeparamref":
                        currentSectionBuilder.Append(xml["name"]);
                        currentSectionBuilder.Append(' ');
                        break;
                    case "see":
                        currentSectionBuilder.Append(GetCref(xml["cref"]));
                        currentSectionBuilder.Append(xml["langword"]);
                        break;
                    case "seealso":
                        currentSectionBuilder.Append("See also: ");
                        currentSectionBuilder.Append(GetCref(xml["cref"]));
                        break;
                    case "paramref":
                        currentSectionBuilder.Append(xml["name"]);
                        currentSectionBuilder.Append(' ');
                        break;
                    case "br":
                    case "para":
                        currentSectionBuilder.Append(lineEnding);
                        break;
                    default:
                        break;
                }
            }
            else if (xml.NodeType is XmlNodeType.Text && currentSectionBuilder is not null)
            {
                if (elementName is "code")
                {
                    currentSectionBuilder.Append(xml.Value);
                }
                else
                {
                    currentSectionBuilder.Append(TrimMultiLineString(xml.Value, lineEnding));
                }
            }
        } while (xml.Read());

        return new DocumentationComment(
            summaryText.ToString(),
            typeParamElements.Select(s => s.ConvertToDocumentedObject()).ToArray(),
            paramElements.Select(s => s.ConvertToDocumentedObject()).ToArray(),
            returnsText.ToString(),
            remarksText.ToString(),
            exampleText.ToString(),
            valueText.ToString(),
            exception.Select(s => s.ConvertToDocumentedObject()).ToArray());
    }

    private static string TrimMultiLineString(string? input, string lineEnding)
    {
        if (input is null)
            return string.Empty;
        string[] lines = input.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(lineEnding, lines.Select(l => TrimStartRetainingSingleLeadingSpace(l)));
    }

    private static string GetCref(string? cref)
    {
        if (cref is null || cref.Trim().Length == 0)
            return "";
        if (cref.Length < 2)
            return cref;
        if (cref.Substring(1, 1) is ":")
            return string.Concat(cref.AsSpan(2, cref.Length - 2), " ");
        return cref + " ";
    }

    private static string TrimStartRetainingSingleLeadingSpace(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        if (!char.IsWhiteSpace(input[0]))
            return input;
        return $" {input.TrimStart()}";
    }

    public string GetParameterText(string name)
        => Array.Find(ParamElements, parameter => parameter.Name == name)?.Documentation ?? string.Empty;

    public string GetTypeParameterText(string name)
        => Array.Find(TypeParamElements, typeParam => typeParam.Name == name)?.Documentation ?? string.Empty;

    public static readonly DocumentationComment Empty = new();
}

internal class DocumentationItemBuilder
{
    public string Name { get; set; }
    public StringBuilder Documentation { get; set; }

    public DocumentationItemBuilder(string name)
    {
        Documentation = new StringBuilder();
        Name = name;
    }

    public DocumentationItem ConvertToDocumentedObject() => new(Name, Documentation.ToString());
}
