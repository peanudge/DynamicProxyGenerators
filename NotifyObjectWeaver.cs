using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicProxyGenerator;

public static class NotifyingObjectWeaver
{
    const string DynamicAssemblyName = "Dynamic Assembly";
    const string DynamicModuleName = "Dynamic Module";
    const string PropertyChangedEventName = nameof(INotifyPropertyChanged.PropertyChanged);
    const string OnPropertyChangedMethodName = "OnPropertyChanged";
    static readonly Type VoidType = typeof(void);
    static readonly Type DelegateType = typeof(Delegate);

    const MethodAttributes EventMethodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;
    const MethodAttributes OnPropertyChangedAttributes = MethodAttributes.Public | MethodAttributes.HideBySig;

    static readonly AssemblyBuilder DynamicAssembly;
    static readonly ModuleBuilder DynamicModule;

    static readonly Dictionary<Type, Type> Proxies = new();

    static NotifyingObjectWeaver()
    {
        var assemblyName = new AssemblyName(DynamicAssemblyName);
        DynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        DynamicModule = DynamicAssembly.DefineDynamicModule(DynamicModuleName);
    }


    public static Type GetProxyType(Type type)
    {
        Type proxyType;
        if (Proxies.ContainsKey(type))
        {
            proxyType = Proxies[type];
        }
        else
        {
            proxyType = CreateProxyType(type);
            Proxies[type] = proxyType;
        }
        return proxyType;
    }

    // 1. Create a new type.
    static Type CreateProxyType(Type type)
    {
        var typeBuilder = DefineType(type);
        var eventHandlerType = typeof(PropertyChangedEventHandler);
        var propertyChangedFieldBuilder = typeBuilder.DefineField(
            PropertyChangedEventName,
            eventHandlerType,
            FieldAttributes.Private);

        DefineEvent(typeBuilder, eventHandlerType, propertyChangedFieldBuilder);

        var onPropertyChangedMethodBuilder = DefineOnPropertyChangedMethod(
            typeBuilder,
            propertyChangedFieldBuilder);

        DefineProperties(typeBuilder, type, onPropertyChangedMethodBuilder);

        return typeBuilder.CreateType()!;
    }

    // 2. Inherit from the existing type.
    static TypeBuilder DefineType(Type type)
    {
        var name = $"{type.Name}_Proxy";
        var typeBuilder = DynamicModule.DefineType(
            name,
            TypeAttributes.Public | TypeAttributes.Class);

        typeBuilder.SetParent(type);
        var interfaceType = typeof(INotifyPropertyChanged);
        typeBuilder.AddInterfaceImplementation(interfaceType);
        return typeBuilder;
    }


    // 3. Implement the INotifyPropertyChanged interface.
    static void DefineEvent(TypeBuilder typeBuilder, Type eventHandlerType, FieldBuilder fieldBuilder)
    {
        var eventBuilder = typeBuilder.DefineEvent(
            name: nameof(INotifyPropertyChanged.PropertyChanged),
            attributes: EventAttributes.None,
            eventtype: eventHandlerType);
        DefineAddMethodForEvent(typeBuilder, eventHandlerType, fieldBuilder, eventBuilder);
        DefineRemoveMethodForEvent(typeBuilder, eventHandlerType, fieldBuilder, eventBuilder);
    }

    static void DefineAddMethodForEvent(
        TypeBuilder typeBuilder,
        Type eventHandlerType,
        FieldBuilder fieldBuilder,
        EventBuilder eventBuilder)
    {
        var combineMethodInfo = DelegateType.GetMethod(
            "Combine",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { DelegateType, DelegateType },
            null
        )!;

        var addEventMethod = string.Format("add_{0}", PropertyChangedEventName);
        var addMethodBuilder = typeBuilder.DefineMethod(
            addEventMethod, EventMethodAttributes, VoidType, new[] { eventHandlerType });
        var addMethodGenerator = addMethodBuilder.GetILGenerator();

        addMethodGenerator.Emit(OpCodes.Ldarg_0);

        addMethodGenerator.Emit(OpCodes.Ldarg_0);
        addMethodGenerator.Emit(OpCodes.Ldfld, fieldBuilder);

        addMethodGenerator.Emit(OpCodes.Ldarg_1);
        addMethodGenerator.EmitCall(OpCodes.Call, combineMethodInfo, null); // arg_0, field(event), arg_1(eventHandler)
        addMethodGenerator.Emit(OpCodes.Castclass, eventHandlerType);
        addMethodGenerator.Emit(OpCodes.Stfld, fieldBuilder); // store value to field
        addMethodGenerator.Emit(OpCodes.Ret);

        eventBuilder.SetAddOnMethod(addMethodBuilder);
    }

    static void DefineRemoveMethodForEvent(
        TypeBuilder typeBuilder,
        Type eventHandlerType,
        FieldBuilder fieldBuilder,
        EventBuilder eventBuilder
    )
    {
        var removeEventMethod = string.Format("remove_{0}", PropertyChangedEventName)!;
        var removeMethodInfo = DelegateType.GetMethod(
            "Remove",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { DelegateType, DelegateType },
            null)!;

        var removeMethodBuilder = typeBuilder.DefineMethod(
            removeEventMethod,
            EventMethodAttributes,
            VoidType,
            new[] { eventHandlerType });

        var removeMethodGenerator = removeMethodBuilder.GetILGenerator();

        // TODO: Understand this IL
        removeMethodGenerator.Emit(OpCodes.Ldarg_0);
        removeMethodGenerator.Emit(OpCodes.Ldarg_0);
        removeMethodGenerator.Emit(OpCodes.Ldfld, fieldBuilder); // Find target field value of Ldarg_0.
        removeMethodGenerator.Emit(OpCodes.Ldarg_1);
        removeMethodGenerator.EmitCall(OpCodes.Call, removeMethodInfo, null);
        removeMethodGenerator.Emit(OpCodes.Castclass, eventHandlerType);
        removeMethodGenerator.Emit(OpCodes.Stfld, fieldBuilder);
        removeMethodGenerator.Emit(OpCodes.Ret);

        eventBuilder.SetRemoveOnMethod(removeMethodBuilder);
    }

    // 4. Add a method that handles the logic for when a property is changed.
    static MethodBuilder DefineOnPropertyChangedMethod(
        TypeBuilder typeBuilder,
        FieldBuilder propertyChangedFieldBuilder)
    {
        var onPropertyChangedMethodBuilder = typeBuilder.DefineMethod(
            OnPropertyChangedMethodName,
            OnPropertyChangedAttributes,
            VoidType,
            new[] { typeof(string) }
        );
        var onPropertyChangedMethodGenerator = onPropertyChangedMethodBuilder.GetILGenerator();

        var invokeMethod = typeof(PropertyChangedEventHandler)
            .GetMethod(nameof(PropertyChangedEventHandler.Invoke))!;

        var propertyChangedEventArgsType = typeof(PropertyChangedEventArgs);
        onPropertyChangedMethodGenerator.DeclareLocal(propertyChangedEventArgsType);

        /*
        if(propertyChanged is not null) {
            ...
        } else {
            (propertyChangedNullLabel)
        }
        */
        var propertyChangedNullLabel = onPropertyChangedMethodGenerator.DefineLabel();
        onPropertyChangedMethodGenerator.Emit(OpCodes.Ldnull);
        onPropertyChangedMethodGenerator.Emit(OpCodes.Ldarg_0);
        onPropertyChangedMethodGenerator.Emit(OpCodes.Ldfld, propertyChangedFieldBuilder);
        onPropertyChangedMethodGenerator.Emit(OpCodes.Ceq);
        onPropertyChangedMethodGenerator.Emit(OpCodes.Brtrue_S, propertyChangedNullLabel);


        // new PropertyChangedEventArgs(propertyName)
        onPropertyChangedMethodGenerator.Emit(OpCodes.Ldarg_1);
        onPropertyChangedMethodGenerator.Emit(
            OpCodes.Newobj,
            propertyChangedEventArgsType.GetConstructor(new[] { typeof(string) })!);
        onPropertyChangedMethodGenerator.Emit(OpCodes.Stloc_0);

        onPropertyChangedMethodGenerator.Emit(OpCodes.Ldarg_0);
        onPropertyChangedMethodGenerator.Emit(OpCodes.Ldfld, propertyChangedFieldBuilder);

        onPropertyChangedMethodGenerator.Emit(OpCodes.Ldarg_0);
        onPropertyChangedMethodGenerator.Emit(OpCodes.Ldloc_0);
        onPropertyChangedMethodGenerator.EmitCall(OpCodes.Callvirt, invokeMethod, null);
        onPropertyChangedMethodGenerator.MarkLabel(propertyChangedNullLabel);
        onPropertyChangedMethodGenerator.Emit(OpCodes.Ret);

        return onPropertyChangedMethodBuilder;
    }

    static string[] GetPropertiesToNotifyFor(PropertyInfo property)
    {
        var properties = new List<string>() {
            property.Name
        };

        foreach (var attribute in
            (NotifyChangesForAttribute[])property.GetCustomAttributes(typeof(NotifyChangesForAttribute), true))
        {
            properties.AddRange(attribute.PropertyNames);
        }
        return properties.ToArray();
    }

    // 5. Override any virtual methods and implement the code needed for notification when properties change.
    static void DefineProperties(TypeBuilder typeBuilder, Type baseType, MethodBuilder onPropertyChangedMethodBuilder)
    {
        var properties = baseType.GetProperties();
        var query = from p in properties
                    where p.GetGetMethod()!.IsVirtual && !p.GetGetMethod()!.IsFinal
                    select p;
        foreach (var property in query)
        {
            DefineGetMethodForProperty(property, typeBuilder);
            DefineSetMethodForProperty(property, typeBuilder, onPropertyChangedMethodBuilder);
        }
    }

    static void DefineSetMethodForProperty(
        PropertyInfo property,
        TypeBuilder typeBuilder,
        MethodBuilder onPropertyChangedMethodBuilder)
    {
        var setMethodToOverride = property.GetSetMethod();
        if (setMethodToOverride is null)
        {
            return;
        }

        var setMethodBuilder = typeBuilder.DefineMethod(
            setMethodToOverride.Name,
            setMethodToOverride.Attributes,
            VoidType,
            new[] { property.PropertyType });

        var setMethodGenerator = setMethodBuilder.GetILGenerator();
        var propertiesToNotifyFor = GetPropertiesToNotifyFor(property);

        setMethodGenerator.Emit(OpCodes.Ldarg_0);
        setMethodGenerator.Emit(OpCodes.Ldarg_1);
        setMethodGenerator.Emit(OpCodes.Call, setMethodToOverride);

        foreach (var propertyName in propertiesToNotifyFor)
        {
            setMethodGenerator.Emit(OpCodes.Ldarg_0);
            setMethodGenerator.Emit(OpCodes.Ldstr, propertyName);
            setMethodGenerator.Emit(OpCodes.Call, onPropertyChangedMethodBuilder);
        }

        setMethodGenerator.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(setMethodBuilder, setMethodToOverride);
    }

    static void DefineGetMethodForProperty(PropertyInfo property, TypeBuilder typeBuilder)
    {
        var getMethodToOverride = property.GetGetMethod()!;
        var getMethodBuilder = typeBuilder.DefineMethod(
            getMethodToOverride.Name, getMethodToOverride.Attributes, property.PropertyType, Array.Empty<Type>()
        );
        var getMethodGenerator = getMethodBuilder.GetILGenerator();
        var label = getMethodGenerator.DefineLabel();

        getMethodGenerator.DeclareLocal(property.PropertyType);
        getMethodGenerator.Emit(OpCodes.Ldarg_0);
        getMethodGenerator.Emit(OpCodes.Call, getMethodToOverride);
        getMethodGenerator.Emit(OpCodes.Stloc_0);
        getMethodGenerator.Emit(OpCodes.Br_S, label);
        getMethodGenerator.MarkLabel(label);
        getMethodGenerator.Emit(OpCodes.Ldloc_0);
        getMethodGenerator.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(getMethodBuilder, getMethodToOverride);
    }
}
