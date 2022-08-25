using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DemoApp.Services;

public class SSLCertificateGenerator
{
    ISerialNumberProvider _serialNumberProvider;
    public SSLCertificateGenerator(ISerialNumberProvider serialNumberProvider)
    {
        _serialNumberProvider = serialNumberProvider;
    }

    public async Task GenerateSslCertAsync(
        string commonName,
        string issuerFilePath,
        string[] DNSNames,
        byte[] IPAddresses,
        string[] OUs,
        string? organization = null,
        string? country = null,
        string? public_filePath = null,
        string? private_filePath = null,
        DateOnly? notBeforeDate = null,
        DateOnly? notAfterDate = null,
        int rsaSizeInBits = 2048)
    {
        if (notBeforeDate is null)
            notBeforeDate = DateOnly.FromDateTime(DateTime.UtcNow);
        if (notAfterDate is null)
            notAfterDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(5));

        if (public_filePath is null)
        {
            var cd = Environment.CurrentDirectory;
            public_filePath = Path.Combine(cd, "intermediate-ca.pub.pfx");
        }
        else
        {
            var directory = Path.GetDirectoryName(public_filePath);
            if (!Directory.Exists(directory))
                Console.WriteLine($"Directory \"{directory}\" does not exist.");
            return;
        }

        if(private_filePath is null)
        {
            var cd = Environment.CurrentDirectory;
            private_filePath = Path.Combine(cd, "intermediate-ca.prv.pfx");
        }
        else
        {
            var directory = Path.GetDirectoryName(private_filePath);
            if (!Directory.Exists(directory))
                Console.WriteLine($"Directory \"{directory}\" does not exist.");
            return;
        }

        if (public_filePath == private_filePath)
        {
            Console.WriteLine("Public certificate path and private certificate path cannot be the same");
            return;
        }

        if (!File.Exists(issuerFilePath))
        {
            Console.WriteLine($"File path to the issuer certificate does not exist.");
            return;
        }

        var issuerCABytes = await File.ReadAllBytesAsync(issuerFilePath);
        var issuerCA = new X509Certificate2(issuerCABytes);
        if (!issuerCA.HasPrivateKey)
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

            var SAN = new SubjectAlternativeNameBuilder();
            foreach (var dnsName in DNSNames)
                SAN.AddDnsName(dnsName);
            foreach (var ipaddress in IPAddresses)
                SAN.AddIpAddress(new IPAddress(ipaddress));
            req.CertificateExtensions.Add(SAN.Build());

            var serial = _serialNumberProvider.GetNextSerialNumber();

            using (var cert = req.Create(issuerCA, 
                notBeforeDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), 
                notAfterDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), 
                serial))
            {
                var privateCert = cert.CopyWithPrivateKey(rsa);

                var privatePfx = privateCert.Export(X509ContentType.Pfx);
                var pfx = cert.Export(X509ContentType.Pfx);

                var task1 = File.WriteAllBytesAsync(private_filePath, privatePfx);
                var task2 = File.WriteAllBytesAsync(public_filePath, pfx);
                await Task.WhenAll(task1, task2);
            }
        }
    }
}
