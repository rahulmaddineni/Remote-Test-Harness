///////////////////////////////////////////////////////////////////////
// TestHarness.cs - Demonstrate application use of channel           //
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
 * The Server package defines one class, Server, that uses the Comm<TestHarness>
 * class to receive messages from a remote endpoint.
 * 
 * Public Functions:
 * -----------------
 * TestHarness() - Initialize Receiver
 * void wait() - Stop Recceiver
 * Message makeMessage(string author, string fromEndPoint, string toEndPoint) - Create Basic Message
 * void testWait() - stop all child threads 
 * void processMessages() - main activity of TestHarness
 * AppDomain createChildAppDomain() - create child AppDomain
 *
 * Public Interfaces:
 * ------------------
 * - ITestInfo
 * - IRequestInfo
 * - ITestResults
 * - ILoadAndTest
 * - ITestResult
 * 
 * Required Files:
 * ---------------
 * - TestHarness.cs
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
using System.IO;
using System.ServiceModel;
using System.Xml.Linq;
using System.Runtime.Remoting;
using System.Security.Policy;
using SWTools;

namespace CommChannelDemo
{
  ///////////////////////////////////////////////////////////////////
  // Test and RequestInfo are used to pass test request information
  // to child AppDomain
  //
  [Serializable]
  class Test : ITestInfo
  {
    public string testName { get; set; }
    public List<string> files { get; set; } = new List<string>();
  }
  [Serializable]
  class RequestInfo : IRequestInfo
  {
    public string tempDirName { get; set; }
    public List<ITestInfo> requestInfo { get; set; } = new List<ITestInfo>();
  }

  [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
  public class TestHarness
  {
    public Comm<TestHarness> comm { get; set; } = new Comm<TestHarness>();

    public string endPoint { get; } = Comm<TestHarness>.makeEndPoint("http://localhost", 8080);

    private Thread rcvThread = null;
        
    private BlockingQueue<Message> inQ_ = null;

    private string filePath_;

    object sync_ = new object();

    List<Thread> threads_ = new List<Thread>();
    
    Dictionary<int, string> TLS = new Dictionary<int, string>();

    //---< Initialize Receiver >-------------------
    public TestHarness()
    {
      comm.rcvr.CreateRecvChannel(endPoint);
      rcvThread = comm.rcvr.start(rcvThreadProc);
      if (inQ_ == null)
        inQ_ = new BlockingQueue<Message>();
    }

    //----< Stop Recceiver >------------------------
    public void wait()
    {
      rcvThread.Join();
    }

    //----< Create Basic Message >------------------
    public Message makeMessage(string author, string fromEndPoint, string toEndPoint)
    {
      Message msg = new Message();
      msg.author = author;
      msg.from = fromEndPoint;
      msg.to = toEndPoint;
      return msg;
    }

    void rcvThreadProc()
    {
      while (true)
      {
        Message msg = comm.rcvr.GetMessage();
        msg.time = DateTime.Now;
        Console.Write("\n\n  {0} received message: - #Req 10", comm.name);
        msg.showMsg();
        if (msg.type == "TestRequest"){
            Message testRequest = msg.copy();
            inQ_.enQ(testRequest);                              //Enqueuing Test Requests in a Blocking Queue, so processMessages() will dequeue it
        }
        if (msg.body == "quit")
          break;
      }
    }

    //----< make path name from author and time >--------------------

    string makeKey(string author)
    {
      DateTime now = DateTime.Now;
      string nowDateStr = now.Date.ToString("d");
      string[] dateParts = nowDateStr.Split('/');
      string key = "";
      foreach (string part in dateParts)
        key += part.Trim() + '_';
      string nowTimeStr = now.TimeOfDay.ToString();
      string[] timeParts = nowTimeStr.Split(':');
      for(int i = 0; i< timeParts.Count() - 1; ++i)
        key += timeParts[i].Trim() + '_';
      key += timeParts[timeParts.Count() - 1];
      key = author + "_" + key + "_" + "ThreadID" + Thread.CurrentThread.ManagedThreadId;
      return key;
    }
    //----< retrieve test information from testRequest >-------------

    List<ITestInfo> extractTests(Message testRequest)
    {
      Console.Write("\n  Parsing test request:");
      Console.WriteLine("\n  --------------------");
      List<ITestInfo> tests = new List<ITestInfo>();
      XDocument doc = XDocument.Parse(testRequest.body);
      foreach (XElement testElem in doc.Descendants("TestElement"))
      {
        Test test = new Test();
        string testDriverName = testElem.Element("testDriver").Value;
        test.testName = testElem.Element("testName").Value;
        test.files.Add(testDriverName);
        foreach (XElement lib in testElem.Elements("testCodes").Elements("string"))
        {
          test.files.Add(lib.Value);
        }
        tests.Add(test);
      }
      return tests;
    }

    //----< retrieve test code from testRequest >--------------------

    List<string> extractCode(List<ITestInfo> testInfos)
    {
      Console.Write("\n  Retrieving code files from testInfo data structure");
      List<string> codes = new List<string>();
      foreach (ITestInfo testInfo in testInfos)
        codes.AddRange(testInfo.files);
      return codes;
    }

    //----< create local directory and load from Repository >--------

    RequestInfo processRequestAndLoadFiles(Message testRequest)
    {
      string localDir_ = "";
      RequestInfo rqi = new RequestInfo();
      rqi.requestInfo = extractTests(testRequest);
      List<string> files = extractCode(rqi.requestInfo);

      localDir_ = makeKey(testRequest.author);
      rqi.tempDirName = localDir_;
      lock (sync_)
      {
        filePath_ = System.IO.Path.GetFullPath(localDir_);  // LoadAndTest will use this path
        TLS[Thread.CurrentThread.ManagedThreadId] = filePath_;
      }
      Console.Write("\n\n  Creating local test directory \"" + localDir_ + "\"");
      System.IO.Directory.CreateDirectory(localDir_);

      Console.Write("\n  Loading code from Repository - #Req 6\n");
      foreach (string file in files)
      {
        string name = System.IO.Path.GetFileName(file);
        comm.sndr.channel = Sender.CreateServiceChannel("http://localhost:8082/StreamService");
        comm.sndr.SavePath = localDir_;
        Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": retrieving file \"" + name + "\"");
        comm.sndr.download(file);
      }
      Console.WriteLine(" ");
      return rqi;
    }
    
    //----< run tests >----------------------------------------------
    ITestResults runTests(Message testRequest)
    {
      AppDomain ad = null;
      ILoadAndTest ldandtst = null;
      RequestInfo rqi = null;
      ITestResults tr = null;
      
      try
      {
        //lock (sync_)
        //{
          rqi = processRequestAndLoadFiles(testRequest);
          ad = createChildAppDomain();
          ldandtst = installLoader(ad);
        //}
        if (ldandtst != null)
        {
          tr = ldandtst.test(rqi);
        }
        // unloading ChildDomain, and so unloading the library
        saveResultsAndLogs(tr, testRequest);

        lock (sync_)
        {
          Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": unloading: \"" + ad.FriendlyName + "\"\n");
          AppDomain.Unload(ad);
          try
          {
            System.IO.Directory.Delete(rqi.tempDirName, true);
            Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": removed directory " + rqi.tempDirName);
          }
          catch (Exception ex)
          {
            Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": could not remove directory " + rqi.tempDirName);
            Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": " + ex.Message);
          }
        }
        return tr;
      }
      catch(Exception ex)
      {
        Console.Write("\n\n---- {0}\n\n", ex.Message);
        return tr;
      }
    }
    
    //----< save results and logs in Repository >--------------------
     bool saveResultsAndLogs(ITestResults testResults, Message testReq)
    {
      Message resultsMsg = new Message();
      string toRepo = "http://localhost:8082/ICommunicator";
      resultsMsg = makeMessage(testReq.author, testReq.to, toRepo);
      resultsMsg.type = "StoreResult";
      string logName = testResults.testKey + ".txt";
      Console.WriteLine("\n\n  Created key {0} - #Req 8",logName);
      StringBuilder sr = new StringBuilder();
      try
      {
        sr.Append(testResults.testKey);
        sr.Append(",");
        foreach (ITestResult test in testResults.testResults)
        {
          sr.Append("-----------------------------");
          sr.Append(",");
          sr.Append(test.testName);
          sr.Append(",");
          sr.Append(test.testResult);
          sr.Append(",");
          sr.Append(test.testLog);
          sr.Append(",");
        }
        sr.Append("-----------------------------");
        sr.Append(",");

        resultsMsg.body = sr.ToString();
        comm.sndr.PostMessage(resultsMsg);
        Console.WriteLine("\n  Saving Logs: Logs sent to {0} - #Req 7,8", toRepo);
      }
      catch
      {
        return false;
      }
      return true;
    }

    //----< make TestResults Message >-------------------------------
    Message makeTestResultsMessage(ITestResults tr)
    {
      Message trMsg = new Message();
      trMsg.author = "TestHarness";
      trMsg.to = "http://localhost:8081/ICommunicator";
      trMsg.from = "http://localhost:8080";
      trMsg.type = "TestResult";
      XDocument doc = new XDocument();
      XElement root = new XElement("testResultsMsg");
      doc.Add(root);
      XElement testKey = new XElement("testKey");
      testKey.Value = tr.testKey;
      root.Add(testKey);
      XElement timeStamp = new XElement("timeStamp");
      timeStamp.Value = tr.dateTime.ToString();
      root.Add(timeStamp);
      XElement testResults = new XElement("testResults");
      root.Add(testResults);
      foreach(ITestResult test in tr.testResults)
      {
        XElement testResult = new XElement("testResult");
        testResults.Add(testResult);
        XElement testName = new XElement("testName");
        testName.Value = test.testName;
        testResult.Add(testName);
        XElement result = new XElement("result");
        result.Value = test.testResult;
        testResult.Add(result);
        XElement log = new XElement("log");
        log.Value = test.testLog;
        testResult.Add(log);
      }
      trMsg.body = doc.ToString();
      return trMsg;
    }
    
    //----< stop all child threads >-------------------------
    public void testWait()
    {
      foreach (Thread t in threads_)
        t.Join();
    }

    //----< main activity of TestHarness >---------------------------
    public void processMessages()
    {
      AppDomain main = AppDomain.CurrentDomain;
      Console.Write("\n  Starting in AppDomain " + main.FriendlyName + "\n");
            
        ThreadStart doTests = () => {
       // while (true)
        //{
            HRTimer.HiResTimer hr = new HRTimer.HiResTimer();
            hr.Start();
            Message testRequest = inQ_.deQ();
            if (testRequest.body == "quit")
            {
                inQ_.enQ(testRequest);
                return;
            }
            ITestResults testResults = runTests(testRequest);
            lock (sync_)
            {
                //client_.sendResults(makeTestResultsMessage(testResults));
                Message testResultsMsg = makeTestResultsMessage(testResults);
                testResultsMsg.to = testRequest.from;
                comm.sndr.PostMessage(testResultsMsg);
            }
            hr.Stop();
            Console.WriteLine("\n\n  Performance:  Test Request from " + testRequest.author + " is performed in " + hr.ElapsedMicroseconds + " microseconds - #Req 12");
        //}
      }; 

      int numThreads = 6;

      for(int i = 0; i < numThreads; ++i)
      {
        Console.Write("\n  creating AppDomain thread");
        Thread t = new Thread(doTests);
        threads_.Add(t);
        t.Start();
      }
    }
    
    //----< create child AppDomain >---------------------------------
    public AppDomain createChildAppDomain()
    {
      try
      {
        Console.Write("\n  Creating child AppDomain - #Req 4");
        Console.WriteLine("\n  -----------------------");

        AppDomainSetup domaininfo = new AppDomainSetup();
        domaininfo.ApplicationBase
          = "file:///" + System.Environment.CurrentDirectory;  // defines search path for assemblies
        //domaininfo.ApplicationBase
        //  = tempPath_;  // defines search path for assemblies
        //domaininfo.PrivateBinPath
        //  = "file:///" + System.Environment.CurrentDirectory;  // defines search path for assemblies

        //Create evidence for the new AppDomain from evidence of current

        Evidence adevidence = AppDomain.CurrentDomain.Evidence;

        // Create Child AppDomain

        AppDomain ad
          = AppDomain.CreateDomain("ChildDomain", adevidence, domaininfo);

        Console.Write("\n  Created AppDomain \"" + ad.FriendlyName + "\"");
        return ad;
      }
      catch (Exception except)
      {
        Console.Write("\n  " + except.Message + "\n\n");
      }
      return null;
    }

    //----< Load and Test is responsible for testing >---------------
    ILoadAndTest installLoader(AppDomain ad)
    {
      ad.Load("LoadAndTest");
      //showAssemblies(ad);
      //Console.WriteLine();

      // create proxy for LoadAndTest object in child AppDomain

      ObjectHandle oh
        = ad.CreateInstance("LoadAndTest", "CommChannelDemo.LoadAndTest");
      object ob = oh.Unwrap();    // unwrap creates proxy to ChildDomain
                                  // Console.Write("\n  {0}", ob);

      // set reference to LoadAndTest object in child

      ILoadAndTest landt = (ILoadAndTest)ob;

      // create Callback object in parent domain and pass reference
      // to LoadAndTest object in child

      //landt.setCallback(cb_);
      lock (sync_)
      {
        filePath_ = TLS[Thread.CurrentThread.ManagedThreadId];
        landt.loadPath(filePath_);  // send file path to LoadAndTest
      }
      return landt;
    }
    
    static void Main(string[] args)
    {
      Console.Title = "Test Harness";
      Console.Write("\n  Testing Test Harness Demo (Console) - # Req 11");
      Console.Write("\n ==================================\n");
      Console.WriteLine("\n  Implemented in C# using .NET Framework. - # Req 1");
      Console.WriteLine("\n  Demontrating automatically - # Req 13");

      TestHarness Server = new TestHarness();
      Server.processMessages();

      Message msg = Server.makeMessage("Rahul", Server.endPoint, Server.endPoint);

      Console.Write("\n  press key to exit: ");
      Console.ReadKey();
      msg.to = Server.endPoint;
      msg.body = "quit";
      Server.comm.sndr.PostMessage(msg);

      Server.wait();
      Server.testWait();      
      Console.Write("\n\n");
   
     }
  }
}
