using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Types;

internal record PointerType(IType Base) : IType
{
    public virtual TypeReference Resolve(TranslationUnitContext context)
    {
        if (Base is FunctionType ft)
            return ft.ResolvePointer(context);

        return Base.Resolve(context).MakePointerType();
    }

    public virtual int SizeInBytes => throw new WipException(132, "Could not calculate size yet.");

    // explicit impl while Size not implemented
    public override string ToString()
        => $"PointerType {{ Base = {Base} }}";
}
