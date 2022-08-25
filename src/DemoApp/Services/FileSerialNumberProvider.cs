using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoApp.Services
{
    public class FileSerialNumberProvider : ISerialNumberProvider
    {
        string getDirectory()
        {
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
