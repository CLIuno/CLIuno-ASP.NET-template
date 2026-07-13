using Microsoft.AspNetCore.Mvc;

namespace BackendASPNET.Support;

/// <summary>Builders for the shared CLIuno response envelope: {status, message, data}.</summary>
public static class Api
{
    public static ObjectResult Ok(string message, Dictionary<string, object?>? data = null) =>
        Envelope(200, "success", message, data);

    public static ObjectResult Created(string message, Dictionary<string, object?>? data = null) =>
        Envelope(201, "success", message, data);

    public static ObjectResult Error(int status, string message) =>
        Envelope(status, "error", message, null);

    public static Dictionary<string, object?> Data(params object?[] pairs)
    {
        var map = new Dictionary<string, object?>();
        for (var i = 0; i < pairs.Length; i += 2)
        {
            map[(string)pairs[i]!] = pairs[i + 1];
        }
        return map;
    }

    private static ObjectResult Envelope(
        int status, string state, string message, Dictionary<string, object?>? data)
    {
        var body = new Dictionary<string, object?> { ["status"] = state, ["message"] = message };
        if (data is not null)
        {
            body["data"] = data;
        }
        return new ObjectResult(body) { StatusCode = status };
    }
}

public class ApiException(int status, string message) : Exception(message)
{
    public int Status => status;
}
