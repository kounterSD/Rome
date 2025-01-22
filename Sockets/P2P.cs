using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Sockets;

public class P2P
{
    static int tcpListenPort = 63966;
    static int tcpConnectToPort = 63966;
    public void EstablishTCP(Node node)
    {
        TCPClient(node.ipaddress, tcpConnectToPort);
    }
    
    public void TCPListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, tcpListenPort);
        listener.Start();
        Console.WriteLine($"TCP Socket listening on {listener.LocalEndpoint}...");

        while (true)
        {
            if (listener.Pending())
            {
                string userAns;
                TcpClient client = listener.AcceptTcpClient();
                IPEndPoint IncomConn = (IPEndPoint)client.Client.RemoteEndPoint;
                Node peer = GetNodeByIP(IncomConn.Address);

                if (peer.status == Node.ConnectionRequestStatus.Accepted)
                {
                    HandleClient(client, peer);
                }
                else
                {
                    Console.Write($"Would you like to accept incoming connection from:\n{IncomConn} (y/n)");
                    userAns = Console.ReadLine();
                    
                    if (userAns == "y")
                    {
                        //set the node as accepted for future
                        Multicast.discoveredNodes[Multicast.discoveredNodes.IndexOf(peer)].status = Node.ConnectionRequestStatus.Accepted;
                        HandleClient(client, peer);
                    }

                    if (userAns == "n")
                    {
                        Console.WriteLine("Closing connection");
                        client.Close();
                        
                    }
                }
               
            }
            
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
                Console.WriteLine($">{peer.alias}({peer.ipaddress}:{peer.port}): {receivedMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void TCPClient(IPAddress serverIP, int port)
    {
        while (true)
        {
            Console.Write(">You:");
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

    public Node SelectNode(List<Node> nodes)
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

    Node GetNodeByIP(IPAddress ipAddress)
    {
        Node node = Multicast.discoveredNodes.FirstOrDefault(n => n.ipaddress.Equals(ipAddress));
        return node;
        
    }
}