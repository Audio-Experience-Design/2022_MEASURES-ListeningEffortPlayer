using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    public TMPro.TMP_Text loadingText;
    public OSCController oscController;

    public void Awake()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        string ipAddresses = Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
            .Where(ip => !(ip.ToString() == "127.0.0.1"))
            .Select(ip => ip.ToString())
            .Select(ipAddresses => $"{ipAddresses}:{oscController.listenPort}")
            .Aggregate((head, tail) => $"{head}\n{tail}");

        loadingText.text = loadingText.text.Replace("{IP_ADDRESS}", $"{ipAddresses}");
    }

}
