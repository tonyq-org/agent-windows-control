using System.Text.Json;
using AgentWindowsControl.Core;

var options = new JsonSerializerOptions { WriteIndented = true };

if (args is ["tools"])
{
    Console.WriteLine(JsonSerializer.Serialize(ToolCatalog.All, options));
    return 0;
}

if (args is ["license"])
{
    Console.WriteLine(JsonSerializer.Serialize(Licensing.LibreAutomate, options));
    return 0;
}

Console.WriteLine("""
agent-windows-control

Usage:
  awc tools     List planned automation tools as JSON.
  awc license   Print LibreAutomate license compatibility notes as JSON.
""");
return 0;
