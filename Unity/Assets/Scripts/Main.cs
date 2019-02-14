/*
	Tablet VR - Main class defining GUIs and behaviour.
	leonardomt@gmail.com
	Last modified: 2018-01-15.
*/

using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace TabletVR {
	using TabletVR.Network;
	public class Main : MonoBehaviour {
		delegate bool TestHandler(string value);
		
		// Device modes.
		enum DeviceModes {Control, Monitor};
		
		// Application components.
		Arduino arduino;
		Logger logger;
		Receiver receiver;
		Sender sender = new Sender();
		
		// UI components.
		Canvas menuCanvas;
		RawImage intertrialImage;
		Text consoleText;
		Text viewAngleText;
		Text deviceModeText;
		GameObject environment;
		
		// State variables.
		float wheelZ = 0f;
		float wheelFactor = 1f;
		float[] viewAngles = new float[]{0, 110, 250};
		float viewAngle = 0f;
		int viewAngleIndex = 0;
		bool intertrial = false;
		
		// Default settings.
		DeviceModes deviceMode = DeviceModes.Control;
		int wheelSteps = 1000;
		float wheelRadius = 5f;
		float intertrialDuration = 1f;
		float startPosition = 0f;
		float finishPosition = 100f;
		string serialName = "COM3";
		string ips = "";
		
		const int port = 32000;
		const int baudrate = 115200;
		const int rewardPin = 54;
		const string outputFolder = @"C:\Users\Public\Documents";
		const string version = "20180115";
		const float keyboardSpeed = 10f;
		
		void Awake() {
			// Attach communication components.
			receiver = gameObject.AddComponent<Receiver>();
			receiver.DataReceived += (source, message) => OnDataReceivedFromNetwork(message);
			arduino = gameObject.AddComponent<Arduino>();
			arduino.DataReceived += (source, data) => {
				OnDataReceivedFromArduino(data);
			};
			
			// Setup UI components.
			menuCanvas = GameObject.Find("MenuCanvas").GetComponent<Canvas>();
			intertrialImage = GameObject.Find("Blank").GetComponent<RawImage>();
			consoleText = GameObject.Find("Console").GetComponent<Text>();
			
			InputField startPositionInput = GameObject.Find("StartPosition").GetComponent<InputField>();
			startPosition = PlayerPrefs.GetFloat(startPositionInput.name, startPosition);
			SetupCombo(GameObject.Find("SetStartPosition").GetComponent<Button>(), startPositionInput, startPosition.ToString(), TestFloat,
				() => {
					startPosition = float.Parse(startPositionInput.text);
					PlayerPrefs.SetFloat(startPositionInput.name, startPosition);
					PlayerPrefs.Save();
				},
				() => transform.position = new Vector3(transform.position.x, transform.position.y, startPosition)
			);
			
			InputField finishPositionInput = GameObject.Find("FinishPosition").GetComponent<InputField>();
			finishPosition = PlayerPrefs.GetFloat(finishPositionInput.name, finishPosition);
			SetupCombo(GameObject.Find("SetFinishPosition").GetComponent<Button>(), finishPositionInput, finishPosition.ToString(), TestFloat,
				() => {
					finishPosition = float.Parse(finishPositionInput.text);
					PlayerPrefs.SetFloat(finishPositionInput.name, finishPosition);
					PlayerPrefs.Save();
				},
				() => transform.position = new Vector3(transform.position.x, transform.position.y, finishPosition)
			);
				
			InputField intertrialDurationInput = GameObject.Find("IntertrialDuration").GetComponent<InputField>();
			intertrialDuration = PlayerPrefs.GetFloat(intertrialDurationInput.name, intertrialDuration);
			SetupCombo(GameObject.Find("SetIntertrialDuration").GetComponent<Button>(), intertrialDurationInput, intertrialDuration.ToString(), TestPositiveFloat,
				() => {
					intertrialDuration = float.Parse(intertrialDurationInput.text);
					PlayerPrefs.SetFloat(intertrialDurationInput.name, intertrialDuration);
					PlayerPrefs.Save();
				},
				() => StartIntertrial()
			);
			
			InputField wheelRadiusInput = GameObject.Find("WheelRadius").GetComponent<InputField>();
			wheelRadius = PlayerPrefs.GetFloat(wheelRadiusInput.name, wheelRadius);
			SetupCombo(GameObject.Find("SetWheelRadius").GetComponent<Button>(), wheelRadiusInput, wheelRadius.ToString(), TestFloat,
				() => {
					wheelRadius = float.Parse(wheelRadiusInput.text);
					wheelFactor = 2 * Mathf.PI / wheelSteps * wheelRadius;
					PlayerPrefs.SetFloat(wheelRadiusInput.name, wheelRadius);
					PlayerPrefs.Save();
				}
			);
			
			InputField wheelStepsInput = GameObject.Find("WheelSteps").GetComponent<InputField>();
			wheelSteps = (int) PlayerPrefs.GetFloat(wheelStepsInput.name, wheelSteps);
			SetupCombo(GameObject.Find("SetWheelSteps").GetComponent<Button>(), wheelStepsInput, wheelSteps.ToString(), TestInt,
				() => {
					wheelSteps = int.Parse(wheelStepsInput.text);
					wheelFactor = 2 * Mathf.PI / wheelSteps * wheelRadius;
					PlayerPrefs.SetFloat(wheelStepsInput.name, wheelSteps);
					PlayerPrefs.Save();
				}
			);
				
			InputField serialNameInput = GameObject.Find("SerialName").GetComponent<InputField>();
			Button serialNameButton = GameObject.Find("SetSerialName").GetComponent<Button>();
			Text serialNameButtonText = serialNameButton.GetComponentInChildren<Text>();
			serialNameButtonText.text = "Connect";
			serialName = PlayerPrefs.GetString(serialNameInput.name, serialName);
			serialNameInput.text = serialName;
			bool serialConnected = false;
			serialNameInput.onValidateInput += 
				delegate(string input, int charIndex, char addedChar) {
					if (serialConnected) {
						serialNameButton.interactable = true;
						serialConnected = false;
						arduino.Stop();
					}
					serialNameButtonText.text = "Connect";
					return addedChar;
				};
			serialNameButton.onClick.AddListener(
				delegate {
					if (serialConnected) {
						serialConnected = false;
						serialNameButtonText.text = "Connect";
						arduino.Stop();
					} else {
						serialNameButton.interactable = false;
						arduino.Setup(serialNameInput.text, baudrate, 
							(connected, message) => {
								serialNameButton.interactable = true;
								if (connected) {
									serialConnected = true;
									serialNameButtonText.text = "Disconnect";
								} else {
									Log(string.Format("Unable to connect to '{0}': {1}", serialNameInput.text, message.Trim()));
								}
							}
						);
						// Always save regarless of connection outcome.
						serialName = serialNameInput.text;
						PlayerPrefs.SetString(serialNameInput.name, serialName);
						PlayerPrefs.Save();
					}
				}
			);
			
			InputField ipsInput = GameObject.Find("IPs").GetComponent<InputField>();
			Button ipsButton = GameObject.Find("SetIPs").GetComponent<Button>();
			Text ipsButtonText = ipsButton.GetComponentInChildren<Text>();
			ipsButtonText.text = "Connect";
			ips = PlayerPrefs.GetString(ipsInput.name, ips);
			ipsInput.text = ips;
			bool senderConnected = false;
			ipsInput.onValidateInput += 
				delegate(string input, int charIndex, char addedChar) {
					if (senderConnected) {
						senderConnected = false;
						sender.Stop();
					}
					ipsButtonText.text = "Connect";
					return addedChar;
				};
			ipsButton.onClick.AddListener(
				delegate {
					if (senderConnected) {
						senderConnected = false;
						ipsButtonText.text = "Connect";
						sender.Stop();
					} else {
						bool success = true;
						string[] ipArray = Regex.Split(ipsInput.text, @"[\s,]+");
						foreach (string ip in ipArray) {
							if (ip.Trim() != String.Empty && !Sender.Validate(ip)) {
								Log(string.Format("'{0}' is not a valid IP address.", ip));
								success = false;
							}
						}
						if (success) {
							senderConnected = true;
							ipsButtonText.text = "Disconnect";
							ips = ipsInput.text;
							PlayerPrefs.SetString(ipsInput.name, ips);
							PlayerPrefs.Save();
							sender.Setup(ipArray, port);
						}
					}
				}
			);
			
			viewAngleText = GameObject.Find("ViewAngle").GetComponentInChildren<Text>();
			Button viewAngleButton = GameObject.Find("SetViewAngle").GetComponent<Button>();
			viewAngleIndex = PlayerPrefs.GetInt("ViewAngleIndex", viewAngleIndex);
			ViewAngle = viewAngles[viewAngleIndex];
			// Rotate views.
			viewAngleButton.onClick.AddListener(
				() => {
					viewAngleIndex = (viewAngleIndex + 1) % viewAngles.Length;
					ViewAngle = viewAngles[viewAngleIndex];
					PlayerPrefs.SetInt("ViewAngleIndex", viewAngleIndex);
					PlayerPrefs.Save();
				}
			);
			
			deviceModeText = GameObject.Find("DeviceMode").GetComponentInChildren<Text>();
			Button deviceModeButton = GameObject.Find("SetDeviceMode").GetComponent<Button>();
			deviceMode = (DeviceModes) Enum.Parse(typeof(DeviceModes), PlayerPrefs.GetString("DeviceMode", deviceMode.ToString()));
			DeviceMode = deviceMode;
			// Alternate device modes.
			deviceModeButton.onClick.AddListener(
				() => {
					switch (DeviceMode) {
						case DeviceModes.Control:
							DeviceMode = DeviceModes.Monitor;
							deviceModeText.text = "Monitor";
							break;
						case DeviceModes.Monitor:
							DeviceMode = DeviceModes.Control;
							deviceModeText.text = "Control";
							break;
					}
					PlayerPrefs.SetString("DeviceMode", DeviceMode.ToString());
					PlayerPrefs.Save();
				}
			);
			
			
			wheelFactor = 2 * Mathf.PI / wheelSteps * wheelRadius;
			transform.position = new Vector3(transform.position.x, transform.position.y, startPosition);
			
			if (!receiver.Setup(port))
				Log(string.Format("Port {0} is unavailable in this device and remote data won't reach this device.", port));
			
			string filename = System.IO.Path.Combine(outputFolder, string.Format("VR{0}.csv", System.DateTime.Now.ToString("yyyyMMddHHmmss")));
			logger = new Logger(filename);
			Log("Version: " + version);
			Log("About: leonardomt@gmail.com");
			Log("Filename: " + filename);
		}
		
		void OnDestroy() {
			logger.Dispose();
			sender.Dispose();
		}
		
		void Update() {
			if (DeviceMode == DeviceModes.Control) {
				if (!intertrial && !menuCanvas.enabled) {
					// transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y + Input.GetAxis("Horizontal"), transform.localEulerAngles.z);
					Vector3 change = new Vector3(0f, 0f, Input.GetAxis("Vertical"));
					change = Time.deltaTime * keyboardSpeed * change;
					transform.position = transform.position + transform.rotation * change;
					//transform.position = transform.position + keyboardSpeed * Input.GetAxis("Vertical") * Time.deltaTime * transform.forward;
				}
				
				if (transform.hasChanged) {
					transform.hasChanged = false;
					sender.Send(string.Format("z,{0:F4}", transform.position.z));
					logger.Log("virtual-position,{0:F4}", transform.position.z);
				}
				
				if (transform.position.z >= finishPosition) {
					if (!intertrial) {
						Reward();
						StartIntertrial();
					}
				}
			}
			if (Input.GetKeyDown(KeyCode.Escape)) {
				if (intertrial)
					InterruptIntertrial();
				else
					menuCanvas.enabled = !menuCanvas.enabled;
			}
		}
		
		bool TestFloat(string text) {
			float number;
			return float.TryParse(text, out number);
		}
		
		bool TestPositiveFloat(string text) {
			float number;
			return float.TryParse(text, out number) && number >= 0;
		}
		
		bool TestInt(string text) {
			int number;
			return int.TryParse(text, out number);
		}
		
		void StartIntertrial() {
			InterruptIntertrial();
			StartCoroutine(Intertrial(true));
		}
		
		void InterruptIntertrial() {
			StopAllCoroutines();
			if (intertrial) {
				intertrial = false;
				transform.position = new Vector3(transform.position.x, transform.position.y, startPosition);
				sender.Send("intertrial,false");
				logger.Log("intertrial,false");
				intertrialImage.enabled = false;
			}
		}
		
		IEnumerator Intertrial(bool state) {
			if (state) {
				intertrial = true;
				sender.Send("intertrial,true");
				yield return new WaitForEndOfFrame();
				intertrialImage.enabled = true;
				logger.Log("intertrial,true");
				yield return new WaitForSeconds(intertrialDuration);
				StartCoroutine(Intertrial(false));
			} else {
				sender.Send("intertrial,false");
				yield return new WaitForEndOfFrame();
				transform.position = new Vector3(transform.position.x, transform.position.y, startPosition);
				intertrialImage.enabled = false;
				logger.Log("intertrial,false");
				intertrial = false;
			}
		}
		
		float ViewAngle {
			get {
				return viewAngle;
			}
			
			set {
				viewAngle = value;
				viewAngleText.text = viewAngle.ToString();
				transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, viewAngle, transform.localEulerAngles.z);
			}
		}
		
		DeviceModes DeviceMode {
			get {
				return deviceMode;
			}
			
			set {
				deviceMode = value;
				deviceModeText.text = deviceMode == DeviceModes.Control ? "Control" : "Monitor";
				if (environment == null)
					environment =  Instantiate(Resources.Load("classroom")) as GameObject;
			}
		}
		
		void Reward() {
			arduino.Write(0);
			logger.Log("reward");
		}
		
		void Log(string message) {
			consoleText.text = string.Format("[{0:0000.00}] {1}\n{2}", Time.realtimeSinceStartup, message, consoleText.text);
		}
		
		void SetupCombo(Button button, InputField inputField, string previousValue, TestHandler testHandler, Action onSet = null, Action onTriggered = null) {
			bool trigger = onTriggered != null;
			bool autoRevert = previousValue != null;
			if (autoRevert)
				inputField.text = previousValue;
			
			Text buttonText = button.GetComponentInChildren<Text>();
			buttonText.text = trigger ? "Trigger" : "Set";
			
			inputField.onValidateInput += 
				delegate(string input, int charIndex, char addedChar) {
					buttonText.text = "Set";
					trigger = false;
					return addedChar;
				};
			
			button.onClick.AddListener(
				delegate {
					if (trigger) {
						if (onTriggered != null)
							onTriggered();
					} else {
						if (testHandler(inputField.text)) {
							previousValue = inputField.text;
							if (onTriggered != null) {
								trigger = true;
								buttonText.text = "Trigger";
							}
							if (onSet != null)
								onSet();
						} else {
							if (autoRevert)
								inputField.text = previousValue;
							buttonText.text = "Set";
						}
					}
				}
			);
		}
		
		void OnDataReceivedFromNetwork(string message) {
			bool success;
			string[] parts;
			try {
				parts = message.Split(',');
				success = parts.Length == 2;
			} catch {
				parts = null;
				Debug.LogFormat("'{0}' is unexpected.", message);
				success = false;
			}
			if (success) {
				string key = parts[0];
				string data = parts[1];
				switch (key) {
					case "z":
						// Position data received from the network.
						float z;
						if (float.TryParse(data, out z))
							transform.position = new Vector3(transform.position.x, transform.position.y, z);
						else
							success = false;
						break;
					case "intertrial":
						// Screen command received from the network.
						if (data == "true")
							intertrialImage.enabled = true;
						else if (data == "false")
							intertrialImage.enabled = false;
						else
							success = false;
						break;
					default:
						success = false;
						break;
				}
				if (!success)
					Debug.LogFormat("'{0}' is unexpected.", data);
			}
		}
		
		void OnDataReceivedFromArduino(sbyte change) {
			// Wheel data received from Arduino.
			wheelZ += change;
			if (!intertrial)
				transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + wheelFactor * change);
			logger.Log("wheel-position,{0}", wheelZ);
			//Debug.Log(wheelZ);
		}
	}
}