using IronBlock.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace IronBlock.Blocks.Text
{
    public class TextLength : IBlock
    {
        public override async Task<object> EvaluateAsync(Context context)
        {
            var text = (await this.Values.EvaluateAsync("VALUE", context) ?? "").ToString();

            return (double) text.Length;
        }

		public override SyntaxNode Generate(Context context)
		{
			var textExpression = this.Values.Generate("VALUE", context) as ExpressionSyntax;
			if (textExpression == null) throw new ApplicationException($"Unknown expression for text.");

			return SyntaxGenerator.PropertyAccessExpression(textExpression, nameof(string.Length));
		}
	}
}