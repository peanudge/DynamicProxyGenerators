namespace DynamicProxyGenerator;

public static class NameHelper
{
    public static string CreateUniqueName(string prefix)
    {
        var uid = Guid.NewGuid().ToString();
        uid = uid.Replace('-', '_');
        return $"{prefix}{uid}";
    }

}
