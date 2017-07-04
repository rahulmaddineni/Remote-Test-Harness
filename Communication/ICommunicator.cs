/////////////////////////////////////////////////////////////////////
// ICommunicator.cs - Peer-To-Peer Communicator Service Contract   //
// ver 1.0                                                           //
// Language:    C#, Visual Studio 2015                               //
// Application: Remote Test Harness,                                 //
//				CSE681 - Software Modeling & Analysis                //
// Author:      Rahul Maddineni, Syracuse University                 //
// Source:      Jim Fawcett, Syracuse University                     //
///////////////////////////////////////////////////////////////////////
/*
 * Maintenance History:
 * ====================
 * ver 1.0 : 18 Nov 2016
 * - first release
 */

using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.IO;

namespace CommChannelDemo
{
  [ServiceContract(Namespace = "http://CommChannelDemo")]
  public interface ICommunicator
  {
    [OperationContract(IsOneWay = true)]
    void PostMessage(Message msg);

    // used only locally so not exposed as service method

    Message GetMessage();
    
    [OperationContract(IsOneWay=true)]
    void upLoadFile(FileTransferMessage msg);

    [OperationContract]
    Stream downLoadFile(string filename);
  }

  // The class Message is defined in CommChannelDemo.Messages as [Serializable]
  // and that appears to be equivalent to defining a similar [DataContract]
  [MessageContract]
  public class FileTransferMessage
  {
    [MessageHeader(MustUnderstand = true)]
    public string filename { get; set; }

    [MessageBodyMember(Order = 1)]
    public Stream transferStream { get; set; }
  }

}
