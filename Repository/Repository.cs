///////////////////////////////////////////////////////////////////////
// Repository.cs - Contains Test Drivers, Test Codes, Logs           //
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
 * - Provides the Stream Service 
 * - Accepts files from Clients and stores them.
 * - Sends files to Test Harness
 * - Stores Logs from the Test Harness
 * - Provides Query Mechanism for Clients to Query the Logs
 * 
 * Public Functions:
 * -----------------
 * Repository() - Initialize Receiver
 * void wait() - Stop Receiver
 * Message makeMessage(string author, string fromEndPoint, string toEndPoint) - Create Basic Message
 * void storeLogs(string result) - Store the logs sent by Test Harness
 * List<string> queryLogs(string queryText) - Search for query in Logs
 * void getFileNames(string to) - Get File names in Repository
 * void sendLog(List<string> Log, string to) - Send Queries to corresponding client
 *
 * Public Interfaces:
 * ------------------
 * - IRepository
 * 
 * Required Files:
 * - Communication.cs, ITest.cs, Logger.cs, Messages.cs, Serialization.cs, Repository.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 18 Nov 2016
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ServiceModel;
using System.Threading;

namespace CommChannelDemo
{ 
  [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
  public class Repository : IRepository
  {
    string logStoragePath = "..\\..\\LogStorage";
    string dllRepopath = "..//..//DLLStorage";
   
    public Comm<Repository> comm { get; set; } = new Comm<Repository>();

    public string endPoint { get; } = Comm<Repository>.makeEndPoint("http://localhost", 8082);

    private Thread rcvThread = null;

    private ServiceHost sh;

    //----< Initialize Receiver >--------------
    public Repository()
    {
      DLog.write("\n  Creating instance of Repository");
      comm.rcvr.CreateRecvChannel(endPoint);
      rcvThread = comm.rcvr.start(rcvThreadProc);
      sh = Receiver<Repository>.CreateServiceChannel("http://localhost:8082/StreamService");
    }
    
    //----< Stop Receiver >------------------
    public void wait()
    {
      rcvThread.Join();
    }

    //----< Create Basic Message >------------
    public Message makeMessage(string author, string fromEndPoint, string toEndPoint)
    {
      Message msg = new Message();
      msg.author = author;
      msg.from = fromEndPoint;
      msg.to = toEndPoint;
      return msg;
    }

    //----< Thread Proc Handling for Receiver >-------------
    void rcvThreadProc()
    {
      while (true)
      {
        Message msg = comm.rcvr.GetMessage();
        msg.time = DateTime.Now;
        Console.Write("\n\n  {0} received message: - #Req 10", comm.name);
        Console.WriteLine("\n  ---------------------------");
        Console.WriteLine("\n  From: {0}", msg.from);
        Console.WriteLine("  At: {0}", msg.time);

        //msg.showMsg();
        if (msg.type == "StoreResult"){
          if(msg.body != null)
            {
                Console.WriteLine("\n  Logs Received from: " + msg.from);
                storeLogs(msg.body);
            }
        }
        if (msg.type == "LogQuery")
        {
            Console.WriteLine("\n  Received Query: " + msg.body + "  - #Req 7");
            if (msg.body != null)
            {
                List<string> query = queryLogs(msg.body);
                sendLog(query, msg.from);
            }            
        }
        if (msg.type == "RepoFileQuery")
        {
            getFileNames(msg.from);
        }
        if (msg.body == "quit")
        {
            Console.WriteLine("\n  Repository Shutting Down");
            break;
        }
        
      }
    }

    //----< Stores the logs sent by the Test Harness >------------
    void storeLogs(string result)
    {
      string[] res = result.Split(',');
      string logName = res[0] + ".txt";
      System.IO.StreamWriter sr = null;
      try
        {    
            sr = new System.IO.StreamWriter(System.IO.Path.Combine(logStoragePath, logName));
            foreach (string re in res)
            {
                sr.WriteLine(re);
            }
            Console.WriteLine("\n  Logs Stored to: " + logName  + " - #Req 8");
        }
        catch
        {
        sr.Close();
        }
        sr.Close();
    }

    //----< search for text in log files >---------------------------
    public List<string> queryLogs(string queryText)
    {
      List<string> queryResults = new List<string>();
      string path = System.IO.Path.GetFullPath(logStoragePath);
      string[] files = System.IO.Directory.GetFiles(path, "*.txt");
      foreach(string file in files)
      {
        string contents = File.ReadAllText(file);
        if (contents.Contains(queryText))
        {
          string name = System.IO.Path.GetFileName(file);
          queryResults.Add(name);
        }
      }
      return queryResults;
    }
    
    //----< Get file names in the repository >-----------------
    public void getFileNames(string to)
    {
        Message fileNamesMsg = new Message();
        fileNamesMsg = makeMessage("Repository","http://localhost:8082/ICommunicator",to);
        fileNamesMsg.type = "RepoFileReply";
        StringBuilder fn = new StringBuilder();
        string path = System.IO.Path.GetFullPath(dllRepopath);
        string[] files = System.IO.Directory.GetFiles(path, "*.dll");
        foreach(string file in files)
        {
            string name = System.IO.Path.GetFileName(file);
            fn.Append(name);
            fn.Append(",");
        }
        fileNamesMsg.body = fn.ToString();
        comm.sndr.PostMessage(fileNamesMsg);
    }
    
    //----< Send Queried Logs to the Requesting Client >--------------
    public void sendLog(List<string> Log, string to)
    {
        Message logsMsg = new Message();
        logsMsg = makeMessage("Repository","http://localhost:8082/ICommunicator",to);
        logsMsg.type = "LogReply";
        StringBuilder sr = new StringBuilder();
        foreach(string log in Log)
        {
            sr.Append(log);
            sr.Append(",");
        }
        logsMsg.body = sr.ToString();
        comm.sndr.PostMessage(logsMsg);
        Console.WriteLine("\n  Query Logs Sent to: {0}", to);
    }

    static void Main(string[] args)
    {
      Console.Title = "Repository";
      Console.Write("\n  Testing Repository Demo (Console) - # Req 11");
      Console.Write("\n  ================================\n");
      Console.WriteLine("\n  Implemented in C# using .NET Framework. - # Req 1");
      Console.WriteLine("\n  Demontrating automatically - # Req 13");

      Repository Server = new Repository();

      Message msg = Server.makeMessage("Fawcett", Server.endPoint, Server.endPoint);

      ServiceHost host = Server.sh;

      host.Open();

      Console.Write("\n  Stream Service is open listening on http://localhost:8082/StreamService");

      Console.Write("\n  press key to exit: ");
      Console.ReadKey();

      msg.to = Server.endPoint;
      msg.body = "quit";
      Server.comm.sndr.PostMessage(msg);
      Server.wait();
      Console.Write("\n\n");
   
      host.Close();
    }

  }
}
