using DemoApp.Services;
using System.CommandLine;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DemoApp.Commands;

public class RootCACommand : Command
{
    public RootCACommand(CountryCompletions countryCompletions) 
        : base("rootCA")
    {
        Description = "Create a self-signed root CA certificate.";

        var cnArg = new Argument<string>("CommonName");
        cnArg.HelpName = "Common Name";
        cnArg.Description = "Add a common name to the certificate's subject name.";
        this.AddArgument(cnArg);

        var ou = new Option<string[]>("--organizational-unit");
        ou.AddAlias("-ou");
        ou.Description = "Add one or more OUs to the certificate's subject name.";
        this.AddOption(ou);

        var o = new Option<string?>("--organization");
        o.AddAlias("-o");
        o.Description = "Add an Organization to the certificate's subject name.";
        this.AddOption(o);

        var c = new Option<string?>("--country");
        c.AddAlias("-c");
        c.Description = "Add a Country to the certificate's subject name.";
        c.AddCompletions(countryCompletions.CountryCodes);
        this.AddOption(c);

        var fp = new Option<string>("--file-path");
        fp.AddAlias("-fp");
        fp.Description = "Override the default export path for the root CA.";
        fp.SetDefaultValueFactory(
            // use the current path for the default value
            () => Path.Combine(Environment.CurrentDirectory, "rootca.pfx")
        );
        this.AddOption(fp);

        var nb = new Option<DateOnly?>("--not-before");
        nb.AddAlias("-nb");
        nb.Description = "Add a date for the certificate to become active on (UTC).";
        nb.SetDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow));
        this.AddOption(nb);

        var na = new Option<DateOnly?>("--not-after");
        na.AddAlias("-na");
        na.Description = "Add a date for the certificate to expire on (UTC).";
        nb.SetDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10)));
        this.AddOption(na);

        var rsa = new Option<int>("--rsa-size-in-bits");
        rsa.AddAlias("-rsa");
        rsa.Description = "Change the default RSA size (as measured in bits).";
        rsa.SetDefaultValue(2048);
        this.AddOption(rsa);


        this.SetHandler(ExecuteAsync, cnArg, ou, o, c, fp, nb, na, rsa);
    }

    async Task ExecuteAsync(
        string commonName,
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
            filePath = Path.Combine(cd, "root-ca.pfx");
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

            using (var cert = req.CreateSelfSigned(
                notBeforeDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                notAfterDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                )
            )
            {
                var pfx = cert.Export(X509ContentType.Pfx);

                await File.WriteAllBytesAsync(filePath, pfx);

                Console.WriteLine(filePath);
            }
        }
    }
}