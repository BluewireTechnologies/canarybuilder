using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Text.RegularExpressions;

public class AssemblyInfo : Microsoft.Build.Utilities.Task
{
    [Required]
    public string FileName { get; set; }
    [Output]
    public string AssemblyVersion {
        get { return assemblyVersion.ExistingValue ?? ""; }
        set { assemblyVersion.NewValue = value; }
    }
    [Output]
    public string AssemblyFileVersion {
        get { return assemblyFileVersion.ExistingValue ?? ""; }
        set { assemblyFileVersion.NewValue = value; }
    }
    [Output]
    public string AssemblyInformationalVersion {
        get { return assemblyInformationalVersion.ExistingValue ?? ""; }
        set { assemblyInformationalVersion.NewValue = value; }
    }

    private AttributeInfo assemblyVersion = new AttributeInfo(typeof(AssemblyVersionAttribute));
    private AttributeInfo assemblyFileVersion = new AttributeInfo(typeof(AssemblyFileVersionAttribute));
    private AttributeInfo assemblyInformationalVersion = new AttributeInfo(typeof(AssemblyInformationalVersionAttribute));

    private static Regex rxAttribute = new Regex(@"^\s*  \[  \s*assembly\s*:\s*  (?<name>[\.\w]+)  \s*\(\s*@? ""  (?<parameter>.*)  ""  \s*\)\s*  \]", RegexOptions.IgnorePatternWhitespace);

    public override bool Execute()
    {
        var allAttrs = new[] {
            assemblyVersion,
            assemblyFileVersion,
            assemblyInformationalVersion
        };
        var lines = File.ReadAllLines(FileName);

        var processedLines = lines.Select(ProcessLine).ToList();

        var missing = allAttrs.Where(a => !String.IsNullOrEmpty(a.NewValue) && a.ExistingValue == null);
        foreach(var attr in missing)
        {
            processedLines.Add(CreateAttribute(attr));
        }

        if (allAttrs.Any(a => !String.IsNullOrEmpty(a.NewValue))) File.WriteAllLines(FileName, processedLines);
        return !Log.HasLoggedErrors;
    }

    private string ProcessLine(string line)
    {
        if (line.TrimStart().StartsWith("//")) return line; // Comment.
        var m = rxAttribute.Match(line);
        if (!m.Success) return line;

        var attr = GetAssemblyInfoAttribute(m.Groups["name"].Value);
        if (attr == null) return line;

        attr.ExistingValue = ReadAttribute(m);
        if (String.IsNullOrEmpty(attr.NewValue)) return line;

        return CreateAttribute(attr);
    }

    // NOTE: No unescaping is done. Assumes a simple string, like a version number.
    private string ReadAttribute(Match m) { return m.Groups["parameter"].Value; }
    // NOTE: No escaping is done. Assumes a simple string, like a version number.
    private string CreateAttribute(AttributeInfo attr) { return String.Format("[assembly: {0}(\"{1}\")]", attr.Type.FullName, attr.NewValue); }

    class AttributeInfo
    {
        public AttributeInfo(Type attributeType) { Type = attributeType; }
        public Type Type { get; set; }
        public string NewValue { get; set; }
        public string ExistingValue { get; set; }
    }

    private AttributeInfo GetAssemblyInfoAttribute(string name)
    {
        // We shall quietly ignore the namespace since it's highly unlikely that someone
        // will create custom AssemblyVersionAttribute, etc classes.
        var typeName = name.Substring(name.LastIndexOf(".") + 1);
        if (!typeName.EndsWith("Attribute")) typeName += "Attribute";
        switch (typeName)
        {
            case "AssemblyVersionAttribute": return assemblyVersion;
            case "AssemblyFileVersionAttribute": return assemblyFileVersion;
            case "AssemblyInformationalVersionAttribute": return assemblyInformationalVersion;
        }
        return null;
    }
}
