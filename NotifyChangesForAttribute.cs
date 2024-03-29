namespace DynamicProxyGenerator;

[AttributeUsage(AttributeTargets.Property)]
public class NotifyChangesForAttribute : Attribute
{
    public NotifyChangesForAttribute(params string[] propertyNames)
    {
        PropertyNames = propertyNames;
    }

    public string[] PropertyNames { get; }
}
