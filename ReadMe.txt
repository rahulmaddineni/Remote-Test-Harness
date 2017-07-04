ReadMe:
=======

Project Demonstration:
---------------------
- I have started a Console application, WPF GUI application, Test Harness and the Repository.
- Console application runs automatically
- I used UIAutomation for WPF to run automatically
- Everythings runs autommatically but there is a small issue with combo box item selection,
  but if you hit the send button in the Test tab of the GUI, it runs normally (sorry for trouble)

About Project:
==============

Project Solution consists of:
----------------------------
Two Client Consoles:
	- Client
	- Client 2
Two WPF GUI Clients:
	- ClientGUI
	- ClientGUI2

ClientGUI2 is automated but ClientGUI is not automated so it can be tested manually.

Steps to test ClientGUI manually:
--------------------------------
In Upload tab:
	- Hit Repository "Connect" button
	- Hit "Browse" button and select the DLL folder next to this folder
	- Select the DLL's from ListBox and hit "Upload" button
In Test tab:
	- Hit Test Harness "Connect" button
	- Select the Test Driver and Tested code with the names containing Test Driver and Tested Code
	  in them respectively from combo boxes
	- Hit "Send" button
In Results tab:
	- can check the results of the test request you sent
In Logs tab:
	- Enter the query in the text box
	- hit "Query" button