using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Principal;
using System.Security.Permissions;
using Microsoft.Win32;
using System.Diagnostics;
using System.Collections;
using System.Net.NetworkInformation;
using System.Security;
using System.Management;
using Microsoft.VisualBasic;

namespace MacTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        string newMac;
        int selectedId = 0;
        private Hashtable name = new Hashtable();
        private Hashtable regid = new Hashtable();
        private Hashtable intid = new Hashtable();
        int adapterFound;
        int adapterAssigned;
        private void setStyle1()
        {
            progressBar1.Style = ProgressBarStyle.Blocks;
        }
        private void setStyle2()
        {
            progressBar1.Style = ProgressBarStyle.Marquee;
        }
        public bool IsUserAdministrator()
        {
            bool isAdmin;
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;Console.WriteLine(ex);
            }
            catch(Exception ex)
            {
                isAdmin = false;Console.WriteLine(ex);
            }
            return isAdmin;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedId = comboBox1.SelectedIndex;
            Random ran = new Random();string c="";
            for (int i = 1; i < 13; i++)
            {
                if (i==2||i==4||i==6||i==8||i==10)
                {
                    c += string.Format("{0:X}", ran.Next(0, 16))+"-";
                }
                else
                {
                    c +=string.Format("{0:X}", ran.Next(0, 16));
                }
                
            }
            newMac = c;
            progressBar1.Invoke(new MethodInvoker(setStyle1));
            worker.RunWorkerAsync(true);
        }
        private void refreshList()
        {
            name.Clear();
            regid.Clear();
            intid.Clear();
            comboBox1.Items.Clear();
            adapterFound = 0;
            adapterAssigned = 0;
            int i = 0;
            string[] regKeys = Registry.LocalMachine.OpenSubKey("SYSTEM").OpenSubKey("CurrentControlSet").OpenSubKey("Control").OpenSubKey("Class").OpenSubKey("{4D36E972-E325-11CE-BFC1-08002BE10318}", true).GetSubKeyNames();
            foreach (string s in regKeys)
            {
                Console.WriteLine("->"+ s);
            }
            Console.WriteLine("Getting network adapters.");
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                if (adapter.NetworkInterfaceType.ToString()=="Ethernet"||adapter.NetworkInterfaceType.ToString()=="Wireless80211")
                {
                    Console.WriteLine("->" + adapter.Name + "(ID:" + adapter.Id + ")");
                    adapterFound++;
                }
            }
            Console.WriteLine("Adapters found:" + adapterFound);
            Console.WriteLine("Assigning network interfaces to registry ids..");
            foreach (string  s in regKeys)
            {
                RegistryPermission f = new RegistryPermission(RegistryPermissionAccess.Write, @"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4D36E972-E325-11CE-BFC1-08002BE10318}\\" + s);
                f.Demand();string id = "";
                try
                {
                    id = Registry.LocalMachine.OpenSubKey("SYSTEM").OpenSubKey("CurrentControlSet").OpenSubKey("Control").OpenSubKey("Class").OpenSubKey("{4D36E972-E325-11CE-BFC1-08002BE10318}").OpenSubKey(s).GetValue("NetCfgInstanceId").ToString();

                }
                catch 
                {
                    Console.WriteLine("Registry key\"" + s + "\"is not a network adapter");
                }
                foreach (NetworkInterface adapter in adapters)
                {
                    if (adapter.NetworkInterfaceType.ToString()=="Ethernet"||adapter.NetworkInterfaceType.ToString()=="Wireless80211")
                    {
                        if (id==adapter.Id)
                        {
                            Console.WriteLine(s + "->" + adapter.Id);
                            adapterAssigned++;
                            Console.WriteLine("Getting information for adapter" + s + "...");
                            name.Add(i, adapter.Name);
                            intid.Add(i, adapter.Id);
                            regid.Add(i, s);
                            comboBox1.Items.Add(adapter.Name);
                            i ++;
                        }
                        
                    }
                }
            }
            Console.WriteLine("Adapter assigned:", adapterAssigned);
            if (adapterFound!=adapterAssigned)
            {
                MessageBox.Show("Could not asign all network interface!!");
            }
            else
            {
                if (comboBox1.Items.Count!=adapterAssigned)
                {
                    Console.WriteLine(adapterAssigned + "adapter found,but only" + comboBox1.Items.Count + "in list,scanning again.");
                    System.Threading.Thread.Sleep(1000);
                    refreshList();
                }
                else
                {
                    Console.WriteLine("All adapters have been assigned.");
                }
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Microsoft.Win32.RegistryKey mykey;
                mykey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SYSTEM").OpenSubKey("CurrentControlSet").OpenSubKey("Control").OpenSubKey("Class").OpenSubKey("{4D36E972-E325-11CE-BFC1-08002BE10318}").OpenSubKey(regid[selectedId].ToString(), true);
                bool create = (bool)e.Argument;
                if (create)
                {
                    mykey.SetValue("NetworkAddress",newMac);
                }
                else
                {
                    try
                    {
                        mykey.DeleteValue("NetworkAddress");
                    }
                    catch
                    {
                        Console.WriteLine("Key doesnt exit");
                    }
                }
            }
            catch 
            {
            }
            
            progressBar1.Value = 33;
            Process p = new Process();
            p.StartInfo.FileName = "netsh.exe";
            p.StartInfo.Arguments = "interface set interface name=\"" + name[selectedId].ToString() + "\" admin=disabled";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            Console.WriteLine("Process return:"+ output);

            progressBar1.Value = 66;
            Process p2 = new Process();
            p2.StartInfo.FileName = "netsh.exe";
            p2.StartInfo.Arguments = "interface set interface name=\"" + name[selectedId].ToString() + "\" admin=enabled";
            p2.StartInfo.UseShellExecute = false;
            p2.StartInfo.CreateNoWindow = true;
            p2.StartInfo.RedirectStandardOutput = true;
            p2.Start();
            string output2 = p2.StandardOutput.ReadToEnd();
            Console.WriteLine("Process return:" + output2);

            progressBar1.Value = 100;
            System.Threading.Thread.Sleep(1000);
            progressBar1.Invoke(new MethodInvoker(setStyle2));
            refreshList();
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Value = 0;
            selectedId = -1;
        }

        private void queryWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            refreshList();
        }

        private void queryWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            selectedId = -1;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            queryWorker.RunWorkerAsync();
        }

    }
}
