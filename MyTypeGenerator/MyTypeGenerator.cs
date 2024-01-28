using System.Reflection;
using System.Reflection.Emit;

namespace DynamicProxyGenerator;

public class MyTypeGenerator
{
    private const string ASSEMBLY_NAME_PREFIX = "";
    private const string MODULE_NAME_PREFIX = "";

    public static Type Generate()
    {

        var assemblyNameString = NameHelper.CreateUniqueName(ASSEMBLY_NAME_PREFIX);
        var assemblyName = new AssemblyName(assemblyNameString);
        var moduleName = NameHelper.CreateUniqueName(MODULE_NAME_PREFIX);

        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);

        var module = assembly.DefineDynamicModule(moduleName);

        #region Signature

        // INFO: Define "public class MyType { }"
        var typeBuilder = module.DefineType(
            name: "MyType",
            attr: TypeAttributes.Public | TypeAttributes.Class);

        DefineSaySomehtingMethod(typeBuilder);
        DefineToStringMethod(typeBuilder);

        return typeBuilder.CreateType()!;
    }

    static void DefineSaySomehtingMethod(TypeBuilder typeBuilder)
    {
        // INFO: Define method. "public void SaySomething()"
        var methodBuilder = typeBuilder.DefineMethod("SaySomething", MethodAttributes.Public);

        // INFO: Define method params. "public void SaySomething(string message, int count)"
        methodBuilder.SetParameters(typeof(string), typeof(int));
        methodBuilder.DefineParameter(0, ParameterAttributes.None, "message");
        methodBuilder.DefineParameter(1, ParameterAttributes.None, "count");

        #endregion Signature

        // INFO: Find "Console.WriteLine(string arg_1)"
        var consoleType = typeof(CustomConsoleWriter);
        var writeLineMethod = consoleType.GetMethod(
            nameof(Console.WriteLine),
            new[] { typeof(string), typeof(int) })!;

        var methodILGenerator = methodBuilder.GetILGenerator();


        // 1. Load arguments in what is known as the "Evaluation stack"
        methodILGenerator.Emit(OpCodes.Ldarg_1);
        methodILGenerator.Emit(OpCodes.Ldarg_2);
        // 2. Call Console.WriteLine(string, int);
        // opinion: maybe will consume in evaluation stack
        methodILGenerator.EmitCall(
            opcode: OpCodes.Call,
            methodInfo: writeLineMethod,
            optionalParameterTypes: new[] {
                typeof(string), // stack1: arg_1
                typeof(int)  // stack2: arg_2
            });

        methodILGenerator.Emit(OpCodes.Ret);
    }

    static void DefineToStringMethod(TypeBuilder typeBuilder)
    {
        var toStringMethod = typeof(object).GetMethod(nameof(object.ToString))!;

        var newToStringMethod = typeBuilder.DefineMethod(
            name: nameof(object.ToString),
            attributes: toStringMethod.Attributes,
            returnType: typeof(string),
            parameterTypes: Array.Empty<Type>()
        );

        var toStringILGenerator = newToStringMethod.GetILGenerator();

        // Pushes a new object reference to a string literal stored in the metadata.
        toStringILGenerator.Emit(OpCodes.Ldstr, "A message from ToString()");
        toStringILGenerator.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(
            methodInfoBody: newToStringMethod,
            methodInfoDeclaration: toStringMethod);
    }

}
