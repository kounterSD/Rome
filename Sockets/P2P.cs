using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Sockets;

public class P2P
{
    private readonly CancellationTokenSource cTokenSource;

    public P2P(CancellationTokenSource _cts)
    {
       cTokenSource = _cts;
    }
    public P2P(){}
    
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
                //if there is an incomming connection
                if (listener.Pending())
                {
                    TcpClient client = listener.AcceptTcpClient();
                    IPEndPoint IncomConn = (IPEndPoint)client.Client.RemoteEndPoint;
                    peer = GetNodeByIP(IncomConn.Address);
                    Task.Run(()=>HandleIncommingReq(client, peer));
                }
               
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    public void HandleIncommingReq(TcpClient client, Node node)
    {
        if (PeerAllowed(node))
        {
            HandleClient(client, node);
            EstablishTCP(node);
        }

        if (!PeerAllowed(node))
        {
            Console.WriteLine($"Incomming Request from: {node.alias}:{node.ipaddress}\nWould you like to accept?(y/n)");
            string userInput = Console.ReadLine();
            if (userInput == "y")
            {
                Console.WriteLine($"Accepted Request from: {node.alias}:{node.ipaddress}");
                //mark node status as accepted for future connections
                Multicast.discoveredNodes[Multicast.discoveredNodes.IndexOf(GetNodeByIP(node.ipaddress))].status = Node.ConnectionRequestStatus.Accepted;
                RequestMultiListenerCancellation();
                HandleClient(client, node);
                EstablishTCP(node);
            }

            if (userInput == "n")
            {
                Console.WriteLine($"Connection refused");
            }
            
        }
       
    }

    public void RequestMultiListenerCancellation()
    {
        cTokenSource.Cancel();
    }

    public bool PeerAllowed(Node node)
    {
        Node n = GetNodeByIP(node.ipaddress);
        if (n.status == Node.ConnectionRequestStatus.Accepted)
        {
            return true;
        }
        return false;
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

    public Node GetNodeByIP(IPAddress ipAddress)
    {
        Node node = Multicast.discoveredNodes.FirstOrDefault(n => n.ipaddress.Equals(ipAddress));
        return node;
        
    }
}