using System.Net.Mime;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace OpcAgent.Lib;

public static class MessageService
{
    internal static Message PrepareMessage(object data)
    {
        var dataString = JsonConvert.SerializeObject(data);
        Message message = new Message(Encoding.UTF8.GetBytes(dataString));
        message.ContentType = MediaTypeNames.Application.Json;
        message.ContentEncoding = "utf-8";
        return message;
    }
}