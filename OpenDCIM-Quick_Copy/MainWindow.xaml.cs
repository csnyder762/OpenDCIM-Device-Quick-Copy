using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Net;
using System.IO;

using MahApps.Metro.Controls;

namespace OpenDCIM_Quick_Copy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public ObservableCollection<OpenDCIMDevice> AllDevices { get; set; }
        public ObservableCollection<OpenDCIMCabinet> AllCabinets { get; set; }
        public ObservableCollection<OpenDCIMDataCenter> AllDataCenters { get; set; }
        public Brush DefaultColor { get; set; }
        public NetworkCredential MyCredential { get; set; }
        public string Server = "## ENTER A SERVER NAME INTO LINE 30 ##";

        public MainWindow()
        {
            InitializeComponent();

            AllDevices = new ObservableCollection<OpenDCIMDevice>();
            AllCabinets = new ObservableCollection<OpenDCIMCabinet>();
            AllDataCenters = new ObservableCollection<OpenDCIMDataCenter>();

            this.DataContext = this;
        }

        private void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            // string server = "";
            bool credsValid = false;

            do
            {
                Cred prompt = new Cred();
                MyCredential = prompt.GetCredentialsVistaAndUp(Server);

                if (MyCredential != null)
                {
                    OpenDCIMResponse cabinets = Get_OpenDCIMRequest(string.Format("https://{0}/api/v1/cabinet", Server), MyCredential);
                    if (cabinets.Errorcode == "200")
                    {
                        credsValid = true;
                        if (cabinets.Cabinet != null)
                        {
                            foreach (OpenDCIMCabinet cab in cabinets.Cabinet)
                            {
                                AllCabinets.Add(cab);
                            }
                        }
                    }
                }
                else if (MyCredential == null)
                {
                    Environment.Exit(0);
                }
            } while (credsValid == false);

            this.Title = string.Format("OpenDCIM Quick Copy - {0}", Server);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText("### You can edit the values in this box ###");
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText("It's fine if the Device ID is 0 or the current deveice.");
            jsonTextBox.AppendText("You can set the label to whatever you want here.");
            jsonTextBox.AppendText("By default, it will be the same name but with - COPY");
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText("You can search for a cabinet by cabinet ID, cabinet name or cabinet label.");
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText("                       ###### CHANGE lOG ######");
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText(" --- 01-10-21 ---");
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText("Remove the credential manager dependency and added my own.");
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText("Prompt again for credentials on 401 request error.");
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText(" --- 12-28-21 ---");
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText("You are now asked for credentials, as you have seen already.");
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText("After a device is added, it will refresh the Device List and Cabinet View with the new device.");
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            jsonTextBox.AppendText("Added a check for illegal characters in the device label that might cause an error.");
        }


        // Webrequests
        public OpenDCIMResponse Get_OpenDCIMRequest(string url, NetworkCredential cred)
        {
            string encodedCreds = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(cred.UserName + ":" + cred.Password));
            
            try
            {
                var request = HttpWebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add("Authorization", "Basic " + encodedCreds);
                request.UseDefaultCredentials = true;
                request.PreAuthenticate = true;
                request.Credentials = CredentialCache.DefaultNetworkCredentials;

                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                OpenDCIMResponse dcimresponse = JsonConvert.DeserializeObject<OpenDCIMResponse>(responseString);

                return dcimresponse;
            }
            catch (UriFormatException e)
            {
                MessageBox.Show("Either you didn't change the code to your server name or you included an illegal character", "UriFormatException", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
                return null;
            }
            catch (Exception e) // I know, I know, should be handling web exceptions here BUUUUUUUUUUT...yeah
            {
                OpenDCIMResponse response = new OpenDCIMResponse();
                if(e.Message.Contains("Unauthorized"))
                {
                    response.Error = true;
                    response.Errorcode = "401";
                }
                MessageBox.Show(e.Message, "OpenDCIM Get Request Error");
                // outputTextBox.AppendText(e.Message);
                return response;
            }
        }
        public OpenDCIMPutResponse Put_OpenDCIMRequest(string server, OpenDCIMDevice deviceToClone, string deviceLabel, NetworkCredential credential)
        {
            string encodedCreds = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(credential.UserName + ":" + credential.Password));

            var body = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(deviceToClone, Formatting.None));

            var request = HttpWebRequest.Create(string.Format("https://{0}/api/v1/device/{1}", server, deviceLabel));
            request.Method = "PUT";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = body.Length;
            request.Headers.Add("Authorization", "Basic " + encodedCreds);

            using (var stream = request.GetRequestStream())
            {
                stream.Write(body, 0, body.Length);
            }

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                OpenDCIMPutResponse dcimresponse = JsonConvert.DeserializeObject<OpenDCIMPutResponse>(responseString);

                return dcimresponse;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Device Put Request Failure");
                return null;
            }
        }


        // UI Changes
        private void AllCabinetsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid dg = (DataGrid)sender;
            if(dg.SelectedItem != null && (dg.SelectedItem.GetType().Name == "OpenDCIMCabinet"))
            {
                OpenDCIMCabinet selectedCab = (OpenDCIMCabinet)dg.SelectedItem;
                Cabinet_SelectionChange(Server, selectedCab);
            }
        }
        private void Cabinet_SelectionChange(string server, OpenDCIMCabinet selectedCab)
        {
            OpenDCIMResponse wr = Get_OpenDCIMRequest(string.Format("https://{0}/api/v1/device?Cabinet={1}", server, selectedCab.CabinetID), MyCredential);
            if (wr.Errorcode == "200" && wr.Device.Count > 0)
            {
                AllDevices.Clear();
                foreach (OpenDCIMDevice device in wr.Device)
                {
                    AllDevices.Add(device);
                }
                List<int> usedRackUnits = Find_UsedRackUnits(selectedCab, AllDevices.ToList());
                Create_CabinetView(usedRackUnits, selectedCab);
            }
        }
        private void DeviceDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid dg = (DataGrid)sender;
            if(dg.SelectedItem != null && dg.SelectedItem.GetType().Name == "OpenDCIMDevice")
            {
                OpenDCIMDevice device = (OpenDCIMDevice)dg.SelectedItem;
                Set_JSONTextBox(device, null);
            }
        }

        private void Set_JSONTextBox(OpenDCIMDevice device, int? position)
        {
            OpenDCIMDevice clonedDevice = (OpenDCIMDevice)device.Clone();

            if (position != null)
            {
                clonedDevice.Position = (int)position;
            }
            else
            {
                clonedDevice.Position = 0;
            }

            clonedDevice.DeviceId = 0;

            string cDate = DateTime.Now.ToString();
            clonedDevice.Label = string.Format("{0} - COPY", device.Label.Replace('/', '-'));
            clonedDevice.InstallDate = cDate.Split(' ')[0].ToString();
            clonedDevice.SerialNo = "";
            clonedDevice.Notes = string.Format("Cloned device - {0}", cDate);

            string putBody = JsonConvert.SerializeObject(clonedDevice, Formatting.Indented);
            jsonTextBox.Text = putBody;
        }
        private void CabinetSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if(AllCabinets.Count > 0 && tb.Text.Length > 0)
            {
                allCabinetsDataGrid.ItemsSource = AllCabinets.Where(x => (x.CabinetID.ToString() == tb.Text.ToUpper()) || (x.DataCenterName.Contains(tb.Text.ToUpper()) || (x.Location.Contains(tb.Text.ToUpper()))));
            }
        }
        private bool Check_DeviceOverlap(OpenDCIMCabinet selectedCabinet, OpenDCIMDevice device)
        {
            // Requirements, selected device (converted from the text box), cabinet, AllDevices

            List<int> usedRackUnits = Find_UsedRackUnits(selectedCabinet, AllDevices.ToList());
            int deviceStartRU = device.Position;
            int deviceEndRU = device.Position + (device.Height - 1);

            if(usedRackUnits.Contains(deviceStartRU) || usedRackUnits.Contains(deviceEndRU))
            {
                MessageBox.Show(string.Format("Device Overlap on RU {0}", deviceEndRU), "Overlapping Devices", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }
            else if(deviceEndRU > selectedCabinet.CabinetHeight)
            {
                MessageBox.Show(string.Format("Device Rack Unit {0} is above the rack!", deviceEndRU), "Device Too Tall", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }
            else
            {
                return false;
            }
        }
        private void CloneButton_Click(object sender, RoutedEventArgs e)
        {
            // Eventually put some checks in here to make sure there are enough required fields?
            try
            {
                OpenDCIMDevice deviceToClone = JsonConvert.DeserializeObject<OpenDCIMDevice>(jsonTextBox.Text);
                bool overlap = false;
                if (allCabinetsDataGrid.SelectedItem != null && allCabinetsDataGrid.SelectedItem.GetType().Name == "OpenDCIMCabinet")
                {
                    OpenDCIMCabinet cabinet = (OpenDCIMCabinet)allCabinetsDataGrid.SelectedItem;
                    overlap = Check_DeviceOverlap(cabinet, deviceToClone);
                }
                if (overlap == false)
                {
                    if (deviceToClone.Label.Length > 0)
                    {
                        OpenDCIMPutResponse putResponse = Put_OpenDCIMRequest(Server, deviceToClone, deviceToClone.Label, MyCredential);
                        Write_OutputToTextBox(putResponse);
                        if (putResponse.Errorcode != "200")
                        {

                        }
                        else
                        {
                            OpenDCIMCabinet cabinet = (OpenDCIMCabinet)allCabinetsDataGrid.SelectedItem;
                            Cabinet_SelectionChange(Server, cabinet);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Invalid Device Label. Length <= 0.");
                    }
                }
                else
                {
                    // If there is device overlap, here we end up.
                }
            }
            catch
            {
                MessageBox.Show("Something with wrong with Serializing the Object. Please check for invalid JSON");
            }
        }


        // Work
        public void Write_OutputToTextBox(OpenDCIMPutResponse response)
        {
            outputTextBox.AppendText(string.Format("##### {0} #####{1}", DateTime.Now.ToString(), Environment.NewLine));
            outputTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            if (response.Errorcode == "200") {
                outputTextBox.AppendText(string.Format("# SUCCESS #{0}", Environment.NewLine));
                outputTextBox.AppendText(string.Format(" - Response Code     : {0}{1}", response.Errorcode, Environment.NewLine));
                outputTextBox.AppendText(string.Format(" - Error             : {0}{1}", response.Error, Environment.NewLine));
                outputTextBox.AppendText(string.Format(" - New Device ID     : {0}{1}", response.Device.DeviceId, Environment.NewLine));
                outputTextBox.AppendText(string.Format(" - New Device Label  : {0}{1}", response.Device.Label, Environment.NewLine));
                outputTextBox.AppendText(string.Format(" - Device URL        : {0}", Environment.NewLine));
                outputTextBox.AppendText(string.Format("{0}", Environment.NewLine));
                outputTextBox.AppendText(string.Format("https://{0}/devices.php?DeviceID={1}{2}", Server, response.Device.DeviceId, Environment.NewLine));
                outputTextBox.AppendText(string.Format("{0}", Environment.NewLine));
            }
            else {
                outputTextBox.AppendText(string.Format("# FAILURE #{0}", Environment.NewLine));
                outputTextBox.AppendText(string.Format("    - Response Code     : {0}{1}", response.Errorcode, Environment.NewLine));
                outputTextBox.AppendText(string.Format("    - Error             : {0}{1}", response.Error, Environment.NewLine));
            }
            outputTextBox.AppendText(string.Format("##################################{0}", Environment.NewLine));
            outputTextBox.AppendText(string.Format("{0}{1}", Environment.NewLine, Environment.NewLine));
        }
        public void Create_CabinetView(List<int> usedRackUnits, OpenDCIMCabinet inputCabinet)
        {
            cabinetStackPanel.Children.Clear();
            for (int i = 1; i <= inputCabinet.CabinetHeight; i++)
            {
                Border b = new Border();
                b.BorderBrush = Brushes.Black;
                b.BorderThickness = new Thickness(1);
                b.Height = Math.Round(cabinetStackPanelBorder.Height / inputCabinet.CabinetHeight, 2);
                TextBlock txt1 = new TextBlock();
                txt1.Foreground = Brushes.Black;
                txt1.FontSize = 13;
                // txt1.Text = (cabinetHeight - i + 1).ToString();
                txt1.Text = i.ToString();
                //txt1.IsMouseDirectlyOverChanged += CabinetStackPanel_IsMouseDirectlyOverChanged;
                b.Child = txt1;

                if (usedRackUnits.Contains(i))
                {
                    txt1.Background = Brushes.Black;
                    txt1.Foreground = Brushes.White;
                }
                else
                {
                    txt1.MouseEnter += Txt1_MouseEnter;
                    txt1.MouseLeave += Txt1_MouseLeave;
                    txt1.MouseUp += Txt1_MouseUp;
                }

                cabinetStackPanel.Children.Insert(0, b);
                // cabinetStackPanel.Children.Add(b);
            }
        }
        public List<int> Find_UsedRackUnits(OpenDCIMCabinet inputCabinet, List<OpenDCIMDevice> devicesInCabinet)
        {
            int cabinetHeight = inputCabinet.CabinetHeight;
            List<int> usedRackUnits = new List<int>();

            // Get the height of the cabinet stack panel and divide by RU of cabinet then go from there

            foreach(OpenDCIMDevice device in devicesInCabinet.OrderBy(x => x.Position))
            {
                int startPosition = device.Position;
                int endingPositoin = device.Position + device.Height - 1;

                for(int i = 0; i < device.Height; i++)
                {
                    usedRackUnits.Add(i + startPosition);
                }
            }

            return usedRackUnits;
        }

        private void Txt1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender.GetType().Name == "TextBlock")
            {
                TextBlock tb = (TextBlock)sender;
                Console.WriteLine();
                if(jsonTextBox.Text.Length > 0)
                {
                    try
                    {
                        if(int.TryParse(tb.Text, out int position))
                        {
                            OpenDCIMDevice device = JsonConvert.DeserializeObject<OpenDCIMDevice>(jsonTextBox.Text);
                            string deviceNameWithoutCopy = device.Label.Replace(" - COPY", "");
                            device.Label = deviceNameWithoutCopy;
                            Set_JSONTextBox(device, position);
                        }
                        else
                        {
                            MessageBox.Show("Unable to cast clicked rack unit to int");
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Unable to serialize JSON text box when clicking a cabinet RU. Verify a device is selected and JSON is accurate then try again.");
                    }
                }
            }
        }
        private void Txt1_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender.GetType().Name == "TextBlock")
            {
                TextBlock tb = (TextBlock)sender;
                tb.Background = DefaultColor;
            }
        }
        private void Txt1_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender.GetType().Name == "TextBlock")
            {
                TextBlock tb = (TextBlock)sender;
                DefaultColor = tb.Background;
                tb.Background = Brushes.Yellow;
            }
        }


        public class OpenDCIMResponse
        {
            [JsonProperty("error")]
            public bool Error { get; set; }

            [JsonProperty("errorcode")]
            public string Errorcode { get; set; }

            [JsonProperty("device")]
            public List<OpenDCIMDevice> Device { get; set; }

            [JsonProperty("datacenter")]
            public List<OpenDCIMDataCenter> DataCenter { get; set; }

            [JsonProperty("cabinet")]
            public List<OpenDCIMCabinet> Cabinet { get; set; }
        }
        public class OpenDCIMPutResponse
        {
            [JsonProperty("error")]
            public bool Error { get; set; }

            [JsonProperty("errorcode")]
            public string Errorcode { get; set; }

            [JsonProperty("device")]
            public OpenDCIMDevice Device { get; set; }
        }

        public class OpenDCIMDevice : ICloneable
        {
            public object Clone()
            {
                return this.MemberwiseClone();
            }

            [JsonProperty("DeviceID")]
            public int DeviceId { get; set; }

            [JsonProperty("Label")]
            public string Label { get; set; }

            [JsonProperty("Position")]
            public int Position { get; set; }

            [JsonProperty("Cabinet")]
            public long Cabinet { get; set; }

            [JsonProperty("PrimaryIP")]
            public string PrimaryIp { get; set; }

            [JsonProperty("InstallDate")]
            public string InstallDate { get; set; }

            [JsonProperty("Notes")]
            public string Notes { get; set; }

            [JsonProperty("Height")]
            public int Height { get; set; }

            [JsonProperty("Ports")]
            public long Ports { get; set; }

            [JsonProperty("SerialNo")]
            public string SerialNo { get; set; }

            [JsonProperty("AssetTag")]
            public string AssetTag { get; set; }

            [JsonProperty("SNMPVersion")]
            public string SnmpVersion { get; set; }

            [JsonProperty("SNMPCommunity")]
            public string SnmpCommunity { get; set; }

            [JsonProperty("SNMPFailureCount")]
            public long SnmpFailureCount { get; set; }

            [JsonProperty("Hypervisor")]
            public string Hypervisor { get; set; }

            [JsonProperty("APIPort")]
            public long ApiPort { get; set; }

            [JsonProperty("Owner")]
            public long Owner { get; set; }

            [JsonProperty("EscalationTimeID")]
            public long EscalationTimeId { get; set; }

            [JsonProperty("EscalationID")]
            public long EscalationId { get; set; }

            [JsonProperty("PrimaryContact")]
            public long PrimaryContact { get; set; }

            [JsonProperty("FirstPortNum")]
            public long FirstPortNum { get; set; }

            [JsonProperty("TemplateID")]
            public long TemplateId { get; set; }

            [JsonProperty("NominalWatts")]
            public long NominalWatts { get; set; }

            [JsonProperty("PowerSupplyCount")]
            public long PowerSupplyCount { get; set; }

            [JsonProperty("DeviceType")]
            public string DeviceType { get; set; }

            [JsonProperty("ChassisSlots")]
            public long ChassisSlots { get; set; }

            [JsonProperty("RearChassisSlots")]
            public long RearChassisSlots { get; set; }

            [JsonProperty("ParentDevice")]
            public long ParentDevice { get; set; }

            [JsonProperty("WarrantyCo")]
            public string WarrantyCo { get; set; }

            [JsonProperty("Status")]
            public string Status { get; set; }

            [JsonProperty("HalfDepth")]
            public long HalfDepth { get; set; }

            [JsonProperty("BackSide")]
            public long BackSide { get; set; }

            [JsonProperty("Weight")]
            public long Weight { get; set; }

            [JsonProperty("00Custom_Manufacturer")]
            public string The00CustomManufacturer { get; set; }

            [JsonProperty("00Custom_Model")]
            public string The00CustomModel { get; set; }

            [JsonProperty("00Alternate_Name")]
            public string The00AlternateName { get; set; }

            [JsonProperty("20_Cost_Equipment_Name")]
            public string The20_CostEquipmentName { get; set; }

            [JsonProperty("v3SecurityLevel")]
            public string V3SecurityLevel { get; set; }

            [JsonProperty("v3AuthProtocol")]
            public string V3AuthProtocol { get; set; }

            [JsonProperty("v3PrivProtocol")]
            public string V3PrivProtocol { get; set; }

            [JsonProperty("v3PrivPassphrase")]
            public string V3PrivPassphrase { get; set; }

            [JsonProperty("AuditStamp")]
            public string AuditStamp { get; set; }

            [JsonProperty("MfgDate")]
            public DateTimeOffset MfgDate { get; set; }

            [JsonProperty("WarrantyExpire")]
            public DateTimeOffset WarrantyExpire { get; set; }
        }
        public class OpenDCIMDataCenter
        {
            [JsonProperty("DataCenterID")]
            public int DataCenterID { get; set; }

            [JsonProperty("Name")]
            public string Name { get; set; }

            [JsonProperty("DeliveryAddress")]
            public string DeliveryAddress { get; set; }
        }
        public class OpenDCIMCabinet
        {
            [JsonProperty("CabinetID")]
            public int CabinetID { get; set; }

            [JsonProperty("DataCenterID")]
            public string DataCenterID { get; set; }

            [JsonProperty("DataCenterName")]
            public string DataCenterName { get; set; }

            [JsonProperty("Location")]
            public string Location { get; set; }

            [JsonProperty("CabinetHeight")]
            public int CabinetHeight { get; set; }
        }

        public class Cred
        {
            public class CredentialResult
            {
                public string UserName { get; set; }
                public string Password { get; set; }
                public string Domain { get; set; }
            }

            [DllImport("ole32.dll")]
            public static extern void CoTaskMemFree(IntPtr ptr);

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            private struct CREDUI_INFO
            {
                public int cbSize;
                public IntPtr hwndParent;
                public string pszMessageText;
                public string pszCaptionText;
                public IntPtr hbmBanner;
            }


            [DllImport("credui.dll", CharSet = CharSet.Auto)]
            private static extern bool CredUnPackAuthenticationBuffer(int dwFlags,
                                                                       IntPtr pAuthBuffer,
                                                                       uint cbAuthBuffer,
                                                                       StringBuilder pszUserName,
                                                                       ref int pcchMaxUserName,
                                                                       StringBuilder pszDomainName,
                                                                       ref int pcchMaxDomainame,
                                                                       StringBuilder pszPassword,
                                                                       ref int pcchMaxPassword);

            [DllImport("credui.dll", CharSet = CharSet.Auto)]
            private static extern int CredUIPromptForWindowsCredentials(ref CREDUI_INFO notUsedHere,
                                                                         int authError,
                                                                         ref uint authPackage,
                                                                         IntPtr InAuthBuffer,
                                                                         uint InAuthBufferSize,
                                                                         out IntPtr refOutAuthBuffer,
                                                                         out uint refOutAuthBufferSize,
                                                                         ref bool fSave,
                                                                         int flags);



            public NetworkCredential GetCredentialsVistaAndUp(string serverName)
            {
                CREDUI_INFO credui = new CREDUI_INFO();
                credui.pszCaptionText = "Please enter the credentails for " + serverName;
                credui.pszMessageText = serverName;
                credui.cbSize = Marshal.SizeOf(credui);
                uint authPackage = 0;
                IntPtr outCredBuffer = new IntPtr();
                uint outCredSize;
                bool save = false;
                int result = CredUIPromptForWindowsCredentials(ref credui,
                                                               0,
                                                               ref authPackage,
                                                               IntPtr.Zero,
                                                               0,
                                                               out outCredBuffer,
                                                               out outCredSize,
                                                               ref save,
                                                               1 /* Generic */);

                var usernameBuf = new StringBuilder(100);
                var passwordBuf = new StringBuilder(100);
                var domainBuf = new StringBuilder(100);

                int maxUserName = 100;
                int maxDomain = 100;
                int maxPassword = 100;
                if (result == 0)
                {
                    if (CredUnPackAuthenticationBuffer(0, outCredBuffer, outCredSize, usernameBuf, ref maxUserName,
                                                       domainBuf, ref maxDomain, passwordBuf, ref maxPassword))
                    {
                        //TODO: ms documentation says we should call this but i can't get it to work
                        //SecureZeroMem(outCredBuffer, outCredSize);

                        //clear the memory allocated by CredUIPromptForWindowsCredentials 
                        CoTaskMemFree(outCredBuffer);
                        NetworkCredential networkCredential = new NetworkCredential()
                        {
                            UserName = usernameBuf.ToString(),
                            Password = passwordBuf.ToString(),
                            Domain = domainBuf.ToString()
                        };
                        return networkCredential;
                    }
                }

                // networkCredential = null;
                return null;
            }
        }
    }
}
