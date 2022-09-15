using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ThreeDashTools.Patcher;

// ReSharper disable once UnusedType.Global UnusedMember.Global
public static class Patcher {
    // ReSharper disable once InconsistentNaming UnusedMember.Global
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void Patch(AssemblyDefinition assembly) {
        ModuleDefinition module = assembly.Modules[0];
        //TypeDefinition list = module.GetType("System.Collections.Generic", "List`1");

        //AddMethod(module.GetType("ItemScript"),
        //    new MethodDefinition("Update", MethodAttributes.Private, module.TypeSystem.Void));

        //TypeDefinition checkpointScript = module.GetType("CheckpointScript");
        //checkpointScript.Fields.Add(new FieldDefinition("musicPosition", FieldAttributes.Private,
        //    module.TypeSystem.Single));
    }

    // ReSharper disable once UnusedMember.Local
    private static void AddMethod(TypeDefinition type, MethodDefinition definition) {
        if(type.Methods.Any(def => def.Name == definition.Name))
            return;
        definition.Body.GetILProcessor().Emit(OpCodes.Ret);
        type.Methods.Add(definition);
    }

    //private static GenericInstanceType MakeList(TypeReference list, TypeReference type) =>
    //    list.MakeGenericInstanceType(type);
}
