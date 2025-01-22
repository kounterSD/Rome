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
            Console.WriteLine("\n1. Initialize Passive Listener - See Available Hosts on the Network\n2. Active Beacon - Advertise your presence and establish connection");
            int option = int.Parse(Console.ReadLine());
        
            switch (option)
            {
                case 1:
                
                    multicast.StartMulticastListener("passive");
                    break;
                case 2:
                    Task.Run(() => p2p.TCPListener());
                    Task.Run(() => multicast.StartMulticastBroadcaster(myAlias));
                    multicast.StartMulticastListener("active");
                    break;
                    
            }
        }
    }

}