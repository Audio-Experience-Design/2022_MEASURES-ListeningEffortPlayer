

using System;
using System.IO;
using System.Linq;

public static class LogUtilities
{
    /// <summary>
    /// Write all members of T to a line of the CSV file, using the names of T to define the CSV header if it's a fresh file
    /// </summary>
    public static void writeCSVLine<T>(StreamWriter writer, T logEntry) where T : struct
    {
        System.Reflection.PropertyInfo[] properties = typeof(T).GetProperties();
        string separator = ",";

        long bytesWritten = ((FileStream)writer.BaseStream).Length;
        if (bytesWritten == 0)
        {
            string[] headerLine = properties.Select(prop => prop.Name).ToArray();
            writer.WriteLine(string.Join(separator, headerLine));
        }

        string[] line = properties.Select(prop => ((string)prop.GetValue(logEntry) ?? "").Replace(",","_")).ToArray();
        writer.WriteLine(string.Join(separator, line));
        writer.Flush();
    }

    public static string localTimestamp()
    {
        return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");

    }
}