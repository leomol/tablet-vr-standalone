/*
	Tablet VR - Arduino IO class.
	leonardomt@gmail.com
	Last modified: 2018-06-15.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using System.IO.Ports;

namespace TabletVR {
	public class Arduino : MonoBehaviour {
		public delegate void SetupHandler(bool success, string message);
		public delegate void DataReceivedHandler(Arduino arduino, sbyte data);
		public event DataReceivedHandler DataReceived;
		
		Queue<SetupRequest> setupRequests = new Queue<SetupRequest>();
		Queue<Action> setupCallbacks = new Queue<Action>();
		Queue<sbyte> inputs = new Queue<sbyte>();
		Queue<byte> outputs = new Queue<byte>();
		// Synchronize collections as they are not thread safe in Net 2.0.
		readonly object setupRequestsLock = new object();
		readonly object setupCallbacksLock = new object();
		readonly object inputsLock = new object();
		readonly object outputsLock = new object();
		
		bool run = true;
		
		void Awake() {
			// Free main thread from IO waits.
			Thread thread = new Thread(new ThreadStart(Loop));
			thread.IsBackground = true;
			thread.Start();
		}
		
		void OnDestroy() {
			run = false;
		}
		
		void Update() {
			// Invoke callbacks in the main thread. Use a copy to unlock thread asap.
			Queue<Action> setupCallbacksCopy;
			lock (setupCallbacksLock) {
				setupCallbacksCopy = new Queue<Action>(setupCallbacks);
				setupCallbacks.Clear();
			}
			while (setupCallbacksCopy.Count > 0)
				setupCallbacksCopy.Dequeue()();
			
			if (DataReceived != null) {
				Queue<sbyte> inputsCopy;
				lock (inputsLock) {
					inputsCopy = new Queue<sbyte>(inputs);
					inputs.Clear();
				}
				while (inputsCopy.Count > 0)
					DataReceived(this, inputsCopy.Dequeue());
			}
		}
		
		public void Stop() {
			lock (setupRequestsLock)
				setupRequests.Enqueue(null);
		}
		
		// Setup a SerialPort connection at the given portName and baudrate.
		// Reopening a port (e.g. to change the baudrate) cannot be performed synchronously.
		// Raise a callback asynchronously, once the status of the connection is known.
		public void Setup(string portName, int baudrate, SetupHandler setupCallback = null) {
			lock (setupRequestsLock)
				setupRequests.Enqueue(new SetupRequest(new SerialPort(@"\\.\" + portName, baudrate, Parity.None, 8, StopBits.One), setupCallback));
		}
		
		public void Write(sbyte output) {
			lock (outputsLock)
				outputs.Enqueue((byte) output);
		}
		
		void Loop() {
			SerialPort serialPort = new SerialPort();
			bool serialPortAvailable = false;
			while (run) {
				lock (setupRequestsLock) {
					while (setupRequests.Count > 0) {
						serialPort.Dispose();
						SetupRequest request = setupRequests.Dequeue();
						if (request == null) {
							serialPortAvailable = false;
						} else {
							serialPort = request.Port;
							string message = "";
							try {
								serialPort.Open();
							} catch (Exception e) {
								message = e.Message;
							}
							if (serialPort.IsOpen) {
								serialPort.DtrEnable = true;								
								serialPortAvailable = true;
								serialPort.ReadTimeout = 1;
							} else {
								serialPort.Dispose();
								serialPortAvailable = false;
							}
							lock (setupCallbacksLock) {
								if (request.SetupCallback != null)
									setupCallbacks.Enqueue(() => request.SetupCallback(serialPortAvailable, message));
							}
						}
					}
				}
				
				if (serialPortAvailable) {
					bool inputAvailable;
					sbyte input;
					// Alternative patterns that will fail:
					//   BytesToRead may return larger values than actually available consequently causing a timeout exception in Read(buffer, 0, bytesToRead).
					//   DataReceived may not be invoked at all in NET 2.0, and if it ever did, it is documented that may not be raised for all bytes received.
					try {
						input = (sbyte) serialPort.ReadByte();
						Debug.Log(input);
						inputAvailable = true;
					} catch (TimeoutException) {
						// Timeout is expected when no input is present.
						input = 0;
						inputAvailable = false;
					} catch {
						input = 0;
						inputAvailable = false;
					}
					
					if (inputAvailable) {
						lock (inputsLock)
							inputs.Enqueue(input);
					}
					
					// Write and remove output data.
					byte[] outputArray;
					lock (outputsLock)
						outputArray = outputs.ToArray();
					
					bool outputAvailable = true;
					if (outputArray.Length > 0) {
						try {
							serialPort.Write(outputArray, 0, outputArray.Length);
							lock (outputsLock)
								outputs.Clear();
						} catch {
							outputAvailable = false;
						}
					}
					
					// Take a breath in the absence of both inputs and outputs.
					if (!inputAvailable && !outputAvailable)
						Thread.Sleep(1);
				} else {
					Thread.Sleep(1);
				}
			}
			serialPort.Dispose();
		}
		
		class SetupRequest {
			public SerialPort Port {get;}
			public SetupHandler SetupCallback {get;}
			
			public SetupRequest(SerialPort port, SetupHandler setupCallback) {
				Port = port;
				SetupCallback = setupCallback;
			}
		}
	}
}