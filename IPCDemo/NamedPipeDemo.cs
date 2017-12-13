using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using InterProcessCommunications;

namespace NamedPipeDemo
{
    public class NamedPipeDemo
    {

        static string pipename = "pipe_demo";
        static NamedPipesIPC np_client;
        static NamedPipesIPC np_server;

        static void Main(string[] args)
        {
            Console.WriteLine("Started Named Pipe Demo");
            np_client = new NamedPipesIPC(pipename, cliHandler);
            np_server = new NamedPipesIPC(pipename, srvHandler);

            np_server.Listen();
            np_client.Connect();

            np_client.Send("Client Send");

            Console.WriteLine("Wait for async completion. Then press enter to continue.");
            Console.ReadLine();

            np_client.Kill();
            np_server.Kill();
        }

        public static string srvHandler(string msg)
        {
            Console.WriteLine(msg);
            switch (msg)
            {
                case "Client Send":
                    np_server.Send("Server Respond");
                    np_server.Send("Server Send");
                    break;
                case "Client Respond":
                    break;
            }
            return "done";
        }

        public static string cliHandler(string msg)
        {
            Console.WriteLine(msg);
            switch (msg)
            {
                case "Server Send":
                    np_client.Send("Client Respond");
                    break;
                case "Server Respond":
                    break;
            }
            return "done";
        }

    }
}