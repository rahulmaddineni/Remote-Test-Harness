/////////////////////////////////////////////////////////////////////
// AnotherTestDriver.cs - defines testing process                  //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * The AnotherTestDriver package tests the TestedCode and implements ITest
 * 
 * Public Functions:
 * -----------------
 * bool test() - Test
 * string getLog() - used for getting the log information from test
 *
 * Public Interfaces:
 * ------------------
 * - ITest
 *
 * Required Files:
 * ---------------
 * - AnotherTestDriver.cs, AnotherTested.cs
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommChannelDemo;

namespace CommChannelDemo
{
  public class AnotherTestDriver : ITest
  {
    public bool test()
    {
      CommChannelDemo.AnotherTested tested = new CommChannelDemo.AnotherTested();
      return tested.myWackyFunction();
    }
    public string getLog()
    {
      return "demo test that always fails";
    }
#if (TEST_ANOTHERTESTDRIVER)
    static void Main(string[] args)
    {
    }
#endif
  }
}
