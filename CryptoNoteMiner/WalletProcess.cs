using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CryptoNoteMiner
{
	class WalletProcess
	{
		Process process;

		public void Start(string simplewalletPath, string walletName)
		{
			process = new Process();

			var args = new[] {
				"--generate-new-wallet",
				"\"" + AppDomain.CurrentDomain.BaseDirectory + walletName +"\"",
				"--password x"
			};

			Console.WriteLine(String.Join(" ", args));
			ProcessStartInfo psi = new ProcessStartInfo(simplewalletPath, String.Join(" ", args))
			{
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			process.StartInfo = psi;
			process.EnableRaisingEvents = true;
			process.OutputDataReceived += OutputDataReceived;
			process.ErrorDataReceived += ErrorDataReceived;
			process.Exited += Exited;

			process.StartInfo.RedirectStandardOutput = true;

			process.Start();
			process.BeginErrorReadLine();
			process.BeginOutputReadLine();
		}

		public void Kill()
		{
			if (!process.HasExited)
				process.Kill();
		}

		public DataReceivedEventHandler OutputDataReceived;
		public DataReceivedEventHandler ErrorDataReceived;
		public EventHandler Exited;
	}
}
