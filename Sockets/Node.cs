using System.Net;

namespace Sockets;

public class Node
{ 
    public IPAddress ipaddress;
    public int port;
    public string alias;
    public enum ConnectionRequestStatus
    {
        Accepted,
        Refused,
    };

    public Node(IPAddress ip, string alias, int port)
    {
        this.ipaddress = ip;
        this.alias = alias;
        this.port = port;
    }
}