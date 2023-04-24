using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Net.Sockets;

public class DisplayOSCIPAddress : MonoBehaviour
{
    public Text targetText;

    // Start is called before the first frame update
    void Start()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        string ipAddresses = Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
            .Where(ip => !(ip.ToString() == "127.0.0.1"))
            .Select(ip => ip.ToString())
            .Select(ipAddresses => $"{ipAddresses}:{OSCController.listenPort}")
            .Aggregate((head, tail) => $"{head}, {tail}");

        targetText.text = targetText.text
            .Replace("{IP_ADDRESS}", $"{ipAddresses}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
