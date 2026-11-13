using System.Text.Json;
using AlertInspector;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: AlertInspector <alert-file>");
    return 1;
}

string filePath = args[0];
if (string.IsNullOrWhiteSpace(filePath))
{
    Console.Error.WriteLine("Alert file is required");
    return 1;
}

try
{
    Alert alert = readAlert(filePath);
    string name =  string.IsNullOrWhiteSpace(alert.Agent!.Name) 
        ? "unknown" 
        : alert.Agent.Name;
    Console.WriteLine($"alert={alert.Id} agent={alert.Agent!.Id} name={name} rule={alert.Rule!.Id} level={alert.Rule.Level}");
    return 0;
} 
catch(InvalidDataException e)
{
    Console.Error.WriteLine($"Invalid data: {e.Message}");
    return 1;
}
catch(JsonException e)
{
    Console.Error.WriteLine($"JSON exception: {e.Message}");
    return 1;
}
catch(IOException e)
{
    Console.Error.WriteLine($"IO exception: {e.Message}");
    return 1;
}
catch(Exception e)
{
    Console.Error.WriteLine($"Error: {e.Message}");
    return 1;
}



static Alert readAlert(string filename)
{
    string json = File.ReadAllText(filename);
    Alert alert = JsonSerializer.Deserialize<Alert>(json) ?? throw new Exception("The file must contain an alert object");
    if (string.IsNullOrWhiteSpace(alert.Id))
    {
        throw new InvalidDataException("Alert ID is required");
    }

    if (alert.Agent is null)
    {
        throw new InvalidDataException("Agent is required");    
    }

    if (string.IsNullOrWhiteSpace(alert.Agent.Id))
    {
        throw new InvalidDataException("Agent ID is required");    
    }

    if (alert.Rule is null)
    {
        throw new InvalidDataException("Rule is required");    
    }

    if (string.IsNullOrWhiteSpace(alert.Rule.Id))
    {
        throw new InvalidDataException("Rule ID is required");    
    }

    if (alert.Rule.Level is null)
    {
        throw new InvalidDataException("Rule level is required");    
    }

    return alert;
}