/////////////////////////////////////////////////////////////////////// 
// ClientGUI2 - Demonstrates GUI for Client                          //
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
 * The ClientGUI2 package defines one class, Client2, that uses the Comm<Client>
 * class to pass messages to a remote endpoint.
 * Provides WPF for Client to access Test Harness and Repository
 * 
 * Public Functions:
 * -----------------
 * Client2()    - Constructor that initializes Receiver
 *
 * Public Interfaces:
 * ------------------
 * - ICommunicator
 *
 * Required Files:
 * ---------------
 * - MainWindow.xaml.cs
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Automation.Provider;

namespace CommChannelDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Client2 : Window
    {
        //CommChannelDemo.Client client;
        delegate void NewTestResultsMessage(string msg); // Test Results Delegate
        event NewTestResultsMessage OnNewTestResultsMessage;

        delegate void NewLogsMessage(string msg); // Query Logs Delegate
        event NewLogsMessage OnNewLogsMessage;

        delegate void NewFileResultsMessage(string msg); // Test Results Delegate
        event NewFileResultsMessage OnNewFileResultsMessage;

        public Comm<Client> comm { get; set; } = new Comm<Client>();

        public string endPoint { get; } = Comm<Client>.makeEndPoint("http://localhost", 5050);

        private Thread rcvThread = null;

        //----< Constructor >-----------------------
        public Client2()
        {
            InitializeComponent();

            OnNewTestResultsMessage += new NewTestResultsMessage(OnNewTestResultsMessageHandler);
            OnNewLogsMessage += new NewLogsMessage(OnNewLogsMessageHandler);
            OnNewFileResultsMessage += new NewFileResultsMessage(OnNewFileResultsMessageHandler);

            ResultsTabItem.IsEnabled = false;
            TestTabItem.IsEnabled = false;

            UploadDLLButton.IsEnabled = false;
            BrowseDLLButton.IsEnabled = false;

            Console.Title = "Client2";
            Console.Write("\n  Testing Client 2 Demo (WPF) - # Req 11");
            Console.Write("\n ===========================\n");
            Console.WriteLine("\n  Demontrating automatically - # Req 13");

            comm.rcvr.CreateRecvChannel(endPoint);
            rcvThread = comm.rcvr.start(rcvThreadProc);

            automate();
        }

        void automate()
        {
            ButtonAutomationPeer repo_connect = new ButtonAutomationPeer(RepoStreamConnectButton);
            IInvokeProvider invokeProv = repo_connect.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            invokeProv.Invoke();
            String[] files = Directory.GetFiles("..//..//DLL", "*.dll*", System.IO.SearchOption.AllDirectories);
            foreach (string file in files)
                DLLListBox.Items.Insert(0, file);
            DLLListBox.SelectAll();
            UploadDLLButton.IsEnabled = true;
            ButtonAutomationPeer upload_button = new ButtonAutomationPeer(UploadDLLButton);
            invokeProv = upload_button.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            invokeProv.Invoke();
            tabcontrol1.SelectedIndex = 1;
            ButtonAutomationPeer test_harness_connect = new ButtonAutomationPeer(TestHarnessConnectButton);
            invokeProv = test_harness_connect.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            invokeProv.Invoke();
            testDriverCombo.SelectedIndex = 0;
            testCodeCombo.SelectedIndex = 1;
            ButtonAutomationPeer send_req = new ButtonAutomationPeer(SendRequestButton);
            invokeProv = send_req.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            invokeProv.Invoke();
            tabcontrol1.SelectedIndex = 2;
            tabcontrol1.SelectedIndex = 3;
            QueryText.Text = "pass";
            ButtonAutomationPeer send_query = new ButtonAutomationPeer(SendQueryButton);
            invokeProv = send_query.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            invokeProv.Invoke();
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
                Console.Write("\n\n  Client 2 received message: - #Req 10");
                //msg.showMsg();
                if (msg.type == "TestResult")
                {
                    if (msg.body != null)
                    {
                        Console.WriteLine("\n\n  Received Test Results from Test Harness: - #Req 7");
                        Console.WriteLine("  ----------------------------------------");
                        Console.WriteLine(msg.body);
                        this.Dispatcher.BeginInvoke(
                              System.Windows.Threading.DispatcherPriority.Normal,
                              OnNewTestResultsMessage, 
                              msg.body);
                    }
                }
                if (msg.type == "LogReply")
                {
                    string[] res = msg.body.Split(',');
                    Console.WriteLine("\n\n  Query Results: - #Req 9");
                    Console.WriteLine("  --------------");
                    foreach (string re in res)
                    {
                        Console.WriteLine(re);
                        this.Dispatcher.BeginInvoke(
                          System.Windows.Threading.DispatcherPriority.Normal,
                          OnNewLogsMessage,
                          re);
                    }
                }
                if (msg.type == "RepoFileReply")
                {
                    string[] files = msg.body.Split(',');
                    foreach (string file in files)
                    {
                        this.Dispatcher.BeginInvoke(
                              System.Windows.Threading.DispatcherPriority.Normal,
                              OnNewFileResultsMessage,
                              file);
                    }
                }
                if (msg.body == "quit")
                    break;
            }
        }

        //----< Display Results on WPF Results Tab >------------
        void OnNewTestResultsMessageHandler(string msg)
        {
            statusText.Text = "Results Received.";
            ResultsText.Text = msg;
        }

        //----< Display Query Logs on WPF Logs Tab >------------
        void OnNewLogsMessageHandler(string msg)
        {
            statusText.Text = "Query Logs Received";
            LogsListBox.Items.Add(msg);
        }

        //----< Display Files in Combo Boxes on WPF Test Tab >------------
        void OnNewFileResultsMessageHandler(string msg)
        {
            testCodeCombo.Items.Add(msg);
            testDriverCombo.Items.Add(msg);
        }

        //----< Connect to Repository Stream Service >----------
        private void RepoStreamConnectButton_Click(object sender, RoutedEventArgs e)
        {
            TestTabItem.IsEnabled = true;

            comm.sndr.channel = Sender.CreateServiceChannel("http://localhost:8082/StreamService");        // To Repo

            statusText.Text = "Connected to Repository";
            BrowseDLLButton.IsEnabled = true;
        }

        //----< Browse DLL files >---------------
        private void BrowseDLLButton_Click(object sender, RoutedEventArgs e)
        {
            DLLListBox.Items.Clear();
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            string path = AppDomain.CurrentDomain.BaseDirectory;
            dlg.SelectedPath = path;
            DialogResult result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                path = dlg.SelectedPath;
                String[] files = Directory.GetFiles(path, "*.dll*", System.IO.SearchOption.AllDirectories);
                if (files.Length > 0)
                    UploadDLLButton.IsEnabled = true;
                foreach (string file in files)
                    DLLListBox.Items.Insert(0, file);
            }
        }

        //----< Uploading Files to Repository from Client >---------------
        private void UploadDLLButton_Click(object sender, RoutedEventArgs e)
        {
            Console.Write("\n\n  Uploading files to the Repository - #Req 2,6");
            Console.Write("\n ==================================\n");
            statusText.Text = "Uploading Files to Repository";
            foreach (string file in DLLListBox.SelectedItems)
            {
                string filename = System.IO.Path.GetFileName(file);
                string filepath = "";
                if (file.Contains(filename))
                {
                    filepath = file.Replace(filename, "");
                }
                comm.sndr.ToSendPath = filepath;
                comm.sndr.uploadFile(filename);
            }
            statusText.Text = "Uploaded files to Repository";
        }

        //----< Send Query to Repository >--------------
        private void SendQueryButton_Click(object sender, RoutedEventArgs e)
        {
            // Sending Query to Repository
            Console.Write("\n\n  Sending Query to Repository");
            Console.Write("\n ============================\n");
            string remoteEndPoint = Comm<Client>.makeEndPoint("http://localhost", 8082);
            statusText.Text = "Sending Query to " + remoteEndPoint;
            Message msg1 = makeMessage("Rahul", endPoint, remoteEndPoint);
            msg1.type = "LogQuery";
            msg1.body = QueryText.Text;
            Console.WriteLine("\n  Query: " + QueryText.Text);
            comm.sndr.PostMessage(msg1);
        }

        //----< Send Test Request to the Test Harness >------------
        private void SendRequestButton_Click(object sender, RoutedEventArgs e)
        {
            Console.Write("\n\n  Making Test Request and sending it to Test Harness - #Req2");
            Console.Write("\n ===================================================\n");
            string remoteEndPoint = Comm<Client>.makeEndPoint("http://localhost", 8080);
            statusText.Text = "Sending Test Request to " + remoteEndPoint;
            Message msg = makeMessage("Fawcett", endPoint, remoteEndPoint);
            msg.type = "TestRequest";
            if (testDriverCombo.SelectedItem != null && testCodeCombo.SelectedItem != null)
            {
                msg.body = MessageTest.makeTestRequest(testDriverCombo.SelectedItem.ToString(), testCodeCombo.SelectedItem.ToString());
                Console.WriteLine(msg.body);
                comm.sndr.PostMessage(msg);
                statusText.Text = "Test Request sent to " + remoteEndPoint;
                ResultsTabItem.IsEnabled = true;
            }
            else
            {
                if (testCodeCombo.SelectedItem == null)
                {
                    statusText.Text = "Select a test code library";
                }
                else
                {
                    statusText.Text = "Select a test driver";
                }
            }
        }

        //----< Get the files from Repository >-----------------
        private void TestHarnessConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string remoteEndPoint = Comm<Client>.makeEndPoint("http://localhost", 8082);
            Message msg = makeMessage("Fawcett", endPoint, remoteEndPoint);
            msg.type = "RepoFileQuery";
            msg.body = "";
            comm.sndr.PostMessage(msg);
        }
    }
}
