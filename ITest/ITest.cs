///////////////////////////////////////////////////////////////////////
// ITest.cs - interfaces for communication between system parts      //
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
 * ITest.cs provides interfaces:
 * - ITestHarness   used by TestExec and Client
 * - ICallback      used by child AppDomain to send messages to TestHarness
 * - IRequestInfo   used by TestHarness
 * - ITestInfo      used by TestHarness
 * - ILoadAndTest   used by TestHarness
 * - ITest          used by LoadAndTest
 * - IRepository    used by Client and TestHarness
 * - IClient        used by TestExec and TestHarness
 *
 * Required files:
 * ---------------
 * - ITest.cs
 * 
 * Maintanence History:
 * --------------------
 * ver 1.0 : 18 Nov 2016
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommChannelDemo
{
    /////////////////////////////////////////////////////////////
    // used by child AppDomain to send messages to TestHarness

    public interface ICallback
    {
        void sendMessage(Message msg);
    }
    public interface ITestHarness
    {
        void setClient(IClient client);
        void sendTestRequest(Message testRequest);
        Message sendMessage(Message msg);
    }
    /////////////////////////////////////////////////////////////
    // used by child AppDomain to invoke test driver's test()

    public interface ITest
    {
        bool test();
        string getLog();
    }
    /////////////////////////////////////////////////////////////
    // used by child AppDomain to communicate with Repository
    // via TestHarness Comm

    public interface IRepository
    {
        void getFileNames(string to);
        void sendLog(List<string> log, string to);
        List<string> queryLogs(string queryText);
    }
    /////////////////////////////////////////////////////////////
    // used by child AppDomain to send results to client
    // via TestHarness Comm

    public interface IClient
    {
        void sendResults(Message result);
        void makeQuery(string queryText);
    }
    /////////////////////////////////////////////////////////////
    // used by TestHarness to communicate with child AppDomain

    public interface ILoadAndTest
    {
        ITestResults test(IRequestInfo requestInfo);
        void loadPath(string path);
    }
    public interface ITestInfo
    {
        string testName { get; set; }
        List<string> files { get; set; }
    }
    public interface IRequestInfo
    {
        string tempDirName { get; set; }
        List<ITestInfo> requestInfo { get; set; }
    }
    public interface ITestResult
    {
        string testName { get; set; }
        string testResult { get; set; }
        string testLog { get; set; }
    }
    public interface ITestResults
    {
        string testKey { get; set; }
        DateTime dateTime { get; set; }
        List<ITestResult> testResults { get; set; }
    }
}
