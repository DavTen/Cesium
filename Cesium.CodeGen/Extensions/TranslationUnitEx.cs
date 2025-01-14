using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Extensions;

internal static class TranslationUnitEx
{
    public static IEnumerable<IBlockItem> ToIntermediate(this Ast.TranslationUnit translationUnit) =>
        translationUnit.Declarations.SelectMany(x => (x switch
        {
            Ast.FunctionDefinition func => new IBlockItem[] { new FunctionDefinition(func) },
            Ast.SymbolDeclaration sym => GetTopLevelDeclarations(sym),
            _ => throw new WipException(212, $"Declaration not supported: {x}.")
        }));

    private static IEnumerable<IBlockItem> GetTopLevelDeclarations(Ast.SymbolDeclaration sym)
    {
        sym.Deconstruct(out var astDeclaration);
        var wholeDeclaration = IScopedDeclarationInfo.Of(astDeclaration);
        switch (wholeDeclaration)
        {
            case ScopedIdentifierDeclaration scopedDeclaration:
                var (storageClass, items) = scopedDeclaration;

                foreach (var (declaration, initializer) in items)
                {
                    var (type, identifier, cliImportMemberName) = declaration;
                    if (identifier == null)
                        throw new CompilationException($"Unnamed global symbol of type {type} is not supported.");

                    if (type is FunctionType functionType)
                    {
                        if (initializer != null)
                            throw new CompilationException(
                                $"Initializer expression for a function declaration isn't supported: {initializer}.");

                        if (storageClass != StorageClass.Auto)
                            throw new WipException(344,
                                $"Storage class {storageClass} isn't supported for a top-level function, yet.");

                        var functionDeclaration = new FunctionDeclaration(identifier, functionType, cliImportMemberName);
                        yield return functionDeclaration;
                        continue;
                    }

                    if (cliImportMemberName != null)
                    {
                        throw new CompilationException($"CLI initializer should be a function for identifier {identifier}.");
                    }

                    if (type is PrimitiveType or PointerType or InPlaceArrayType) // TODO[#75]: Consider other type categories.
                    {
                        var variable = new GlobalVariableDefinition(storageClass, type, identifier, initializer);
                        yield return variable;
                        continue;
                    }

                    throw new WipException(75, $"Declaration not supported, yet: {declaration}.");
                }
                break;
            case TypeDefDeclaration typeDefDeclaration:
                var typeDefBlockItem = new TypeDefBlockItem(typeDefDeclaration);
                yield return typeDefBlockItem;
                break;
            default:
                throw new WipException(212, $"Unknown kind of declaration: {wholeDeclaration}.");
        }
    }
}
