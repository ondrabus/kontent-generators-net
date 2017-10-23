﻿using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace CloudModelGenerator
{
    public class ClassCodeGenerator
    {
        public const string DEFAULT_NAMESPACE = "KenticoCloudModels";

        public ClassDefinition ClassDefinition { get; }

        public string ClassFilename { get; }

        public bool CustomPartial { get; }

        public string Namespace { get; }

        public bool OverwriteExisting { get; }

        public ClassCodeGenerator(ClassDefinition classDefinition, string classFilename, string @namespace = DEFAULT_NAMESPACE, bool customPartial = false)
        {
            ClassDefinition = classDefinition ?? throw new ArgumentNullException(nameof(classDefinition));
            ClassFilename = classFilename ?? ClassDefinition.ClassName;
            CustomPartial = customPartial;
            Namespace = @namespace ?? DEFAULT_NAMESPACE;
            OverwriteExisting = !CustomPartial;
        }

        public string GenerateCode()
        {
            var usings = new[]
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Collections.Generic")),
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("KenticoCloud.Delivery"))
            };

            var properties = ClassDefinition.Properties.Select(element =>
                SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(element.TypeName), element.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    )
            ).ToArray();

            var propertyCodenameConstants = ClassDefinition.PropertyCodenameConstants.Select(element =>
                    SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.VariableDeclaration(
                                    SyntaxFactory.ParseTypeName("string"),
                                    SyntaxFactory.SeparatedList(new[] {
                                        SyntaxFactory.VariableDeclarator(
                                            SyntaxFactory.Identifier($"{TextHelpers.GetValidPascalCaseIdentifierName(element.Name)}Codename"),
                                            null,
                                            SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression( SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(element.Codename)))
                                        )
                                    })
                                )
                            )
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.ConstKeyword))
            ).ToArray();

            var classDeclaration = SyntaxFactory.ClassDeclaration(ClassDefinition.ClassName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

            if (!CustomPartial)
            {
                var classCodenameConstant = SyntaxFactory.FieldDeclaration(
                                SyntaxFactory.VariableDeclaration(
                                        SyntaxFactory.ParseTypeName("string"),
                                        SyntaxFactory.SeparatedList(new[] {
                                            SyntaxFactory.VariableDeclarator(
                                                SyntaxFactory.Identifier("Codename"),
                                                null,
                                                SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression( SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(ClassDefinition.Codename)))
                                            )
                                        })
                                    )
                                )
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.ConstKeyword));

                classDeclaration = classDeclaration.AddMembers(classCodenameConstant);
            }

            classDeclaration = classDeclaration.AddMembers(propertyCodenameConstants)
                .AddMembers(properties);

            var description = SyntaxFactory.Comment(
@"// This code was generated by a cloud-generators-net tool 
// (see https://github.com/Kentico/cloud-generators-net).
// 
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated. 
// For further modifications of the class, create a separate file with the partial class." + Environment.NewLine + Environment.NewLine
);

            CompilationUnitSyntax cu = SyntaxFactory.CompilationUnit()
                .AddUsings(usings)
                .AddMembers(
                    SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(Namespace))
                        .AddMembers(classDeclaration)
                );

            if (!CustomPartial)
            {
                cu = cu.WithLeadingTrivia(description);
            }

            AdhocWorkspace cw = new AdhocWorkspace();
            return Formatter.Format(cu, cw).ToFullString();
        }
    }
}
