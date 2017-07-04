///////////////////////////////////////////////////////////////////////
// CommService.cs - Provides Communicator Service                    //
// ver 1.0                                                           //
// Language:    C#, Visual Studio 2015                               //
// Application: Remote Test Harness,                                 //
//				CSE681 - Software Modeling & Analysis                //
// Author:      Rahul Maddineni, Syracuse University                 //
// Source:      Jim Fawcett, Syracuse University                     //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defindes a Sender class and Receiver class that
 * manage all of the details to set up a WCF channel.
 * 
 * Public Functions:
 * -----------------
 * Receiver :
 * Receiver() - Initialize Receiver
 * Thread start(ThreadStart rcvThreadProc) - Start Receiver Thread
 * void Close() - Stop the service
 * void CreateRecvChannel(string address) - Create ServiceHost for Communication service
 * void PostMessage(Message msg) - Implement service method to receive messages from other Peers
 * Message GetMessage() - Implement service method to extract messages from other Peers
 * void upLoadFile(FileTransferMessage msg) - Implement uploading files to the Repository through stream
 * Stream downLoadFile(string filename) - Sends the files client requested through a stream
 * static ServiceHost CreateServiceChannel(string url) - Creates a Service channel for Host to listen for stream messages
 * 
 * Sender:
 * Sender() - Initialize Sender
 * void CreateSendChannel(string address) - Create proxy to another Peer's Communicator
 * void PostMessage(Message msg) - passes message to send thread that posts message to another Peer's queue
 * void Close() - closes the send channel
 * static ICommunicator CreateServiceChannel(string url) - Creates a service channel for client to talk to stream service
 * void uploadFile(string filename) - Uploading the file to host from client: calls upload file in the host
 * void download(string filename) - Downloading file from host to client: calls download file in the host
 * 
 * Comm:
 * Comm() - Start Comm Service
 * static string makeEndPoint(string url, int port) - Creates an endpoint
 *
 * Public Interfaces:
 * ------------------
 * - ICommunicator
 *
 * Required Files:
 * ---------------
 * CommService.cs, ICommunicator, BlockingQueue.cs, Messages.cs, Serialization.cs, HiResTimer.cs
 *   
 * Maintenance History:
 * --------------------
 * ver 1.0 : 18 Nov 2016
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using SWTools;
using System.IO;

namespace CommChannelDemo
{
  ///////////////////////////////////////////////////////////////////
  // Receiver hosts Communication service used by other Peers
  [ServiceBehavior(IncludeExceptionDetailInFaults=true)]
  public class Receiver<T> : ICommunicator
  {
    static BlockingQueue<Message> rcvBlockingQ = null;
    ServiceHost service = null;
    string filename;
    string savePath = "..\\..\\DLLStorage";
    int BlockSize = 1024;
    byte[] block;
    HRTimer.HiResTimer hrt = null;

    public string name { get; set; } 

    //----< Receiver Constructor >---------------
    public Receiver()
    {
      if (rcvBlockingQ == null)
        rcvBlockingQ = new BlockingQueue<Message>();
      block = new byte[BlockSize];
      hrt = new HRTimer.HiResTimer();
    }

    //------< Start Receiver Thread >--------------
    public Thread start(ThreadStart rcvThreadProc)
    {
      Thread rcvThread = new Thread(rcvThreadProc);
      rcvThread.Start();
      return rcvThread;
    }

    //----< Stop the Service >---------------
    public void Close()
    {
      service.Close();
    }

    //----< Create ServiceHost for Communication service >-------------
    public void CreateRecvChannel(string address)
    {
      WSHttpBinding binding = new WSHttpBinding();
      Uri baseAddress = new Uri(address);
      service = new ServiceHost(typeof(Receiver<T>), baseAddress);
      service.AddServiceEndpoint(typeof(ICommunicator), binding, baseAddress);
      service.Open();
      Console.Write("\n  Service is open listening on {0}", address);
    }

    //----< Implement service method to receive messages from other Peers >-------------
    public void PostMessage(Message msg)
    {
      //Console.Write("\n  service enQing message: \"{0}\"", msg.body);
      rcvBlockingQ.enQ(msg);
    }

    //----< Implement service method to extract messages from other Peers >----------------
    public Message GetMessage()
    {
      Message msg = rcvBlockingQ.deQ();
      //Console.Write("\n  {0} dequeuing message from {1}", name, msg.from);
      return msg;
    }
    
    //----< Implement uploading files to the Repository through stream >---------
    public void upLoadFile(FileTransferMessage msg)
    {
      int totalBytes = 0;
      hrt.Start();
      filename = msg.filename;
      string rfilename = Path.Combine(savePath, filename);
      if (!Directory.Exists(savePath))
        Directory.CreateDirectory(savePath);
      using (var outputStream = new FileStream(rfilename, FileMode.Create))
      {
        while (true)
        {
          int bytesRead = msg.transferStream.Read(block, 0, BlockSize);
          totalBytes += bytesRead;
          if (bytesRead > 0)
            outputStream.Write(block, 0, bytesRead);
          else
            break;
        }
      }
      hrt.Stop();
      Console.Write(
        "\n  Received file \"{0}\" of {1} bytes in {2} microseconds. - #Req 12",
        filename, totalBytes, hrt.ElapsedMicroseconds
      );
    }

    //----< Sends the files client requested through a stream >------------
    public Stream downLoadFile(string filename)
    {
      hrt.Start();
      string sfilename = Path.Combine(savePath, filename);
      FileStream outStream = null;
      if (File.Exists(sfilename))
      {
        outStream = new FileStream(sfilename, FileMode.Open);
      }
      else
        throw new Exception("open failed for \"" + filename + "\" - #Req 3");
      hrt.Stop();
      Console.Write("\n  Sent \"{0}\" in {1} microseconds. - #Req 12", filename, hrt.ElapsedMicroseconds);
      return outStream;
    }

    //----< Creates a Service channel for Host to listen for stream messages >--------------------
    public static ServiceHost CreateServiceChannel(string url)
    {
      BasicHttpBinding binding = new BasicHttpBinding();
      binding.TransferMode = TransferMode.Streamed;
      binding.MaxReceivedMessageSize = 50000000;
      Uri baseAddress = new Uri(url);
      Type service = typeof(CommChannelDemo.Receiver<T>);
      ServiceHost host = new ServiceHost(service, baseAddress);
      host.AddServiceEndpoint(typeof(ICommunicator), binding, baseAddress);
      return host;
    }
  }
  ///////////////////////////////////////////////////////////////////
  // Sender is client of another Peer's Communication service

  public class Sender
  {
    public string name { get; set; }

    public ICommunicator channel  { get; set; }
    string lastError = "";
    BlockingQueue<Message> sndBlockingQ = null;
    Thread sndThrd = null;
    int tryCount = 0, MaxCount = 10;
    string currEndpoint = "";
    
    public string ToSendPath { get; set; } = "..\\..\\ToSend";
    public string SavePath { get; set; } = "..\\..\\SavedFiles";
    int BlockSize = 1024;
    byte[] block;
    HRTimer.HiResTimer hrt = null;

    //----< processing for send thread >-----------------------------
    void ThreadProc()
    {
      tryCount = 0;
      while (true)
      {
        Message msg = sndBlockingQ.deQ();
        if(msg.to != currEndpoint)
        {
          currEndpoint = msg.to;
          CreateSendChannel(currEndpoint);
        }
        while (true)
        {
          try
          {
            channel.PostMessage(msg);
            Console.Write("\n  posted message from {0} to {1}", name, msg.to);
            tryCount = 0;
            break;
          }
          catch (Exception ex)
          {
            Console.WriteLine(ex);
            Console.Write("\n  connection failed");
            if (++tryCount < MaxCount)
              Thread.Sleep(100);
            else
            {
              Console.Write("\n  {0}", "can't connect\n");
              currEndpoint = "";
              tryCount = 0;
              break;
            }
          }
        }
        if (msg.body == "quit")
          break;
      }
    }

    //----< initialize Sender >--------------------------------------
    public Sender()
    {
      sndBlockingQ = new BlockingQueue<Message>();
      sndThrd = new Thread(ThreadProc);
      sndThrd.IsBackground = true;
      sndThrd.Start();
      block = new byte[BlockSize];
      hrt = new HRTimer.HiResTimer();
    }

    //----< Create proxy to another Peer's Communicator >------------
    public void CreateSendChannel(string address)
    {
      EndpointAddress baseAddress = new EndpointAddress(address);
      WSHttpBinding binding = new WSHttpBinding();
      ChannelFactory<ICommunicator> factory
        = new ChannelFactory<ICommunicator>(binding, address);
      channel = factory.CreateChannel();
      Console.Write("\n  service proxy created for {0}", address);
    }

    //----< passes message to send thread that posts message to another Peer's queue >------------------
    public void PostMessage(Message msg)
    {
      sndBlockingQ.enQ(msg);
    }

    public string GetLastError()
    {
      string temp = lastError;
      lastError = "";
      return temp;
    }

    //----< closes the send channel >--------------------------------
    public void Close()
    {
      ChannelFactory<ICommunicator> temp = (ChannelFactory<ICommunicator>)channel;
      temp.Close();
    }

    //----< Creates a service channel for client to talk to stream service >-----------------
    public static ICommunicator CreateServiceChannel(string url)
    {
      BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
      BasicHttpBinding binding = new BasicHttpBinding(securityMode);
      binding.TransferMode = TransferMode.Streamed;
      binding.MaxReceivedMessageSize = 500000000;
      EndpointAddress address = new EndpointAddress(url);
      ChannelFactory<ICommunicator> factory = new ChannelFactory<ICommunicator>(binding, address);
      return factory.CreateChannel();
    }

    //----< Uploading the file to host from client: calls upload file in the host >------------
    public void uploadFile(string filename)
    {
      hrt.Start();
      string fqname = Path.Combine(ToSendPath, filename);
      using (var inputStream = new FileStream(fqname, FileMode.Open))
      {
        FileTransferMessage msg = new FileTransferMessage();
        msg.filename = filename;
        msg.transferStream = inputStream;
        channel.upLoadFile(msg);
      }
      hrt.Stop();
      Console.Write("\n  Uploaded file \"{0}\" in {1} microseconds. - #Req 12", filename, hrt.ElapsedMicroseconds);
    }

    //----< Downloading file from host to client: calls download file in the host >------------ 
    public void download(string filename)
    {
      int totalBytes = 0;
      hrt.Start();
      try
      {
        Stream strm = channel.downLoadFile(filename);
        string rfilename = Path.Combine(SavePath, filename);
        if (!Directory.Exists(SavePath))
          Directory.CreateDirectory(SavePath);
        using (var outputStream = new FileStream(rfilename, FileMode.Create))
        {
          while (true)
          {
            int bytesRead = strm.Read(block, 0, BlockSize);
            totalBytes += bytesRead;
            if (bytesRead > 0)
              outputStream.Write(block, 0, bytesRead);
            else
              break;
          }
        }
        hrt.Stop();
        Console.Write("\n  Received file \"{0}\" of {1} bytes in {2} microseconds. - #Req 12", filename, totalBytes, hrt.ElapsedMicroseconds);
      }
      catch (Exception ex)
      {
        Console.Write("\n  {0}  - #Req 3\n", ex.Message);
      }
    }
  }
  ///////////////////////////////////////////////////////////////////
  // Comm class simply aggregates a Sender and a Receiver
  //
  public class Comm<T>
  {
    public string name { get; set; } = typeof(T).Name;

    public Receiver<T> rcvr { get; set; } = new Receiver<T>();

    public Sender sndr { get; set; } = new Sender();

    //----< Start Comm Service >---------------------
    public Comm()
    {
      rcvr.name = name;
      sndr.name = name;
    }
    
    //----< Creates an endpoint >--------------
    public static string makeEndPoint(string url, int port)
    {
      string endPoint = url + ":" + port.ToString() + "/ICommunicator";
      return endPoint;
    }

    //----< this thrdProc() used only for testing, below >-----------
    public void thrdProc()
    {
      while (true)
      {
        Message msg = rcvr.GetMessage();
        msg.showMsg();
        if (msg.body == "quit")
        {
          break;
        }
      }
    }
  }
#if(TEST_COMMSERVICE)

  class Cat { }
  class TestComm
  {
    [STAThread]
    static void Main(string[] args)
    {
      Comm<Cat> comm = new Comm<Cat>();
      string endPoint = Comm<Cat>.makeEndPoint("http://localhost", 8080);
      comm.rcvr.CreateRecvChannel(endPoint);
      comm.rcvr.start(comm.thrdProc);
      comm.sndr = new Sender();
      comm.sndr.CreateSendChannel(endPoint);
      Message msg1 = new Message();
      msg1.body = "Message #1";
      comm.sndr.PostMessage(msg1);
      Message msg2 = new Message();
      msg2.body = "quit";
      comm.sndr.PostMessage(msg2);
      Console.Write("\n  Comm Service Running:");
      Console.Write("\n  Press key to quit");
      Console.ReadKey();
      Console.Write("\n\n");
    }
#endif
  }
}
