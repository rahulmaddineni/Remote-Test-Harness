/////////////////////////////////////////////////////////////////////
// Tested.cs - code to test                                        //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * Tested provides a function for TestDriver to test
 * 
 * Public Functions:
 * -----------------
 * bool myWackyFunction() - random function to return true always
 *
 * Public Interfaces:
 * ------------------
 * - ITest
 *
 * Required Files:
 * ---------------
 * - Tested.cs
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommChannelDemo
{
  public class Tested
  {
    public bool myWackyFunction()
    {
      return true;
    }
#if (TEST_TESTED)
    static void Main(string[] args)
    {
    }
#endif
  }
}
