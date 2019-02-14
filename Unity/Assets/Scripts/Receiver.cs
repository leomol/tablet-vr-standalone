/*
	Tablet VR - Network data package receiver.
	leonardomt@gmail.com
	Last modified: 2017-12-07.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace TabletVR.Network {
	class Receiver : MonoBehaviour {
		public delegate void DataReceivedHandler(Receiver receiver, string data);
		public event DataReceivedHandler DataReceived;
		
		Queue<string> inputs = new Queue<string>();
		readonly object inputsLock = new object();
		readonly object socketLock = new object();
		UdpClient targetSocket = null;
		int targetPortNumber = 0;
		bool run = true;
		
		void Awake() {
			Thread thread = new Thread(new ThreadStart(ReceiveLoop));
			thread.IsBackground = true;
			thread.Start();
		}
		
		void Update() {
			// Invoke callbacks in the main thread. Use a copy to unlock thread asap.
			if (DataReceived != null) {
				Queue<string> inputsCopy;
				lock (inputsLock) {
					inputsCopy = new Queue<string>(inputs);
					inputs.Clear();
				}
				while (inputsCopy.Count > 0)
					DataReceived(this, inputsCopy.Dequeue());
			}
		}
		
		void OnDestroy() {
			run = false;
		}
		
		public void Stop() {
			lock (socketLock)
				targetSocket = null;
		}
		
		public bool Setup(int portNumber) {
			bool success;
			if (portNumber == targetPortNumber) {
				success = true;
			} else {
				lock (socketLock) {
					try {
						targetSocket = new UdpClient(portNumber);
						targetPortNumber = portNumber;
						success = true;
					} catch {
						success = false;
					}
				}
			}
			return success;
		}
		
		void ReceiveLoop() {
			UdpClient socket = null;
			while (run) {
				lock (socketLock) {
					// Handle setup requests.
					socket = targetSocket;
					if (socket != null)
						socket.Client.ReceiveTimeout = 250;
				}
				
				if (socket == null) {
					Thread.Sleep(1);
				} else {
					bool available;
					string input;
					IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
					try {
						input = Encoding.UTF8.GetString(socket.Receive(ref ep));
						available = true;
					} catch {
						input = "";
						available = false;
					}
					if (available) {
						lock (inputsLock)
							inputs.Enqueue(input);
					} else {
						Thread.Sleep(1);
					}
				}
			}
		}
	}
}