# OpenDCIM_Quick_Copy
WPF / C# based application using MahApps.Metro for the UI to more quickly copy an existing OpenDCIM device to create a new one using the API. OpenDCIM can become slow when containing a large number of devices while the API, in my experience, remains almost instant. This is especially helpful when building out racks full of similar equipment.

The initial prompt for credentials does not rely on any dependencies. I modified code found here https://stackoverflow.com/questions/4134882/show-authentication-dialog-in-c-sharp-for-windows-vista-7 that imports the User32.dll and modified it as necessary. If credentials are null (Cancel or X), application will exit. Otherwise, it will attempt to make an API request to get all cabinets. If the request is successful, the application will continue otherwise a message box will appear with the error from the failed API request.


### Change the server name on line 30 of MainWindow.xaml.cs ###


Cabinets will be listed in the top left box
  - You can search by CabinetID or Cabinet Label

Select a cabinet and both the Devices (Bottom Left) and Cabinet View (Right Side) will be populated

Cabinet View
  - The cabinet view will show in black, all used Rack Units and in white, all available Rack Units
  - If a device is selected and the JSON text box is filled with the devices information, selected an open rack unit will change the JSON devices position to the selected rack unit

JSON Text Box
  - The JSON in this box can be modified
  - By default, the device label will be the same as the selected device but " - COPY" will be added

Clone Device! Button
  - This will attempt to seralize a valid OpenDCIMDevice object from what is in the JSON text box
    - If successful, it will then make a POST API request to clone the device to the new position
  - If there is device overlap, it will prevent you from copying the device to that RU
  - If the device goes above the top of the rack, you will be prevented from copying the device
  - The API response will be in the OUTPUT Text Box

OUTPUT Text Box
  - The out, success or fail of the API PUT copy request
  - If successful, the new device ID and direct link will appear


