using System.Runtime.CompilerServices;

namespace IntegrationTests;

public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
        // Teaches Verify how to serialise HttpResponseMessage, so a test can
        // verify status, headers and body as one artifact rather than a
        // hand-written assertion per field.
        VerifyHttp.Initialize();

        // Snapshots live in a Snapshots folder beside the test file rather than
        // scattered next to it, so the Features directory stays readable as the
        // suite grows.
        Verifier.DerivePathInfo(
            (sourceFile, _, type, method) => new(
                directory: Path.Combine(Path.GetDirectoryName(sourceFile)!, "Snapshots"),
                typeName: type.Name,
                methodName: method.Name));
    }
}
