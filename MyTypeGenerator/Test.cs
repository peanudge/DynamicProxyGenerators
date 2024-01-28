using DynamicProxyGenerator;

public static class Test
{
    public static void Run()
    {
        // Generate Type in runtime.
        var myType = MyTypeGenerator.Generate();
        // Search target method from generated type.
        var method = myType.GetMethod("SaySomething")!;
        // Create instance about type.
        var myTypeInstance = Activator.CreateInstance(myType);
        // Call target method with searched instance.
        method.Invoke(myTypeInstance, new object[] { "Hello world", 3 });


        Console.WriteLine(myTypeInstance);

    }
}
