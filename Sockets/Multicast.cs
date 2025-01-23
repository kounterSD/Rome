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
    //Dynamic currently online Nodes list
    List<Node> onlineNodes = new List<Node>();
    //List of all discovered Nodes in this session
    public static List<Node> discoveredNodes = new List<Node>();
    
    //on/off switch for Multicast Listener
    public static bool mlisten = false;
    
    public void StartMulticastListener(String mode)
    {
        try
        { 
            mlisten = true;
            UdpClient listener = new UdpClient(multicastPort);
            IPAddress multicastGroup = IPAddress.Parse(multicastAddress);
            listener.JoinMulticastGroup(multicastGroup);
            //avoids listening to your own packets// doesnt work in MacOS/Linux
            listener.Client.MulticastLoopback = false;
        
            //to stop the listener
            Thread stopMultiListener = new Thread(() => StopListener(listener));
            stopMultiListener.Start();
        
            Console.WriteLine($"Listening for ENDPOINT DISCOVERY messages on {listener.Client.LocalEndPoint}:{multicastPort}");
        
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        
            while (mlisten)
            {
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
                        Node peer = new Node(remoteEndPoint.Address, alias, peerPort);

                        //if node list does not have the peer then add.
                        AddUniqueNode(peer, discoveredNodes);
                        AddUniqueNode(peer, onlineNodes);
                    }
                }
            
                Console.WriteLine($"-------------\nLast 5 seconds: {onlineNodes.Count} nodes available: ");
                DisplayNodes(onlineNodes);
                Console.WriteLine("Press Enter to stop the listener...");
            }
        
            //If Active Beacon mode --> Establish p2p TCP connection. & no incoming request
            if (mode == "active" && !p2p.pendingRequest)
            {
                Node node = p2p.SelectNode(onlineNodes);
                p2p.EstablishTCP(node);
            }

            if (mode == "active" && p2p.pendingRequest)
            {
                Node node = p2p.peer;
                p2p.EstablishTCP(node);
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public void StopListener(UdpClient udpClient)
    {
        Console.WriteLine("Press Enter to stop the listener...");
        Console.ReadLine();
        mlisten = false;
        Thread.Sleep(5000);
        udpClient.Close();
    }
    
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
    
    //regex to filter ALIAS from Discovery Message
    public string GetAlias(string message)
    {
        string pattern = @"(?<=ALIAS:)[^ ]+";
        Match match = Regex.Match(message, pattern);
        if (match.Success)
        {
            return match.Value;
        }
        return null;
    }

    void DisplayNodes(List<Node> nodes)
    {
        foreach (Node node in nodes)
        {
            Console.WriteLine($"{nodes.IndexOf(node)}. {node.alias} : {node.ipaddress}:{node.port}");
        }
        Console.WriteLine("-------------");
    }

    void AddUniqueNode(Node node, List<Node> nodes)
    {
        if (!nodes.Any(n => n.ipaddress.Equals(node.ipaddress) && n.alias.Equals(node.alias))) 
        { 
            nodes.Add(node);
        }
    }
}