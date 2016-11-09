using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoNoteMiner
{
	public partial class Main : Form
	{
		bool platform64bit;

		string simplewalletPath;
		string cpuminerPath;

		string walletName;
		string walletPath;
		string address;

		List<Process> minerProcesses = new List<Process>();

		string miningBtnStart;
		string miningBtnStop;

		SynchronizationContext _syncContext;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);
		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

		public Main()
		{
			InitializeComponent();

			_syncContext = SynchronizationContext.Current;

			miningBtnStart = buttonStartMining.Text;
			miningBtnStop = "Stop Mining";

			platform64bit = ArchitectureCheck.Is64Bit();

			string platformString = platform64bit ? "64bit" : "32bit";

			simplewalletPath = AppDomain.CurrentDomain.BaseDirectory + @"binaries\simplewallet\" + platformString + @"\simplewallet.exe";
			cpuminerPath = AppDomain.CurrentDomain.BaseDirectory + @"binaries\cpuminer\" + platformString + @"\minerd.exe";

			walletName = "xdnwallet5";
			walletPath = AppDomain.CurrentDomain.BaseDirectory + walletName + ".address";

			if (!File.Exists(simplewalletPath))
			{
				MessageBox.Show("Missing " + simplewalletPath, "CryptoNoteMiner", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Process.GetCurrentProcess().Kill();
			}

			if (!File.Exists(cpuminerPath))
			{
				MessageBox.Show("Missing " + cpuminerPath, "CryptoNoteMiner", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Process.GetCurrentProcess().Kill();
			}

			if (!File.Exists(walletPath))
			{
				GenerateWallet();
			}
			else
			{
				ReadWalletAddress();
			}

			var coresAvailable = Environment.ProcessorCount;
			for (var i = 0; i < coresAvailable; i++)
			{
				string text = (i + 1).ToString();
				if (i + 1 == coresAvailable) text += " (max)";
				comboBoxCores.Items.Add(text);
			}

			var coresConfig = INI.Value("cores");
			int coresInt = comboBoxCores.Items.Count - 1;
			if (coresConfig != "")
			{
				int coresParsed;
				var parsed = int.TryParse(coresConfig, out coresParsed);
				if (parsed) coresInt = coresParsed - 1;
				if (coresInt + 1 > coresAvailable) coresInt = coresAvailable - 1;

			}
			comboBoxCores.SelectedIndex = coresInt;

			var poolHost = INI.Value("pool_host");
			if (poolHost != "")
			{
				textBoxPoolHost.Text = poolHost;
			}
			var poolPort = INI.Value("pool_port");
			if (poolPort != "")
			{
				textBoxPoolPort.Text = poolPort;
			}

			Application.ApplicationExit += (s, e) => killMiners();

		}

		void ReadWalletAddress()
		{
			address = File.ReadAllText(walletPath);
			_syncContext.Post(_ =>
			{
				textBoxAddress.Text = address;
			}, null);

		}

		WalletProcess wp = new WalletProcess();
		List<string> errorOutput = new List<string>();

		void GenerateWallet()
		{
			MessageBox.Show("Generating new wallet with the password: x\nPlease wait untill address shows up.", "CryptoNoteMiner", MessageBoxButtons.OK, MessageBoxIcon.Information);
			wp.Exited += process_Exited;
			wp.ErrorDataReceived += process_ErrorDataReceived;
			wp.OutputDataReceived += process_OutputDataReceived;
			wp.Start(simplewalletPath, walletName);
		}

		void process_Exited(object sender, EventArgs e)
		{
			if (errorOutput.Count > 0)
				MessageBox.Show("Failed to generate new wallet:\n" + string.Join("\n", errorOutput), "CryptoNoteMiner", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
				return;

			errorOutput.Add(e.Data);
		}

		void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
				return;

			if (e.Data.Contains("generated"))
			{
				wp.Kill();
				ReadWalletAddress();
			}
		}

		void startMiningProcesses()
		{
			var args = new ArrayList(new[] {
				"-a cryptonight",
				"-o stratum+tcp://" + textBoxPoolHost.Text + ':' + textBoxPoolPort.Text,
				"-u " + address,
				"-p x"
			});
			var cores = comboBoxCores.SelectedIndex + 1;
			if (cores != comboBoxCores.Items.Count)
			{
				args.Add("-t " + cores);
			}

			startMiningProcess((string[])args.ToArray(typeof(string)), cores);

		}

		void startMiningProcess(string[] args, int cores)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo(cpuminerPath, String.Join(" ", args));

			Process process = new Process();
			minerProcesses.Add(process);
			process.StartInfo = startInfo;
			process.EnableRaisingEvents = true;
			process.Exited += (s, e) =>
			{
				Log("Miner died");
				minerProcesses.Remove(process);
				if (minerProcesses.Count == 0)
				{
					_syncContext.Post(_ =>
					{
						if (buttonStartMining.Text != miningBtnStart)
							buttonStartMining.PerformClick();
					}, null);
				}
			};

			process.Start();

			IntPtr ptr = IntPtr.Zero;
			while ((ptr = process.MainWindowHandle) == IntPtr.Zero || process.HasExited) ;

			SetParent(process.MainWindowHandle, panel1.Handle);
			MoveWindow(process.MainWindowHandle, 0, 0, panel1.Width, panel1.Height - 20, true);

			Log("Miner started on " + cores + " cores");
		}


		void Log(string text)
		{
			if (text == null) return;
			_syncContext.Post(_ =>
			{
				textBoxLog.AppendText(Environment.NewLine + text);
				textBoxLog.SelectionStart = textBoxLog.Text.Length;
				textBoxLog.ScrollToCaret();
			}, null);
		}

		void SaveINI()
		{
			INI.Config(
				"pool_host", textBoxPoolHost.Text,
				"pool_port", textBoxPoolPort.Text,
				"cores", (comboBoxCores.SelectedIndex + 1).ToString()
			);
		}

		void killMiners()
		{
			foreach (Process process in minerProcesses)
			{
				if (!process.HasExited)
					process.Kill();
			}
			minerProcesses.Clear();
		}

		private void buttonStartMining_Click(object sender, EventArgs e)
		{
			if (buttonStartMining.Text == miningBtnStart)
			{
				SaveINI();
				buttonStartMining.Text = miningBtnStop;
				textBoxPoolHost.Enabled = textBoxPoolPort.Enabled = comboBoxCores.Enabled = false;
				startMiningProcesses();
			}
			else
			{
				buttonStartMining.Text = miningBtnStart;
				textBoxPoolHost.Enabled = textBoxPoolPort.Enabled = comboBoxCores.Enabled = true;
				killMiners();
			}
		}



	}
}
