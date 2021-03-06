﻿///////////////////////////////////////////////////////////////////////
// Client.cs - Sends Test Requests to Test Harness                   //
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
 * The Client package defines one class, Client, that uses the Comm<Client>
 * class to pass messages to a remote endpoint.
 * 
 * Public Functions:
 * -----------------
 * Client()    - Constructor that initializes Receiver
 * void wait() - join receive thread   
 * public Message makeMessage(string author, string fromEndPoint, string toEndPoint) - Construct a basic message
 * public Message makeMessage() - Basic Message with no arguments
 *
 * Public Interfaces:
 * ------------------
 * - ICommunicator
 *
 * Required Files:
 * ---------------
 * - Client.cs
 * - ICommunicator.cs, CommServices.cs
 * - Messages.cs, MessageTest, Serialization
 *
 * Maintenance History:
 * --------------------
 * Ver 1.0 : 18 Nov 2016
 * - first release 
 *  
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Utilities;
using System.ServiceModel;
using System.IO;

namespace CommChannelDemo
{
  ///////////////////////////////////////////////////////////////////
  // Client class demonstrates how an application uses Comm
  //
  public class Client
  {
    public Comm<Client> comm { get; set; } = new Comm<Client>();

    public string endPoint { get; } = Comm<Client>.makeEndPoint("http://localhost", 4150);

    private Thread rcvThread = null;
    
    //----< initialize receiver >------------------------------------

    public Client()
    {
      comm.rcvr.CreateRecvChannel(endPoint);
      rcvThread = comm.rcvr.start(rcvThreadProc);
    }
    //----< join receive thread >------------------------------------

    public void wait()
    {
      rcvThread.Join();
    }
    //----< construct a basic message >------------------------------

    public Message makeMessage(string author, string fromEndPoint, string toEndPoint)
    {
      Message msg = new Message();
      msg.author = author;
      msg.from = fromEndPoint;
      msg.to = toEndPoint;
      return msg;
    }
    //----< use private service method to receive a message >--------
     
    void rcvThreadProc()
    {
      while (true)
      {
        Message msg = comm.rcvr.GetMessage();
        msg.time = DateTime.Now;
        Console.Write("\n\n  Client 1 received message: - #Req 10");
        //msg.showMsg();
        if(msg.type == "TestResult")
        {
            if(msg.body != null){
                Console.WriteLine("\n\n  Received Test Results from Test Harness: - #Req 7");
                Console.WriteLine("  ----------------------------------------");
                Console.WriteLine(msg.body);
            }
            Console.Write("\n\n  Sending Query to Repository");
            Console.Write("\n ============================\n");
            string remoteEndPoint1 = Comm<Client>.makeEndPoint("http://localhost", 8082);
            Message msg1 = makeMessage("Rahul", endPoint, remoteEndPoint1);
            msg1.type = "LogQuery";
            msg1.body = "pass";
            Console.WriteLine("\n  Query: " + msg1.body);
            comm.sndr.PostMessage(msg1);
        }
        if(msg.type == "LogReply")
        {
            string[] res = msg.body.Split(',');
            Console.WriteLine("\n\n  Query Results: - #Req 9");
            Console.WriteLine("  --------------");
            foreach (string re in res)
               {
                    Console.WriteLine(re);
               }
        }
        if(msg.type == "RepoFileReply")
        {
            // Handled in WPF
        }
        if (msg.body == "quit")
        {
            Console.WriteLine("\n\n  Client 1 Shutting Down.");
            Console.WriteLine("\n  Quit");
            break;
        }
          
      }
    }

    //----< run client demo >----------------------------------------

    static void Main(string[] args)
    {
      Console.Title = "Client1";
      Console.Write("\n  Testing Client 1 Demo (Console) - # Req 11");
      Console.Write("\n ===============================\n");
      Console.WriteLine("\n  Demontrating automatically - # Req 13");

      Client client = new Client();

      Console.Write("\n\n  Uploading files to the Repository - #Req 2,6");
      Console.Write("\n ==================================\n");
      
      client.comm.sndr.channel = Sender.CreateServiceChannel("http://localhost:8082/StreamService");        // To Repo
      client.comm.sndr.ToSendPath = "..\\..\\DLL";
      
      client.comm.sndr.uploadFile("TestDriver.dll");
      client.comm.sndr.uploadFile("TestedCode.dll");

      // Sending Test Request to Test Harness
      Console.Write("\n\n  Making Test Request and sending it to Test Harness - #Req2");
      Console.Write("\n ===================================================\n");
      string remoteEndPoint = Comm<Client>.makeEndPoint("http://localhost", 8080);
      Message msg = client.makeMessage("Rahul", client.endPoint, remoteEndPoint);
      msg.type = "TestRequest";
      msg.body = MessageTest.makeTestRequest("TestDriver.dll","TestedCode.dll");
      Console.WriteLine(msg.body);
      client.comm.sndr.PostMessage(msg);

      Console.Write("\n  press key to exit: ");
      Console.ReadKey();
      msg.to = client.endPoint;
      msg.type = "QUIT";
      msg.body = "quit";
      client.comm.sndr.PostMessage(msg);
      client.wait();
      Console.Write("\n\n");
    }
  }
}


