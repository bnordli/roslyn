' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.SuggestionMode
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.SuggestionMode
    Friend Class VisualBasicSuggestionModeCompletionProvider
        Inherits SuggestionModeCompletionProvider

        Protected Overrides Async Function GetSuggestionModeItemAsync(document As Document, position As Integer, itemSpan As TextSpan, trigger As CompletionTrigger, cancellationToken As CancellationToken) As Task(Of CompletionItem)
            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)

            Dim span = New TextSpan(position, 0)
            Dim semanticModel = Await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(False)
            Dim syntaxTree = semanticModel.SyntaxTree

            ' If we're option explicit off, then basically any expression context can have a
            ' builder, since it might be an implicit local declaration.
            Dim targetToken = syntaxTree.GetTargetToken(position, cancellationToken)

            If semanticModel.OptionExplicit = False AndAlso (syntaxTree.IsExpressionContext(position, targetToken, cancellationToken) OrElse syntaxTree.IsSingleLineStatementContext(position, targetToken, cancellationToken)) Then
                Return CreateSuggestionModeItem(VBFeaturesResources.EmptyString1, itemSpan, VBFeaturesResources.EmptyString1)
            End If

            ' Builder if we're typing a field
            Dim description = VBFeaturesResources.TypeANameHereToDeclareA & vbCrLf &
                              VBFeaturesResources.NoteSpaceCompletionIsDisa

            If syntaxTree.IsFieldNameDeclarationContext(position, targetToken, cancellationToken) Then
                Return CreateSuggestionModeItem(VBFeaturesResources.NewField, itemSpan, description)
            End If

            If targetToken.Kind = SyntaxKind.None OrElse targetToken.FollowsEndOfStatement(position) Then
                Return Nothing
            End If

            ' Builder if we're typing a parameter
            If syntaxTree.IsParameterNameDeclarationContext(position, cancellationToken) Then
                ' Don't provide a builder if only the "Optional" keyword is recommended --
                ' it's mandatory in that case!
                Dim methodDeclaration = targetToken.GetAncestor(Of MethodBaseSyntax)()
                If methodDeclaration IsNot Nothing Then
                    If targetToken.Kind = SyntaxKind.CommaToken AndAlso targetToken.Parent.Kind = SyntaxKind.ParameterList Then
                        For Each parameter In methodDeclaration.ParameterList.Parameters.Where(Function(p) p.FullSpan.End < position)
                            ' A previous parameter was Optional, so the suggested Optional is an offer they can't refuse. No builder.
                            If parameter.Modifiers.Any(Function(modifier) modifier.Kind = SyntaxKind.OptionalKeyword) Then
                                Return Nothing
                            End If
                        Next
                    End If
                End If

                description = VBFeaturesResources.TypeANameHereToDeclareA0 & vbCrLf &
                              VBFeaturesResources.NoteSpaceCompletionIsDisa

                ' Otherwise just return a builder. It won't show up unless other modifiers are
                ' recommended, which is what we want.
                Return CreateSuggestionModeItem(VBFeaturesResources.ParameterName, itemSpan, description)
            End If

            ' Builder in select clause: after Select, after comma
            If targetToken.Parent.Kind = SyntaxKind.SelectClause Then
                If targetToken.IsKind(SyntaxKind.SelectKeyword, SyntaxKind.CommaToken) Then
                    description = VBFeaturesResources.TypeANewNameForTheColumn & vbCrLf &
                                  VBFeaturesResources.NoteUseTabForAutomaticCo

                    Return CreateSuggestionModeItem(VBFeaturesResources.ResultAlias, itemSpan, description)
                End If
            End If

            ' Build after For
            If targetToken.IsKindOrHasMatchingText(SyntaxKind.ForKeyword) AndAlso
               targetToken.Parent.IsKind(SyntaxKind.ForStatement) Then

                description = VBFeaturesResources.TypeANewVariableName & vbCrLf &
                              VBFeaturesResources.NoteSpaceAndCompletion

                Return CreateSuggestionModeItem(VBFeaturesResources.NewVariable, itemSpan, description)
            End If

            ' Build after Using
            If targetToken.IsKindOrHasMatchingText(SyntaxKind.UsingKeyword) AndAlso
               targetToken.Parent.IsKind(SyntaxKind.UsingStatement) Then

                description = VBFeaturesResources.TypeANewVariableName & vbCrLf &
                              VBFeaturesResources.NoteSpaceAndCompletion

                Return CreateSuggestionModeItem(VBFeaturesResources.NewResource, itemSpan, description)
            End If

            ' Builder at Namespace declaration name
            If syntaxTree.IsNamespaceDeclarationNameContext(position, cancellationToken) Then

                description = VBFeaturesResources.TypeANameHereToDeclareANamespace & vbCrLf &
                              VBFeaturesResources.NoteSpaceAndCompletion

                Return CreateSuggestionModeItem(VBFeaturesResources.NamespaceName, itemSpan, description)
            End If

            Return Nothing
        End Function

    End Class
End Namespace
