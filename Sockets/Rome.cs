namespace Sockets;

class Rome
{
    static void Main(string[] args)
    {
        Console.WriteLine("Welcome to Rome!");
        Menu();
    }

    static void Menu()
    {
        Multicast multicast = new Multicast();
        P2P p2p = new P2P();
        
        Console.WriteLine("Enter your Alias: ");
        string myAlias = Console.ReadLine();
        
        bool exit = false;
        while (!exit)
        {
            Console.WriteLine("\n1. Initialize Passive Listener - See Available Nodes on the Network\n2. Active Beacon - Advertise your presence and establish connection");
            int option = int.Parse(Console.ReadLine());
        
            switch (option)
            {
                case 1:
                    multicast.StartMulticastListener("passive");
                    break;
                case 2:
                    Thread p2pListener = new Thread(() => p2p.TCPListener());
                    p2pListener.Start();

                    Thread multiListener = new Thread(() => multicast.StartMulticastListener("active"));
                    multiListener.Start();
                    
                    multicast.StartMulticastBroadcaster(myAlias);
                    break;
                    
            }
        }
    }
    
    

}