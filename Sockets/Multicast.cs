using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Sockets;

public class Multicast
{
    P2P p2p = new P2P();
    
    string multicastAddress = "224.3.69.69";
    int multicastPort = 8999;
    //Dynamic List: Currently Online Nodes
    List<Node> onlineNodes = new List<Node>();
    //List of all discovered Nodes in this session
    public static List<Node> discoveredNodes = new List<Node>();
    
    
    //on/off switch for Multicast Listener
    public static bool mlisten;
    
    //Node discovery, and subsequent node funneling
    public void StartMulticastListener(String mode, CancellationToken cancellationToken)
    {
        try 
        { 
            mlisten = true;
            using (UdpClient listener = new UdpClient(multicastPort))
            {
                IPAddress multicastGroup = IPAddress.Parse(multicastAddress);
                listener.JoinMulticastGroup(multicastGroup);
                //avoids listening to your own packets// doesnt work in MacOS or Linux..works in Windows
                listener.Client.MulticastLoopback = false;
                //to stop the listener
                var stopMultiListener = Task.Run(() => CloseMulticastListener(listener), cancellationToken);
        
                Console.WriteLine($"Listening for ENDPOINT DISCOVERY messages on {listener.Client.LocalEndPoint}");
        
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);


                while (mlisten)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine($"Stopping multicast listener");
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    onlineNodes.Clear();
            
                    //listening for DISCOVERY REQ for 5 seconds
                    DateTime startTime = DateTime.Now;
                    while ((DateTime.Now - startTime).TotalSeconds < 5)
                    {
                        //LOOP for 5s until a DISCOVERY is recieved.
                        byte[] receivedBytes = listener.Receive(ref remoteEndPoint); 
                        string receivedMessage = Encoding.UTF8.GetString(receivedBytes); 
                
                        if (receivedMessage.Contains("DISCOVERY_REQUEST")) 
                        { 
                            string alias = GetAlias(receivedMessage);  
                            int peerPort = remoteEndPoint.Port;
                            Node peer = new Node(remoteEndPoint.Address, alias, peerPort, Node.ConnectionRequestStatus.Refused);

                            //if node list does not have the peer then add.
                            AddUniqueNode(peer, discoveredNodes);
                            AddUniqueNode(peer, onlineNodes);
                        }
                    }
            
                    Console.WriteLine($"-------------\nLast 5 seconds: {onlineNodes.Count} nodes available: ");
                    DisplayNodes(onlineNodes);
                    Console.WriteLine("Press Enter to stop the listener...");
                }
            
                //If Active Beacon mode --> Establish p2p TCP connection.
                if (mode == "active")
                {
                    var initiateConn = Task.Run(() =>
                    {
                        //Asks user for input: which node to estalish conenction with
                        Node node = p2p.SelectNode(onlineNodes);
                        //accept connection when they reply.
                        discoveredNodes[discoveredNodes.IndexOf(p2p.GetNodeByIP(node.ipaddress))].status = Node.ConnectionRequestStatus.Accepted;
                        //establishes connection with the selected node.
                        p2p.EstablishTCP(node);
                    }, cancellationToken);
                    while (true)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine($"Stopping multicast listener");
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                
                }
            } 
        }
            
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    //bg method to close Multicast Listener
    public void CloseMulticastListener(UdpClient udpClient)
    {
        Console.WriteLine("Press Enter to stop the listener...");
        Console.ReadLine();
        mlisten = false;
        Thread.Sleep(5000);
        udpClient.Close();
    }
    
    //Starts sending MULTICAST_DISCOVERY_REQUESTS
    public void StartMulticastBroadcaster(string alias)
    {
        bool endMulticast = false;
        
        using (UdpClient broadcaster = new UdpClient())
        {
            IPAddress multicastGroup = IPAddress.Parse(multicastAddress);
            IPEndPoint multicastEndpoint = new IPEndPoint(multicastGroup, multicastPort);
            int i = 1;
            
            while (!endMulticast)
            {
                //DISCOVERY_REQUEST + Alias(Identifier)
                string message = $":: {i} ::\nDISCOVERY_REQUEST\nALIAS:{alias}";
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                broadcaster.Send(messageBytes, messageBytes.Length, multicastEndpoint);
                //sleep 3 second before sending DISCOVERY_REQUEST
                Thread.Sleep(3000);
                i++;
            }
        }
    }
    
    //Extract ALIAS from Discovery Message
    string GetAlias(string message)
    {
        string key = "ALIAS:";
        int start = message.IndexOf(key);
        start += key.Length; // Move index to start of alias value
        
        int endIndex = message.IndexOf('\n', start);
        
        if (endIndex == -1)
            return message.Substring(start).Trim(); // Extract till end if no newline
        
        return message.Substring(start, endIndex - start).Trim(); // Extract alias
    }
    
    //Displays nodes in a list
    void DisplayNodes(List<Node> nodes)
    {
        foreach (Node node in nodes)
        {
            Console.WriteLine($"{nodes.IndexOf(node)}. {node.alias} : {node.ipaddress}:{node.port}");
        }
        Console.WriteLine("-------------");
    }
    
    //Uniquely adds given node to the given list(Comparing )
    void AddUniqueNode(Node node, List<Node> nodes)
    {
        if (!nodes.Any(n => n.ipaddress.Equals(node.ipaddress) && n.alias.Equals(node.alias))) 
        { 
            nodes.Add(node);
        }
    }
}
