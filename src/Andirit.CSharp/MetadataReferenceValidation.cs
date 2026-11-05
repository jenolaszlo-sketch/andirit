using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Andirit;

// Bounded Q-F3 structural checks. This is not an IL verifier or a semantic binder.
internal static class MetadataReferenceValidation
{
    internal static void Validate(MetadataReader reader, CancellationToken cancellationToken)
    {
        foreach (var handle in reader.TypeReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var type = reader.GetTypeReference(handle);
            Name(reader, type.Name);
            _ = reader.GetString(type.Namespace);
        }
        foreach (var handle in reader.TypeDefinitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var type = reader.GetTypeDefinition(handle);
            Name(reader, type.Name);
            _ = reader.GetString(type.Namespace);
            GenericParameters(reader, type.GetGenericParameters(), handle);
        }
        foreach (var handle in reader.MethodDefinitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var method = reader.GetMethodDefinition(handle);
            Name(reader, method.Name);
            var owner = method.GetDeclaringType();
            Require(!owner.IsNil);
            int typeArity = reader.GetTypeDefinition(owner).GetGenericParameters().Count;
            var generics = method.GetGenericParameters();
            GenericParameters(reader, generics, handle);
            var blob = reader.GetBlobReader(method.Signature);
            int parameterCount;
            try { parameterCount = Signature(reader, ref blob, method.Attributes, typeArity, generics.Count, cancellationToken); }
            catch (BadImageFormatException exception)
            { throw new BadImageFormatException($"Invalid MethodDef signature at row {MetadataTokens.GetRowNumber(handle)} ({reader.GetString(method.Name)}).", exception); }
            var sequences = new HashSet<int>();
            foreach (var parameterHandle in method.GetParameters())
            {
                var parameter = reader.GetParameter(parameterHandle);
                Require(parameter.SequenceNumber <= parameterCount && sequences.Add(parameter.SequenceNumber));
                _ = reader.GetString(parameter.Name);
            }
        }
    }

    private static void Name(MetadataReader reader, StringHandle handle) => Require(reader.GetString(handle).Length != 0);

    private static void GenericParameters(MetadataReader reader, GenericParameterHandleCollection parameters, EntityHandle owner)
    {
        int index = 0;
        foreach (var handle in parameters)
        {
            var parameter = reader.GetGenericParameter(handle);
            Require(parameter.Parent == owner && parameter.Index == index++);
        }
    }

    private enum Kind { Type, Sequence, ArrayShape }
    [Flags]
    private enum Position { Type = 0, Void = 1, ByRef = 2 }
    private readonly record struct Work(Kind Kind, Position Position = Position.Type, int Count = 0, bool Sentinel = false);

    private static int Signature(MetadataReader reader, ref BlobReader blob, MethodAttributes attributes,
        int typeArity, int methodArity, CancellationToken cancellationToken)
    {
        var pending = new Stack<Work>();
        int parameterCount = Method(ref blob, pending, definition: true, attributes, methodArity);
        // Explicit work items keep both long and deeply nested signatures bounded by
        // their bytes, without recursive decoder stack exhaustion or count-sized allocations.
        while (pending.TryPop(out var work))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (work.Kind == Kind.Sequence)
            {
                if (work.Count == 0) continue;
                Require(work.Count <= blob.RemainingBytes);
                bool sentinel = work.Sentinel;
                if (sentinel && Peek(ref blob) == 0x41)
                {
                    _ = blob.ReadByte();
                    sentinel = false;
                }
                pending.Push(work with { Count = work.Count - 1, Sentinel = sentinel });
                pending.Push(new(Kind.Type, work.Position));
                continue;
            }
            if (work.Kind == Kind.ArrayShape)
            {
                int rank = blob.ReadCompressedInteger();
                Require(rank > 0);
                int sizes = blob.ReadCompressedInteger();
                Require(sizes <= rank && sizes <= blob.RemainingBytes);
                for (int i = 0; i < sizes; i++) _ = blob.ReadCompressedInteger();
                int bounds = blob.ReadCompressedInteger();
                Require(bounds <= rank && bounds <= blob.RemainingBytes);
                for (int i = 0; i < bounds; i++) _ = blob.ReadCompressedSignedInteger();
                continue;
            }
            byte code = blob.ReadByte();
            while (code is 0x1f or 0x20) // custom modifiers; preserve legal metadata names
            {
                TypeHandle(reader, blob.ReadTypeHandle(), allowSpecification: true);
                code = blob.ReadByte();
            }
            switch (code)
            {
                case 0x01: Require(work.Position.HasFlag(Position.Void)); break;
                case >= 0x02 and <= 0x0e: // primitive types, including string
                case 0x18:
                case 0x19:
                case 0x1c: // native int/uint, object
                case 0x16: break; // typedref, including runtime pointer forms (ArgIterator)
                case 0x0f: pending.Push(new(Kind.Type, Position.Void)); break; // unmanaged pointer
                case 0x10:
                    Require(work.Position.HasFlag(Position.ByRef));
                    pending.Push(new(Kind.Type));
                    break;
                case 0x11:
                case 0x12:
                    TypeHandle(reader, blob.ReadTypeHandle(), allowSpecification: false);
                    break;
                case 0x13: Require(blob.ReadCompressedInteger() < typeArity); break;
                case 0x1e: Require(blob.ReadCompressedInteger() < methodArity); break;
                case 0x14:
                    pending.Push(new(Kind.ArrayShape));
                    pending.Push(new(Kind.Type));
                    break;
                case 0x1d: pending.Push(new(Kind.Type)); break;
                case 0x15:
                    Require(blob.ReadByte() is 0x11 or 0x12);
                    var generic = blob.ReadTypeHandle();
                    TypeHandle(reader, generic, allowSpecification: false);
                    int count = blob.ReadCompressedInteger();
                    Require(count > 0 && count <= blob.RemainingBytes);
                    if (generic.Kind == HandleKind.TypeDefinition)
                        Require(count == reader.GetTypeDefinition((TypeDefinitionHandle)generic).GetGenericParameters().Count);
                    pending.Push(new(Kind.Sequence, Count: count));
                    break;
                case 0x1b:
                    _ = Method(ref blob, pending, definition: false, 0, methodArity);
                    break;
                default: throw new BadImageFormatException("Invalid method signature type.");
            }
        }
        Require(blob.RemainingBytes == 0);
        return parameterCount;
    }

    private static int Method(ref BlobReader blob, Stack<Work> pending, bool definition,
        MethodAttributes attributes, int methodArity)
    {
        var header = blob.ReadSignatureHeader();
        Require(header.Kind == SignatureKind.Method && (header.RawValue & 0x80) == 0);
        Require(!header.HasExplicitThis || header.IsInstance);
        if (definition)
        {
            Require(header.CallingConvention is SignatureCallingConvention.Default or SignatureCallingConvention.VarArgs);
            Require(!header.HasExplicitThis && header.IsInstance == !attributes.HasFlag(MethodAttributes.Static));
        }
        else Require((int)header.CallingConvention <= 5 || (int)header.CallingConvention == 9);
        int arity = header.IsGeneric ? blob.ReadCompressedInteger() : 0;
        Require(!header.IsGeneric || arity > 0);
        if (definition) Require(arity == methodArity);
        int count = blob.ReadCompressedInteger();
        Require(count < blob.RemainingBytes); // at least one return type byte plus each parameter
        pending.Push(new(Kind.Sequence, Position.ByRef, count,
            !definition && header.CallingConvention == SignatureCallingConvention.VarArgs));
        pending.Push(new(Kind.Type, Position.Void | Position.ByRef));
        return count;
    }

    private static byte Peek(ref BlobReader blob)
    {
        var copy = blob;
        return copy.ReadByte();
    }

    private static void TypeHandle(MetadataReader reader, EntityHandle handle, bool allowSpecification)
    {
        Require(!handle.IsNil);
        int count = handle.Kind switch
        {
            HandleKind.TypeDefinition => reader.TypeDefinitions.Count,
            HandleKind.TypeReference => reader.TypeReferences.Count,
            HandleKind.TypeSpecification when allowSpecification => reader.GetTableRowCount(TableIndex.TypeSpec),
            _ => throw new BadImageFormatException("Invalid signature type handle.")
        };
        Require(MetadataTokens.GetRowNumber(handle) <= count);
        if (handle.Kind == HandleKind.TypeSpecification)
            _ = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new BadImageFormatException("Invalid reference metadata structure.");
    }
}
