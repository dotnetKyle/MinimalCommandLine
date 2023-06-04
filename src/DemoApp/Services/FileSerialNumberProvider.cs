namespace DemoApp.Services
{
    public class FileSerialNumberProvider : ISerialNumberProvider
    {
        string getDirectory()
        {
            // TODO: this call will probably only work in Windows, use Environment variables to get the Home path and use that instead
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var appDirectory = Path.Combine(appData, "DemoApp");

            if(!Directory.Exists(appDirectory))
                Directory.CreateDirectory(appDirectory);

            return appDirectory;
        }
        
        string getFileName()
        {
            var directory = getDirectory();
            
            var fileName = Path.Combine(directory, ".serial");
            
            return fileName;
        }

        public byte[] GetNextSerialNumber()
        {
            var lastSerial = GetLastSerialNumber();

            if (lastSerial is null)
                lastSerial = 0;

            var nextSerial = lastSerial.Value + 1;

            File.WriteAllText(getFileName(), nextSerial.ToString());

            return BitConverter.GetBytes(nextSerial);
        }

        public int? GetLastSerialNumber()
        {
            var fileName = getFileName();

            if (File.Exists(fileName))
            {
                var lastSerialText = File.ReadAllText(fileName);

                if (int.TryParse(lastSerialText, out var lastSerial))
                    return lastSerial;
                else
                    return null;
            }
            else
            {
                return null;
            }
        }
    }
}
