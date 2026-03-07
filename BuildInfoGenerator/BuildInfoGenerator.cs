using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BuildInfoGenerator
{
    // This generator generates assembly information, without having to rely on the System.Reflection namespace.
    [Generator]
    public class BuildInfoGenerator : ISourceGenerator
    {
        private const string NEWLINE = "\n";

        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                BuildSourceInfo(context);
            }
            catch (Exception ex)
            {
                var descriptor = new DiagnosticDescriptor(
                    id: "BUILDGEN999",
                    title: "Generator crashed",
                    messageFormat: $"BuildInfoGenerator crashed: {ex}",
                    category: "Build",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true);
                context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
            }
        }

        private static bool BuildSourceInfo(GeneratorExecutionContext context)
        {
            if (!GetProperty(context,
                             propertyName: "RootNamespace",
                             isRequired: true,
                             property: out string rootNamespace))
            {
                return false;
            }

            // (Name, Value)
            List<ValueTuple<string, string>> constants = new List<ValueTuple<string, string>>(10);

            constants.Add(("Version", context.Compilation.Assembly.Identity.Version.ToString()));
            constants.Add(("Name", context.Compilation.Assembly.Identity.Name));

            string description = GetAssemblyDescription(context);

            if (description != null)
            {
                constants.Add(("Description", description));
            }

            string constantsSource = string.Empty;

            for (int index = 0; index < constants.Count; index++)
            {
                ValueTuple<string, string> constant = constants[index];

                constantsSource += $"public const string {constant.Item1} = \"{constant.Item2}\";";

                if (index + 1 < constants.Count)
                {
                    constantsSource += NEWLINE;
                }
            }

            string source = $@"
            namespace {rootNamespace}
            {{
                internal static class BuildInfo
                {{
                    {constantsSource}
                }}
            }}";

            context.AddSource("BuildInfo.g.cs", SourceText.From(source, Encoding.UTF8));

            return true;
        }

        private static string GetAssemblyDescription(GeneratorExecutionContext context)
        {
            IAssemblySymbol assembly = context.Compilation.Assembly;

            AttributeData descriptionAttribute = assembly
            .GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.Reflection.AssemblyDescriptionAttribute");

            if (descriptionAttribute == null)
            {
                return null;
            }

            return descriptionAttribute.ConstructorArguments.Length > 0 ?
                descriptionAttribute.ConstructorArguments[0].Value as string
                : null;
        }

        private static bool GetProperty(GeneratorExecutionContext context, string propertyName, bool isRequired, out string property)
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{propertyName}", out property))
            {
                property = null;

                if (isRequired)
                {
                    DiagnosticDescriptor diagnostic = new DiagnosticDescriptor
                    (
                    id: "BUILDGEN001",
                    title: "Missing required property",
                    messageFormat: $"The build property \"{propertyName}\" must be set for this generator.",
                    category: "Build",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                    );

                    context.ReportDiagnostic(Diagnostic.Create(diagnostic, Location.None));
                }

                return false;
            }

            return true;
        }
    }
}
