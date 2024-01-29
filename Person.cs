namespace DynamicProxyGenerator;

public class Person
{
    [NotifyChangesFor(nameof(FullName))]
    public virtual string FirstName { get; set; } = string.Empty;

    [NotifyChangesFor(nameof(FullName))]
    public virtual string SecondName { get; set; } = string.Empty;

    public virtual string FullName => $"{FirstName} {SecondName}";
}
