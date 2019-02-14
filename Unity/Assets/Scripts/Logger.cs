/*
	Tablet VR - Disk logger.
	leonardomt@gmail.com
	Last modified: 2017-12-07.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace TabletVR {
	public class Logger : IDisposable {
		string filename;
		int interval;
		Queue<string> queue = new Queue<string>();
		readonly object queueLock = new object();
		Type lastExceptionType = null;
		bool dispose = false;
		
		public Logger(string filename, float interval = 1f) {
			this.filename = filename;
			this.interval = (int) Math.Round(interval * 1000);
			Thread appendThread;
			appendThread = new Thread(() => AppendThread());
			appendThread.IsBackground = true;
			appendThread.Start();
		}
		
		// Try save buffer within interval, then terminate.
		public void Dispose() {
			dispose = true;
		}
		
		public void Log(string text, params object[] args) {
			#if UNITY_STANDALONE || UNITY_EDITOR
			lock (queueLock)
				queue.Enqueue(string.Format("{0:F4},{1}", Time.realtimeSinceStartup, string.Format(text, args)));
			#endif
		}
		
		void AppendThread() {
			bool last = false;
			bool run = true;
			while (run) {
				Queue<string> buffer;
				lock (queueLock) {
					buffer = queue;
					queue = new Queue<string>();
				}
				if (buffer.Count > 0) {
					string data = String.Join("\n", buffer.ToArray()) + "\n";
					bool success;
					try {
						File.AppendAllText(filename, data);
						success = true;
					} catch (Exception exception) {
						Type exceptionType = exception.GetType();
						if (exceptionType != lastExceptionType)
							lastExceptionType = exceptionType;
						queue = buffer;
						success = false;
					}
					if (success && lastExceptionType != null)
						lastExceptionType = null;
				}
				
				if (last)
					run = false;
				else if (dispose)
					last = true;
				else
					Thread.Sleep(interval);
			}
		}
	}
}