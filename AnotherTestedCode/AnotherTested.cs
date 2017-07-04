/////////////////////////////////////////////////////////////////////
// AnotherTested.cs - code to test                                 //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * AnotherTested provides a function for TestDriver to test
 * 
 * Public Functions:
 * -----------------
 * bool myWackyFunction() - random function to return false always
 *
 * Public Interfaces:
 * ------------------
 * - ITest
 *
 * Required Files:
 * ---------------
 * - AnotherTested.cs
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommChannelDemo
{
  public class AnotherTested
  {
    public bool myWackyFunction()
    {
      return false;
    }
#if (TEST_TESTED)
    static void Main(string[] args)
    {
    }
#endif
  }
}
