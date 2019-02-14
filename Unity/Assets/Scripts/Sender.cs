/*
	Tablet VR - Network data package sender.
	leonardomt@gmail.com
	Last edit: 2017-11-21.
*/

using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace TabletVR.Network {
	class Sender : IDisposable {
		List<SingleSender> singleSenders = new List<SingleSender>();
		
		public Sender() {
		}
		
		public void Dispose() {
			Stop();
		}
		
		public void Stop() {
			foreach (SingleSender singleSender in singleSenders)
				singleSender.Dispose();
		}
		
		public bool Setup(string[] ips, int port) {
			bool success = true;
			foreach (string ip in ips) {
				if (!Validate(ip)) {
					success = false;
					break;
				}
			}
			if (success) {
				foreach (SingleSender singleSender in singleSenders)
					singleSender.Dispose();
				singleSenders.Clear();
				
				foreach (string ip in ips)
					singleSenders.Add(new SingleSender(ip, port));
			}
			return success;
		}
		
		public static bool Validate(string ip) {
			bool success = true;
			string[] octets = ip.Split('.');
			if (octets.Length == 4) {
				byte mbyte;
				foreach (string octect in octets) {
					if (!byte.TryParse(octect, out mbyte)) {
						success = false;
						break;
					}
				}
			} else {
				success = false;
			}
			return success;
		}
		
		public void Send(string text) {
			foreach (SingleSender singleSender in singleSenders)
				singleSender.Send(text);
		}
		
		class SingleSender : IDisposable {
			Queue<string> outputs = new Queue<string>();
			readonly object outputLock = new object();
			UdpClient socket;
			bool run = true;
			
			public SingleSender(string ip, int port) {
				socket = new UdpClient(ip, port);
				socket.Client.SendTimeout = 500;
				Thread sendThread = new Thread(new ThreadStart(SendLoop));
				sendThread.IsBackground = true;
				sendThread.Start();
			}
			
			public void Dispose() {
				run = false;
			}
			
			public void Send(string text) {
				lock (outputLock)
					outputs.Enqueue(text);
			}
			
			void SendLoop() {
				while (run) {
					string output = null;
					lock (outputLock) {
						if (outputs.Count > 0)
							output = outputs.Dequeue();
					}
					if (output == null) {
						Thread.Sleep(1);
					} else {
						byte[] bytes = Encoding.UTF8.GetBytes(output);
						try {
							socket.Send(bytes, bytes.Length);
						} catch {
						}
					}
				}
			}
		}
	}
}