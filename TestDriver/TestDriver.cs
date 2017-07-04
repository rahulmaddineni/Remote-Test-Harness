/////////////////////////////////////////////////////////////////////
// TestDriver.cs - defines testing process                         //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////

/*
 * Package Operations:
 * -------------------
 * The TestDriver package tests the TestedCode and implements ITest
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
 * - TestDriver.cs, Tested.cs
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommChannelDemo
{
    public class TestDriver : ITest
    {
        public bool test()
        {
            CommChannelDemo.Tested tested = new CommChannelDemo.Tested();
            return tested.myWackyFunction();
        }
        public string getLog()
        {
            return "demo test that always passes";
        }
#if (TEST_TESTDRIVER)
        static void Main(string[] args)
        {
        }
#endif
    }
}
