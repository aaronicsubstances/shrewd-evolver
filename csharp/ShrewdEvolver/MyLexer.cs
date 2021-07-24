// tag: 20210724T0000
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver
{
    public class MyLexer
    {
        public static List<MyToken> Parse(string code)
        {
            CompilationUnitSyntax tree = SyntaxFactory.ParseCompilationUnit(code);
            IEnumerable<SyntaxToken> rawTokens = tree.DescendantTokens();
            var malFormedToken = rawTokens.FirstOrDefault(x => x.IsMissing);
            if (malFormedToken.Kind() == SyntaxKind.None)
            {
                malFormedToken = rawTokens.FirstOrDefault(t => t.GetAllTrivia().Any(
                    x => x.Kind() == SyntaxKind.SkippedTokensTrivia));
            }
            if (malFormedToken.Kind() != SyntaxKind.None)
            {
                throw new Exception($"Received code is malformed around: {malFormedToken}");
            }
            int runningIndex = 0;
            var myTokens = new List<MyToken>();
            foreach (var rawToken in rawTokens)
            {
                if (rawToken.HasLeadingTrivia)
                {
                    foreach (var trivia in rawToken.LeadingTrivia)
                    {
                        if (trivia.Kind() == SyntaxKind.WhitespaceTrivia ||
                            trivia.Kind() == SyntaxKind.EndOfLineTrivia)
                        {
                            continue;
                        }
                        myTokens.Add(MyToken.Create(runningIndex++, trivia));
                    }
                }
                myTokens.Add(MyToken.Create(runningIndex++, rawToken));
                if (rawToken.HasTrailingTrivia)
                {
                    foreach (var trivia in rawToken.TrailingTrivia)
                    {
                        if (trivia.Kind() == SyntaxKind.WhitespaceTrivia ||
                            trivia.Kind() == SyntaxKind.EndOfLineTrivia)
                        {
                            continue;
                        }
                        myTokens.Add(MyToken.Create(runningIndex++, trivia));
                    }
                }
            }
            return myTokens;
        }
    }

    public class MyToken
    {
        public int Index { get; set; }
        public int LineNumber { get; set; }
        public string RawType { get; set; }
        public string GenericType { get; set; }
        public bool IsKeyword { get; set; }
        public int StartPos { get; set; }
        public int EndPos { get; set; }
        public string Text { get; set; }
        public bool IsTrivia { get; set; }

        internal static MyToken Create(int index, SyntaxTrivia trivia)
        {
            var loc = trivia.GetLocation();
            var lineInfo = loc.GetLineSpan();
            var genericTokenType = GetGenericType(trivia.Kind(), false);
            var t = new MyToken
            {
                Index = index,
                LineNumber = lineInfo.StartLinePosition.Line,
                RawType = trivia.Kind().ToString(),
                GenericType = genericTokenType,
                Text = trivia.ToString(),
                StartPos = loc.SourceSpan.Start,
                EndPos = loc.SourceSpan.End,
                IsTrivia = true
            };
            return t;
        }

        internal static MyToken Create(int index, SyntaxToken rawToken)
        {
            var loc = rawToken.GetLocation();
            var lineInfo = loc.GetLineSpan();
            var genericTokenType = GetGenericType(rawToken.Kind(), rawToken.IsKeyword());
            var t = new MyToken
            {
                Index = index,
                LineNumber = lineInfo.StartLinePosition.Line,
                RawType = rawToken.Kind().ToString(),
                GenericType = genericTokenType,
                Text = rawToken.Text,
                StartPos = loc.SourceSpan.Start,
                EndPos = loc.SourceSpan.End
            };
            return t;
        }

        private static string GetGenericType(SyntaxKind tokenKind, bool isKeyword)
        {
            if (isKeyword)
            {
                return "keyword";
            }
            switch (tokenKind)
            {
                case SyntaxKind.IdentifierToken:
                    return "identifier";
                case SyntaxKind.NumericLiteralToken:
                    return "number";
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.InterpolatedStringTextToken:
                    return "string";
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                    return "comment";
                case SyntaxKind.EndOfFileToken:
                    return "eof";
                default:
                    return null;
            }
        }
    }
}
