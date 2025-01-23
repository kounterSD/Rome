using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Sockets;

public class P2P
{ 
    //true=Incoming TCP ConnectionRequest available, False=no incoming TCP Connections
    public bool pendingRequest;
    public Node peer;
    
    static int tcpListenPort = 63966;
    static int tcpConnectToPort = 63966;
    public void EstablishTCP(Node node)
    {
        TCPClient(node.ipaddress, tcpConnectToPort);
    }
    
    public void TCPListener()
    {
        try
        {
            TcpListener listener = new TcpListener(IPAddress.Any, tcpListenPort);
            listener.Start();
            Console.WriteLine($"TCP Socket listening on {listener.LocalEndpoint}...");

            while (true)
            {
                if (listener.Pending())
                {
                    //stops multicastListener
                    pendingRequest = true;
                    Thread.Sleep(1000);
                    Multicast.mlisten = false;
                    
                    
                
                    TcpClient client = listener.AcceptTcpClient();
                    IPEndPoint IncomConn = (IPEndPoint)client.Client.RemoteEndPoint;
                    peer = GetNodeByIP(IncomConn.Address);
                    //set the node as accepted for future
                    HandleClient(client, peer);
                }
               
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    //Handlng incoming client after recieving a connection 
    void HandleClient(TcpClient client, Node peer)
    {
        try
        {
            NetworkStream stream = client.GetStream();

            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"\n>{peer.alias}({peer.ipaddress}:{peer.port}): {receivedMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void TCPClient(IPAddress serverIP, int port)
    {
        try
        {
            while (true)
            {
                Console.Write("\n>You: ");
                string message = Console.ReadLine();

                TcpClient client = new TcpClient();
                client.Connect(serverIP, port);
                NetworkStream stream = client.GetStream();

                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
                Thread.Sleep(10);

                client.Close();
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
    }

    public Node SelectNode(List<Node> nodes)
    {
        try
        {
            Console.WriteLine("############\nSelect a node to establish connection: ");
            foreach (var node in nodes)
            {
                Console.WriteLine($"{nodes.IndexOf(node)}. {node.alias} - {node.ipaddress}");
            }
            Console.WriteLine("############");

            int option = int.Parse(Console.ReadLine());
        
            return nodes[option];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    Node GetNodeByIP(IPAddress ipAddress)
    {
        Node node = Multicast.discoveredNodes.FirstOrDefault(n => n.ipaddress.Equals(ipAddress));
        return node;
        
    }
}