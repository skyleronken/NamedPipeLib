using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace InterProcessCommunications
{
    public class NamedPipesIPC
    {

        private string name;
        private string host;
        private NamedPipeServerStream server;
        private NamedPipeClientStream client;
        private Task np_task;
        private StreamReader sr;
        private StreamWriter sw;
        private bool keepalive;
        private Func<string, string> handler;
        private Semaphore _pipelock;

        public NamedPipesIPC(string pipename, Func<string, string> msghandler, string servername = ".")
        {
            name = pipename;                    // Name of Named Pipe
            host = servername;                  // Remote or local server name. Default is localhost.
            keepalive = true;                   // Keep async loop for full duplex send/receive via a single pipe. 
            handler = msghandler;               // function to call when message is received. 
            _pipelock = new Semaphore(0, 1);    // To prevent accessing of StreamWriter before it is instantiated. 
        }

        public void Connect()
        {
            /*
             *  Method to call when NamedPipesIPC is to be used as a client.
             */
            try
            {
                Console.WriteLine("Starting Client");

                client = new NamedPipeClientStream(host, name, PipeDirection.InOut, PipeOptions.Asynchronous);

                np_task = StartClientAsync(handler);
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public void Listen()
        {
            /*
             *  Method to call when NamedPipesIPC is to be used as a server.
             */
            try
            {
                Console.WriteLine("Starting Server");

                server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

                np_task = StartServerAsync(handler);
            }
            catch (Exception e)
            {
                throw e;
            }

        }
        
        private async Task StartServerAsync(Func<string, string> messageHandler)
        {
            // Handle async connection events.
            await Task.Factory.FromAsync(
                (cb, state) => server.BeginWaitForConnection(cb, state),
                ar => server.EndWaitForConnection(ar),
                null);

            sw = new StreamWriter(server);
            sr = new StreamReader(server);
            sw.AutoFlush = true;
            _pipelock.Release(1); // Release the semaphore now that the streams are created. Can now call Send().


            string message;

            while (keepalive)
            {
                message = await sr.ReadLineAsync();
                messageHandler(message);
            }

            server.Disconnect();

        }

        private async Task StartClientAsync(Func<string, string> messageHandler)
        {
            client.Connect();

            sw = new StreamWriter(client);
            sr = new StreamReader(client);
            sw.AutoFlush = true;
            _pipelock.Release(1);

            string message;

            while(keepalive)
            {
                message = await sr.ReadLineAsync();
                messageHandler(message);
            }

            client.WaitForPipeDrain();
            client.Close();
        }

        public void Kill()
        {
            keepalive = false;
        }

        public void Send(string message)
        {

            try
            {
                _pipelock.WaitOne(); // Gain a lock on the namedpipe
                sw.WriteLine(message); 
                _pipelock.Release(); // Release the lock on the namedpipe.
            }
            catch (Exception e)
            {
                throw e;
            }
        }

    }

}
