using DemoApp.Services;
using System.CommandLine;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DemoApp.Commands;

public class SslCertificateCommand : Command
{
    ISerialNumberProvider serialNumberProvider;

    public SslCertificateCommand(ISerialNumberProvider serialNumberProvider, CountryCompletions countryCompletions)
        :base("ssl")
    {
        this.serialNumberProvider = serialNumberProvider;

        Description = "Create an SSL certificate.";


        var cnArg = new Argument<string>("CommonName");
        cnArg.HelpName = "Common Name";
        cnArg.Description = "Add a common name to the SSL certificate's subject name.";
        this.AddArgument(cnArg);

        var issuerArg = new Argument<string>("IssuerFilePath");
        issuerArg.HelpName = "Issuer File Path";
        issuerArg.Description = "Add the file path to the Issuer CA (intermediate or root).";
        this.AddArgument(issuerArg);

        var dnsOption = new Option<string[]>("--dns-name");
        dnsOption.AddAlias("-dns");
        dnsOption.Description = "Add one or more DNS names.";
        dnsOption.SetDefaultValue(Array.Empty<string>());
        this.AddOption(dnsOption);

        var ipOption = new Option<string[]>("--ip-address");
        ipOption.AddAlias("-ip");
        ipOption.Description = "Add one or more IP Addresses.";
        ipOption.SetDefaultValue(Array.Empty<string>());
        this.AddOption(ipOption);

        var ouOption = new Option<string[]>("--organizational-unit");
        ouOption.AddAlias("-ou");
        ouOption.Description = "Add one or more OUs to the SSL certificate's subject name.";
        ouOption.SetDefaultValue(Array.Empty<string>());
        this.AddOption(ouOption);

        var oOption = new Option<string?>("--organization");
        oOption.AddAlias("-o");
        oOption.Description = "Add an Organization to the SSL certificate's subject name.";
        this.AddOption(oOption);

        var cOption = new Option<string?>("--country");
        cOption.AddAlias("-c");
        cOption.Description = "Add a Country to the SSL certificate's subject name.";
        cOption.AddCompletions(countryCompletions.CountryCodes);
        this.AddOption(cOption);

        var publicFpOption = new Option<string>("--public-file-path");
        publicFpOption.AddAlias("-pub");
        publicFpOption.Description = "Override the default export path for the public certificate.";
        publicFpOption.SetDefaultValueFactory(
            // use the current path for the default value
            () => Path.Combine(Environment.CurrentDirectory, "public-ssl-cert.pfx")
        );
        this.AddOption(publicFpOption);

        var privateFpOption = new Option<string>("--private-file-path");
        privateFpOption.AddAlias("-prv");
        privateFpOption.Description = "Override the default export path for the private certificate.";
        privateFpOption.SetDefaultValueFactory(
            // use the current path for the default value
            () => Path.Combine(Environment.CurrentDirectory, "private-ssl-cert.pfx")
        );
        this.AddOption(privateFpOption);

        var nbOption = new Option<DateOnly?>("--not-before");
        nbOption.AddAlias("-nb");
        nbOption.Description = "Add a date for the SSL certificate to become active on (UTC).";
        nbOption.SetDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow));
        this.AddOption(nbOption);

        var naOption = new Option<DateOnly?>("--not-after");
        naOption.AddAlias("-na");
        naOption.Description = "Add a date for the SSL certificate to expire on (UTC).";
        nbOption.SetDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10)));
        this.AddOption(naOption);

        var rsaOption = new Option<int>("--rsa-size-in-bits");
        rsaOption.AddAlias("-rsa");
        rsaOption.Description = "Change the default RSA size for the SSL certificate (as measured in bits).";
        rsaOption.SetDefaultValue(2048);
        this.AddOption(rsaOption);

        // no overload for set handler takes ten args, must set invoke myself, 
        //   TODO: research using source generators to do this work for me
        this.SetHandler((context) =>
        {
            var cn = context.ParseResult.GetValueForArgument(cnArg);
            var issuer = context.ParseResult.GetValueForArgument(issuerArg);
            var dns = context.ParseResult.GetValueForOption(dnsOption);
            var ips = context.ParseResult.GetValueForOption(ipOption);
            var ou = context.ParseResult.GetValueForOption(ouOption);
            var o = context.ParseResult.GetValueForOption(oOption);
            var c = context.ParseResult.GetValueForOption(cOption);
            var privateFp = context.ParseResult.GetValueForOption(privateFpOption);
            var publicFp = context.ParseResult.GetValueForOption(publicFpOption);
            var nb = context.ParseResult.GetValueForOption(nbOption);
            var na = context.ParseResult.GetValueForOption(naOption);
            var rsa = context.ParseResult.GetValueForOption(rsaOption);

            return ExecuteAsync(
                commonName:cn, 
                issuerFilePath:issuer, 
                DNSNames:dns!, 
                IPAddresses:ips!, 
                OUs:ou!,
                organization:o,
                country:c, 
                public_filePath: publicFp,
                private_filePath: privateFp,
                notBeforeDate: nb,
                notAfterDate: na,
                rsaSizeInBits: rsa
            );
        });
    }

    public async Task ExecuteAsync(
        string commonName,
        string issuerFilePath,
        string[] DNSNames,
        string[] IPAddresses,
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
            {
                Console.WriteLine($"Directory \"{directory}\" does not exist.");
                return;
            }
        }

        if (private_filePath is null)
        {
            var cd = Environment.CurrentDirectory;
            private_filePath = Path.Combine(cd, "intermediate-ca.prv.pfx");
        }
        else
        {
            var directory = Path.GetDirectoryName(private_filePath);
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"Directory \"{directory}\" does not exist.");
                return;
            }
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

            req.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false)
            );

            var SAN = new SubjectAlternativeNameBuilder();
            foreach (var dnsName in DNSNames)
                SAN.AddDnsName(dnsName);
            foreach (var ipaddress in IPAddresses)
            {
                var parts = ipaddress.Split('.');
                var bytes = new List<byte>();
                foreach (var b in parts)
                {
                    if (byte.TryParse(b, out var byt))
                        bytes.Add(byt);
                    else
                        throw new FormatException("IP Address is not in the correct format.");
                }

                if (bytes.Count != 4)
                    throw new FormatException("IP Address is not in the correct format.");

                SAN.AddIpAddress(new IPAddress(bytes.ToArray()));
            }
            req.CertificateExtensions.Add(SAN.Build());

            var serial = serialNumberProvider.GetNextSerialNumber();

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

                Console.WriteLine(private_filePath);
                Console.WriteLine(public_filePath);
            }
        }
    }
}
