using DemoApp.Services;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DemoApp.Commands;

public class IntermediateCACommand : Command
{
    ISerialNumberProvider serialNumberProvider;

    public IntermediateCACommand(ISerialNumberProvider serialNumberProvider, CountryCompletions countryCompletions)
        :base("intermediateCA")
    {
        this.serialNumberProvider = serialNumberProvider;

        Description = "Create an intermediate CA certificate";

        var cnArg = new Argument<string>("CommonName");
        cnArg.HelpName = "Common Name";
        cnArg.Description = "Add a common name to the intermediate certificate's subject name.";
        this.AddArgument(cnArg);

        var issuerArg = new Argument<string>("IssuerFilePath");
        issuerArg.HelpName = "Issuer File Path";
        issuerArg.Description = "Add the file path to the Issuer Root CA.";
        this.AddArgument(issuerArg);

        var ouOption = new Option<string[]>("--organizational-unit");
        ouOption.AddAlias("-ou");
        ouOption.Description = "Add one or more OUs to the intermediate certificate's subject name.";
        ouOption.SetDefaultValue(Array.Empty<string>());
        this.AddOption(ouOption);

        var oOption = new Option<string?>("--organization");
        oOption.AddAlias("-o");
        oOption.Description = "Add an Organization to the intermediate certificate's subject name.";
        this.AddOption(oOption);

        var cOption = new Option<string?>("--country");
        cOption.AddAlias("-c");
        cOption.Description = "Add a Country to the intermediate certificate's subject name.";
        cOption.AddCompletions(countryCompletions.CountryCodes);
        this.AddOption(cOption);

        var fpOption = new Option<string>("--file-path");
        fpOption.AddAlias("-fp");
        fpOption.Description = "Override the default export path for the intermediate CA.";
        fpOption.SetDefaultValueFactory(
            // use the current path for the default value
            () => Path.Combine(Environment.CurrentDirectory, "intermediateca.pfx")
        );
        this.AddOption(fpOption);

        var nbOption = new Option<DateOnly?>("--not-before");
        nbOption.AddAlias("-nb");
        nbOption.Description = "Add a date for the intermediate certificate to become active on (UTC).";
        nbOption.SetDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow));
        this.AddOption(nbOption);

        var naOption = new Option<DateOnly?>("--not-after");
        naOption.AddAlias("-na");
        naOption.Description = "Add a date for the intermediate certificate to expire on (UTC).";
        nbOption.SetDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10)));
        this.AddOption(naOption);

        var rsaOption = new Option<int>("--rsa-size-in-bits");
        rsaOption.AddAlias("-rsa");
        rsaOption.Description = "Change the default RSA size for the intermediate certificate (as measured in bits).";
        rsaOption.SetDefaultValue(2048);
        this.AddOption(rsaOption);

        // no overload for set handler takes ten args, must set invoke myself, 
        //   TODO: research using source generators to do this work for me
        this.SetHandler((context) =>
        {
            var cn = context.ParseResult.GetValueForArgument(cnArg);
            var issuer = context.ParseResult.GetValueForArgument(issuerArg);
            var ou = context.ParseResult.GetValueForOption(ouOption);
            var o = context.ParseResult.GetValueForOption(oOption);
            var c = context.ParseResult.GetValueForOption(cOption);
            var fp = context.ParseResult.GetValueForOption(fpOption);
            var nb = context.ParseResult.GetValueForOption(nbOption);
            var na = context.ParseResult.GetValueForOption(naOption);
            var rsa = context.ParseResult.GetValueForOption(rsaOption);
            return ExecuteAsync(cn, issuer, ou, o, c, fp, nb, na, rsa);
        });
    }


    public async Task ExecuteAsync(
        string commonName,
        string issuerFilePath,
        string[]? OUs = null,
        string? organization = null,
        string? country = null,
        string? filePath = null,
        DateOnly? notBeforeDate = null,
        DateOnly? notAfterDate = null,
        int rsaSizeInBits = 2048)
    {
        if (OUs is null)
            OUs = Array.Empty<string>();

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
            {
                Console.WriteLine($"Directory \"{directory}\" does not exist.");
                return;
            }
        }

        if (!File.Exists(issuerFilePath))
        {
            Console.WriteLine($"File path to the issuer certificate does not exist.");
            return;
        }

        var rootCABytes = await File.ReadAllBytesAsync(issuerFilePath);
        var rootCA = new X509Certificate2(rootCABytes);
        if (!rootCA.HasPrivateKey)
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
                new X509BasicConstraintsExtension(true, false, 0, true)
            );

            var serial = serialNumberProvider.GetNextSerialNumber();

            using (var cert = req.Create(rootCA,
                notBeforeDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                notAfterDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                serial))
            {
                var certWithPrivate = cert.CopyWithPrivateKey(rsa);

                var pfx = certWithPrivate.Export(X509ContentType.Pfx);

                await File.WriteAllBytesAsync(filePath, pfx);

                Console.WriteLine(filePath);
            }
        }
    }
}
