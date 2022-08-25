namespace DemoApp.Services
{
    public interface ISerialNumberProvider
    {
        byte[] GetNextSerialNumber();
        int? GetLastSerialNumber();
    }
}
