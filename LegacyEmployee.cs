using System.ComponentModel;
public class Employee : INotifyPropertyChanged
{
    private string _firstName;
    public string FirstName
    {
        get { return _firstName; }
        set
        {
            _firstName = value;
            RaisePropertyChanged("FirstName");
        }
    }
    public event PropertyChangedEventHandler PropertyChanged;

    protected void RaisePropertyChanged(string
      propertyName)
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
