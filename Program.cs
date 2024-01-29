using System.ComponentModel;
using DynamicProxyGenerator;

var type = NotifyingObjectWeaver.GetProxyType(typeof(Person));
Console.WriteLine($"Type name: {type}");

var instance = (Activator.CreateInstance(type) as INotifyPropertyChanged)!;

instance.PropertyChanged += (sender, e) =>
{
    Console.WriteLine($"Property changed: {e.PropertyName}");
};

var instanceAsViewModel = (instance as Person)!;
instanceAsViewModel.FirstName = "John";
