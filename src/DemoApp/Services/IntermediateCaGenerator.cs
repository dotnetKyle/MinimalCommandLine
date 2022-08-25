using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DemoApp.Services;

public class IntermediateCaGenerator
{
    ISerialNumberProvider _serialNumberProvider;
    public IntermediateCaGenerator(ISerialNumberProvider serialNumberProvider)
    {
        _serialNumberProvider = serialNumberProvider;
    }

    public async Task GenerateCaAsync(
        string commonName,
        string issuerFilePath,
        string[] OUs,
        string? organization = null,
        string? country = null,
        string? filePath = null,
        DateOnly? notBeforeDate = null,
        DateOnly? notAfterDate = null,
        int rsaSizeInBits = 2048)
    {
        if (notBeforeDate is null)
            notBeforeDate = DateOnly.FromDateTime(DateTime.UtcNow);
        if (notAfterDate is null)
            notAfterDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(5));

        if (filePath is null)
        {
            var cd = Environment.CurrentDirectory;
            filePath = Path.Combine(cd, "intermediate-ca.pfx");
        }
        else
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                Console.WriteLine($"Directory \"{directory}\" does not exist.");
            return;
        }

        if(!File.Exists(issuerFilePath))
        {
            Console.WriteLine($"File path to the issuer certificate does not exist.");
            return;
        }

        var rootCABytes = await File.ReadAllBytesAsync(issuerFilePath);
        var rootCA = new X509Certificate2(rootCABytes);
        if(!rootCA.HasPrivateKey)
        {
            Console.WriteLine($"The issuer does not have a private key for issuing certificates.");
            return;
        }

        var subjectName = $"CN={commonName}";

        foreach (var ou in OUs)
            subjectName += $", OU={ou}";
        if (organization is not null)
            subjectName += $", O={organization}";
        if (country is not null)
            subjectName += $", C={country}";

        using (var rsa = RSA.Create(rsaSizeInBits))
        {
            var req = new CertificateRequest(
                subjectName,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var serial = _serialNumberProvider.GetNextSerialNumber();

            using (var cert = req.Create(rootCA,
                notBeforeDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                notAfterDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                serial))
            {
                var pfx = cert.Export(X509ContentType.Pfx);

                await File.WriteAllBytesAsync(filePath, pfx);
            }
        }
    }
}
