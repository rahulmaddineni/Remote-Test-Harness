# Remote Test Harness
## Overview of Test Harness
- The software application is built using more than one package, each implementing a specific requirement of the system. These applications may be varying from containing small packages for a small application to thousands of packages for a large single application. Testing is required to ensure the quality of the application as the developers can find out about different bugs about the code they developed, so that they can be able to resolve the issues. Testing process can be automated as the number of tests to be performed might be increasing over the development period of the application and the human assistance for these tests might be difficult to provide.
- Test Harness is very useful when there are large systems that needs testing continuously. It provides the environment to run the tests automatically and provides support for continuous integration. Test Harness Server provides automated testing which can perform the tests automatically when Clients provide the test requests and the code that needs to be tested. It does not have any idea about the tests it had to perform, so the test drivers need to be provided for the code that needs to be tested. It performs all the tests and logs the result for each test and sends the results back to the client.
- The process it does the work is Clients provides the Test Request which contains the packages that needs to be tested, the Test Harness sends the package details to the Build Server to build the executables required for the testing purpose of the Test Harness. Build Server responds with the executables when they are ready. The Test Harness performs the tests on isolated environment. Then it stores the results in the Repository Server which can be queried by the Client and other users any point later to the test execution.
Most of the simple testing systems just produce a report. Test Harness Server maintains a database of all test results, and make it possible to browse the results of all runs, and then drill down to the diagnostic output for a particular test. Additionally, there will be a Query Logger where we can get the information related to specific tests rather than the entire Log.

#### Test Harness Usage:
- One or more client(s) will concurrently supply the Test Harness with Test Requests.
- The Test Harness will request test drivers and code to test, as cited in a TestRequest, from the Repository.
- One or more client(s) will concurrently extract Test Results and logs by enqueuing requests and displaying Test Harness and/or Repository replies when they arrive.
- The TestHarness, Repository, and Clients are all distinct projects that compile to separate executables. All communication between
these processes will be based on messagepassing Windows Communication Foundation (WCF) channels.
- Client activities will be defined by user actions in a Windows Presentation Foundation (WPF) user interface.

## Demonstration:

- Start a Console application, WPF GUI application, Test Harness and the Repository.
- Console application runs automatically
- I used UIAutomation for WPF to run automatically
- Everythings runs autommatically but there is a small issue with combo box item selection,
  but if you hit the send button in the Test tab of the GUI, it runs normally (sorry for trouble)

### Project Solution consists of:

Two Client Consoles:
- Client
- Client 2

Two WPF GUI Clients:
- ClientGUI
- ClientGUI2

ClientGUI2 is automated but ClientGUI is not automated so it can be tested manually.

#### Steps to test ClientGUI manually:

In Upload tab:
- Hit Repository "Connect" button
- Hit "Browse" button and select the DLL folder next to this folder
- Select the DLL's from ListBox and hit "Upload" button

In Test tab:
- Hit Test Harness "Connect" button
- Select the Test Driver and Tested code with the names containing Test Driver and Tested Code in them respectively from combo boxes
- Hit "Send" button

In Results tab:
- can check the results of the test request you sent

In Logs tab:
- Enter the query in the text box
- hit "Query" button

## Output

#### Start Up
![Start Up](https://github.com/rahulmaddineni/Remote-Test-Harness/blob/master/Screenshots/StartUp.PNG)

#### Upload TestRequests and TestCodes to Repo
![Upload](https://github.com/rahulmaddineni/Remote-Test-Harness/blob/master/Screenshots/Upload.PNG)

#### Sending TestRequests along with TestCode to be tested by Test Harness from Repo
![Sending requests](https://github.com/rahulmaddineni/Remote-Test-Harness/blob/master/Screenshots/Sending%20Tests.PNG)

#### Results for Request received from Test Harness
![Results](https://github.com/rahulmaddineni/Remote-Test-Harness/blob/master/Screenshots/Results.PNG)

#### Query the logs
![Query](https://github.com/rahulmaddineni/Remote-Test-Harness/blob/master/Screenshots/Results.PNG)
