namespace OpcAgent.Lib;

public static class DeviceTwinPropertyParser
{
    public static double ConvertSendFrequencyToSeconds(string sendFrequency)
    {
        if (sendFrequency.EndsWith("s"))
        {
            // If the sendFrequency is in seconds
            if (double.TryParse(sendFrequency.Replace("s", ""), out double seconds))
            {
                return seconds;
            }
        }
        else if (sendFrequency.EndsWith("m"))
        {
            // If the sendFrequency is in minutes
            if (double.TryParse(sendFrequency.Replace("m", ""), out double minutes))
            {
                // Convert minutes to seconds
                return minutes * 60;
            }
        }
        else if (sendFrequency.EndsWith("h"))
        {
            // If the sendFrequency is in hours
            if (double.TryParse(sendFrequency.Replace("h", ""), out double hours))
            {
                // Convert hours to seconds
                return hours * 3600;
            }
        }

        // If the format of sendFrequency is not recognized, return a default value or throw an exception
        // You can decide the appropriate action based on your requirements
        throw new ArgumentException("Invalid sendFrequency format.");
    }
} 