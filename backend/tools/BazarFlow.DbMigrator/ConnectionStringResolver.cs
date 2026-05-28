namespace BazarFlow.DbMigrator;

public sealed class ConnectionStringResolver
{
    public string? Resolve(string[] args)
    {
        return Resolve(args, Environment.GetEnvironmentVariable);
    }

    public string? Resolve(string[] args, Func<string, string?> getEnvironmentVariable)
    {
        var fromArgs = ResolveFromArgs(args);
        if (!string.IsNullOrWhiteSpace(fromArgs))
        {
            return fromArgs;
        }

        var defaultConnection = getEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(defaultConnection))
        {
            return defaultConnection;
        }

        var bazarFlowConnection = getEnvironmentVariable("BAZARFLOW_CONNECTION_STRING");
        return string.IsNullOrWhiteSpace(bazarFlowConnection) ? null : bazarFlowConnection;
    }

    private static string? ResolveFromArgs(string[] args)
    {
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (string.Equals(arg, "--connection", StringComparison.OrdinalIgnoreCase))
            {
                return index + 1 < args.Length ? args[index + 1] : null;
            }

            const string prefix = "--connection=";
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return arg[prefix.Length..];
            }
        }

        return null;
    }
}
