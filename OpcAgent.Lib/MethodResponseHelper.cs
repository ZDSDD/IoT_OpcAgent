namespace OpcAgent.Lib;

using System.Text;
using System.Text.Json;
using Microsoft.Azure.Devices.Client;

public static class MethodResponseHelper
{
    public static MethodResponse CreateResponse(int status, object payload, int statusCode)
    {
        var responseContent = new
        {
            status,
            payload
        };

        string responseJson = JsonSerializer.Serialize(responseContent);
        byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
        
        return new MethodResponse(responseBytes, statusCode);
    }
}
