using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MountAnything.Build;

public class ProviderReceiver : ISyntaxReceiver
{
    public string? ClassName { get; set; }
    public string? Namespace { get; set; }
    
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax classDeclaration &&
            TryGetAttribute(classDeclaration, "CmdletProvider", out var attribute))
        {
            ClassName = classDeclaration.Identifier.ToString();
            Namespace = classDeclaration.Parent!.GetType().GetProperty("Name")!.GetValue(classDeclaration.Parent).ToString();
        }
    }

    private static bool TryGetAttribute(ClassDeclarationSyntax classDeclaration, string attributeName,
        out AttributeSyntax attributeSyntax)
    {
        var attribute = classDeclaration.AttributeLists.SelectMany(a => a.Attributes)
            .FirstOrDefault(a => a.Name.ToString() == attributeName);

        if (attribute == null)
        {
            attributeSyntax = null!;
            return false;
        }
        
        attributeSyntax = attribute;
        return true;
    }
}