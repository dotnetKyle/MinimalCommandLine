using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DemoApp.Services;

public class RootCaGenerator
{
	public async Task GenerateRootCaAsync(
		string commonName,
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
			filePath = Path.Combine(cd, "root-ca.pfx");
		}
		else
		{
			var directory = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(directory))
				Console.WriteLine($"Directory \"{directory}\" does not exist.");			
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

			using (var cert = req.CreateSelfSigned(
                notBeforeDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                notAfterDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
				)
			)
			{
				var pfx = cert.Export(X509ContentType.Pfx);

				await File.WriteAllBytesAsync(filePath, pfx);
			}
		}
	}
}
