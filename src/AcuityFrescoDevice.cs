using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Queues;


namespace PepperDashPluginAcuityFresco
{
	/// <summary>
	/// Plugin device
	/// </summary>
	public class AcuityFrescoDevice : EssentialsBridgeableDevice
	{
		private const string CommsDelimiter = "\r";
		private readonly IBasicCommunication _comms;
		private readonly GenericCommunicationMonitor _commsMonitor;
		private GenericQueue _commsRxQueue;

		/// <summary>
		/// Communication status monitor
		/// </summary>
		public StatusMonitorBase CommunicationMonitor { get { return _commsMonitor; } }

		/// <summary>
		/// Online feedback
		/// </summary>
		public BoolFeedback OnlineFeedback { get; private set; }

		/// <summary>
		/// Communication monitor feedback
		/// </summary>
		public IntFeedback CommunicationMonitorFeedback { get; private set; }

		/// <summary>
		/// Socket status feedback
		/// </summary>
		public IntFeedback SocketStatusFeedback { get; private set; }

		/// <summary>
		/// List of configured scenes
		/// </summary>
		public readonly List<AcuityFrescoScene> Scenes;

		private uint _activeScene;

		/// <summary>
		/// Stores the active scene index
		/// </summary>
		public uint ActiveScene
		{
			get { return _activeScene; }
			private set
			{
				if (_activeScene == value) return;
				_activeScene = value;

				SceneSelectFeedback.FireUpdate();

				var index = 0;
				foreach (var scene in Scenes)
				{
					scene.IsActive = (index == _activeScene);
					SceneSelectDirectFeebacks[index].FireUpdate();
					index++;
				}
			}
		}

		/// <summary>
		/// Scene select direct feedback
		/// </summary>
		public Dictionary<int, BoolFeedback> SceneSelectDirectFeebacks { get; private set; }

		/// <summary>
		/// Scene select index feedback
		/// </summary>
		public IntFeedback SceneSelectFeedback { get; private set; }


		/// <summary>
		/// Plugin device constructor
		/// </summary>
		/// <param name="key">device key</param>
		/// <param name="name">device name</param>
		/// <param name="config">device configuration object</param>
		/// <param name="comms">device communication as IBasicCommunication</param>
		/// <see cref="PepperDash.Core.IBasicCommunication"/>
		/// <seealso cref="Crestron.SimplSharp.CrestronSockets.SocketStatus"/>
		public AcuityFrescoDevice(string key, string name, AcuityFrescoPropertiesConfig config, IBasicCommunication comms)
			: base(key, name)
		{
			Debug.Console(TraceLevel, this, "Constructing new {0} instance", name);

			ResetDebugLevels();

			Scenes = config.Scenes;

			SceneSelectDirectFeebacks = new Dictionary<int, BoolFeedback>();
			SceneSelectFeedback = new IntFeedback(() => (int)_activeScene);

			InitializeSceneSelectDirectFeedback(Scenes);

			_comms = comms;
			_commsMonitor = new GenericCommunicationMonitor(this, _comms, config.PollTimeMs, config.WarningTimeoutMs, config.ErrorTimeoutMs, Poll);
			_commsMonitor.StatusChange += OnCommunicationMonitorStatusChange;
			_commsRxQueue = new GenericQueue(key + "-queue");

			//DeviceManager.AllDevicesActivated += (sender, args) => Debug.Console(VerboseLevel, this, "All devices activated");
			
			OnlineFeedback = _commsMonitor.IsOnlineFeedback;
			CommunicationMonitorFeedback = new IntFeedback(() => (int)_commsMonitor.Status);

			var commsGather = new CommunicationGather(_comms, CommsDelimiter);
			commsGather.LineReceived += Handle_LineRecieved;

			var socket = _comms as ISocketStatus;
			if (socket != null)
			{
				socket.ConnectionChange += OnSocketConnectionChange;
				SocketStatusFeedback = new IntFeedback(() => (int)socket.ClientStatus);
			}

			Debug.Console(TraceLevel, this, "Constructing new {0} instance complete", name);
			Debug.Console(TraceLevel, new string('*', 80));
			Debug.Console(TraceLevel, new string('*', 80));
		}		

		/// <summary>
		/// Initialize plugin device
		/// </summary>
		public override void Initialize()
		{
			Debug.Console(VerboseLevel, this, "Initializing {0}", Key);
			_comms.Connect();
			_commsMonitor.Start();
		}

		private void OnCommunicationMonitorStatusChange(object sender, MonitorStatusChangeEventArgs args)
		{
			Debug.Console(DebugLevel, this, "Communication Status: ({0}) {1}, {2}", args.Status, args.Status.ToString(), args.Message);
		}

		private void OnSocketConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
		{
			Debug.Console(DebugLevel, this, "Socket Status: ({0}) {1}", args.Client.ClientStatus, args.Client.ClientStatus.ToString());

			UpdateFeedbacks();

			//if (!args.Client.IsConnected) return;
		}

		#region Overrides of EssentialsBridgeableDevice

		/// <summary>
		/// Links the plugin device to the EISC bridge
		/// </summary>
		/// <param name="trilist"></param>
		/// <param name="joinStart"></param>
		/// <param name="joinMapKey"></param>
		/// <param name="bridge"></param>
		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			var joinMap = new AcuityFrescoBridgeJoinMap(joinStart);

			// This adds the join map to the collection on the bridge
			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

			Debug.Console(TraceLevel, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.Console(TraceLevel, "Linking to Bridge Type {0}", GetType().Name);

			// link joins to bridge
			trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

			OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
			CommunicationMonitorFeedback.LinkInputSig(trilist.UShortInput[joinMap.CommunicationMonitorStatus.JoinNumber]);
			if (SocketStatusFeedback != null)
				SocketStatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.SocketStatus.JoinNumber]);

			trilist.SetUShortSigAction(joinMap.SceneSelect.JoinNumber, index => SelectScene(index));
			SceneSelectFeedback.LinkInputSig(trilist.UShortInput[joinMap.SceneSelect.JoinNumber]);

			var sceneIndex = 0;
			foreach (var scene in Scenes)
			{
				var index = sceneIndex;
				var sceneSelectJoin = (uint)(joinMap.SceneSelectDirect.JoinNumber + index);
				var sceneVisibleJoin = (uint)(joinMap.SceneButtonVisibility.JoinNumber + index);
				var name = scene.Name;

				trilist.SetString(sceneSelectJoin, string.IsNullOrEmpty(name) ? string.Empty : name);
				trilist.SetBool(sceneVisibleJoin, string.IsNullOrEmpty(name) == false);

				trilist.SetSigTrueAction(sceneSelectJoin, () => SelectScene(index));
				SceneSelectDirectFeebacks[sceneIndex].LinkInputSig(trilist.BooleanInput[sceneSelectJoin]);

				sceneIndex++;
			}

			UpdateFeedbacks();

			trilist.OnlineStatusChange += (device, args) =>
			{
				if (!args.DeviceOnLine) return;

				trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

				UpdateFeedbacks();
			};
		}

		#endregion Overrides of EssentialsBridgeableDevice

		private void UpdateFeedbacks()
		{
			OnlineFeedback.FireUpdate();
			CommunicationMonitorFeedback.FireUpdate();
			if (SocketStatusFeedback != null)
				SocketStatusFeedback.FireUpdate();

			if (SceneSelectFeedback != null)
				SceneSelectFeedback.FireUpdate();

			if (SceneSelectDirectFeebacks == null) return;

			foreach (var item in SceneSelectDirectFeebacks)
			{
				Debug.Console(VerboseLevel, this, "UpdateFeedbacks SceneSelectDirectFeedbacks-'{0}' Value: {1}", item.Key, item.Value.BoolValue);
				item.Value.FireUpdate();
			}
		}

		// commonly used with ASCII based API's with a defined delimiter				
		private void Handle_LineRecieved(object sender, GenericCommMethodReceiveTextArgs args)
		{
			if (args == null || string.IsNullOrEmpty(args.Text))
			{
				Debug.Console(DebugLevel, this, "Handle_LineReceived args is null or args.Text is null or empty");
				return;
			}

			Debug.Console(DebugLevel, this, "Handle_LineReceived args.Text: {0}", args.Text);

			try
			{
				Debug.Console(DebugLevel, this, "Handle_LineReceived args.Text: {0}", args.Text);
				_commsRxQueue.Enqueue(new ProcessStringMessage(args.Text, ProcessResponse));
			}
			catch(Exception ex) 
			{
				Debug.Console(DebugLevel, this, Debug.ErrorLogLevel.Error, "HandleLineReceived Exception Message: {0}", ex.Message);
				Debug.Console(VerboseLevel, this, Debug.ErrorLogLevel.Error, "HandleLineRecieved Exception Stack Trace: {0}", ex.StackTrace);
				if (ex.InnerException != null) Debug.Console(DebugLevel, this, Debug.ErrorLogLevel.Error, "HandleLineReceived Inner Exception: '{0}'", ex.InnerException);
			}
		}

		// example response
		// 'scene {sceneId} {level} 0 {roomId}
		// 'scene 1 100 0 b'
		// 'scene 2 100 0 ab'
		// 'scene 0 100 0 abc'
		private void ProcessResponse(string response)
		{
			if (string.IsNullOrEmpty(response))
			{
				Debug.Console(VerboseLevel, this, "ProcessResponse: response '{0}' is null or empty", response);
				return;
			}

			var expression = new Regex(@"scene (?<sceneId>.*) (?<level>.*) 0 (?<roomId>.*)", RegexOptions.None);
			var matches = expression.Match(response);
			if (!matches.Success)
			{
				Debug.Console(DebugLevel, this, "ProcessResponse: response '{0}', regex match failed", response.TrimEnd('\r'));
				return;
			}

			var responseSceneId = matches.Groups["sceneId"].Value.ToLower();
			var responseLevel = matches.Groups["level"].Value;
			var responseRoomId = matches.Groups["roomId"].Value.ToLower();			

			Debug.Console(VerboseLevel, this, "ProcessResponse: responseSceneId-'{0}', responseLevel-'{1}' responseRoomId-'{2}'", 
				responseSceneId, responseLevel, responseRoomId);

			// TODO [ ] Add feedback logic 

		}

		/// <summary>
		/// Sends text to the device plugin comms
		/// </summary>
		/// <remarks>
		/// 'scene {Scene ID} {Level} [0 {room ID}]'
		/// '{}' are required params
		/// '[]' are optional params
		/// </remarks>		
		public void SendText(string text)
		{
			if (_comms.IsConnected == false)
			{
				Debug.Console(DebugLevel, this, Debug.ErrorLogLevel.Warning, "SendText: device comms not connected, unable to send text!");
				return;
			}

			if (string.IsNullOrEmpty(text)) return;

			var cmd = string.IsNullOrEmpty(CommsDelimiter)
				? string.Format("{0}", text)
				: string.Format("{0}{1}", text, CommsDelimiter);

			_comms.SendText(cmd);
		}

		/// <summary>
		/// Polls the device scene status for all
		/// </summary>
		/// <remarks>
		/// 'status scene {scene ID, 1-36 || ALL}' [0 {room ID, A-X}]'
		/// '{}' are required params
		/// '[]' are optional params
		/// </remarks>
		public void Poll()
		{
			Poll(0, null);
		}

		/// <summary>
		/// Polls the device scene status for the scene ID
		/// </summary>
		/// <remarks>
		/// 'status scene {scene ID, 1-36 || ALL}' [0 {room ID, A-X}]'
		/// '{}' are required params
		/// '[]' are optional params
		/// </remarks>
		public void Poll(uint index)
		{
			Poll(index, null);
		}

		/// <summary>
		/// Polls the device scene status for the scene ID and room ID
		/// </summary>
		/// <remarks>
		/// 'status scene {scene ID, 1-36 || ALL}' [0 {room ID, A-X}]'
		/// '{}' are required params
		/// '[]' are optional params
		/// </remarks>
		public void Poll(uint index, string roomId)
		{
			if (index == 0 && string.IsNullOrEmpty(roomId))
			{
				SendText("status scene ALL");
			}
			else
			{
				SendText(string.IsNullOrEmpty(roomId)
				   ? string.Format("status scene {0}", index)
				   : string.Format("status scene {0} 0 {1}", index, roomId));
			}
		}

		/// <summary>
		/// Initializes scene select direct feedback
		/// </summary>
		/// <param name="scenes"></param>
		public void InitializeSceneSelectDirectFeedback(List<AcuityFrescoScene> scenes)
		{
			if (scenes == null) return;

			Debug.Console(TraceLevel, this, "InitiliazeSceneSelectDirectFeedback: {0} has {1} scenes configured", Key, scenes.Count);

			foreach (var scene in scenes)
			{
				var item = scene;
				var index = scenes.FindIndex(s => s.Id.Equals(item.Id));

				Debug.Console(VerboseLevel, this, "Scene-{0} Name: {1}, Id: {2}, RoomId: {3}, Level: {4}, IsActive: {5} ",
					index, item.Name, item.Id, item.RoomId, item.Level, item.IsActive);

				SceneSelectDirectFeebacks.Add(index, new BoolFeedback(() => item.IsActive));
			}
		}

		/// <summary>
		/// Scene select
		/// </summary>
		/// <remarks>
		/// 'scene {SceneId, 1-36} {Level, 0-100%} 0 {RoomId, A-X}'
		/// </remarks>
		/// <example>        
		/// 'scene 36 100 0 X'
		/// 'scene 2 100 0 AB'
		/// 'scene 5 50 0 ABC'
		/// 'scene 0 100 0 A' // lights off
		/// </example>
		public void SelectScene(int index)
		{
			if (index < 0 || index > Scenes.Count)
			{
				Debug.Console(DebugLevel, this, "SelectScene: index-'{0}' is out of range (valid values 0-{1})", index, Scenes.Count);
				return;
			}

			Debug.Console(VerboseLevel, this, "SelectScene: index-'{0}'", index);

			var scene = Scenes[index - 1];
			if (scene == null)
			{
				Debug.Console(DebugLevel, this, "SelectScene: invalid scene index-'{0}'", index);
				return;
			}

			if (scene.Id > 36)
			{
				Debug.Console(DebugLevel, this, "SelectScene: scene index-'{0}' sceneId-'{1}' is out of range (valid values 1-36)", index, scene.Id);
				return;
			}

			if (scene.Level > 100)
			{
				Debug.Console(DebugLevel, this, "SelectScene: scene index-'{0}' level-'{1}' is out of range (valid values 1-100)", index, scene.Level);
				return;
			}

			if (string.IsNullOrEmpty(scene.RoomId))
			{
				Debug.Console(DebugLevel, this, "SelectScene: scene index-'{0}' roomId is null or empty", index);
				return;
			}

			var cmd = string.Format("scene {0} {1} 0 {2}", scene.Id, scene.Level, scene.RoomId);
			Debug.Console(VerboseLevel, this, "SelectScene: cmd-'{0}'", cmd);
			SendText(cmd);
		}

		/// <summary>
		/// Prints the list of scenes to console
		/// </summary>
		/// <example>
		/// devjson:1 {"deviceKey":"{deviceKey}", "methodName":"GetScenes", "params":[]}
		/// </example>
		public void GetScenes()
		{
			Debug.Console(TraceLevel, this, new string('*', 80));
			Debug.Console(TraceLevel, this, "Scene List:");
			for (var i = 0; i <= Scenes.Count; i++)
			{
				Debug.Console(TraceLevel, this, "Scene '{0}': Id-'{1}', Level-'{2}', Room Id-'{3}'", i, Scenes[i].Id, Scenes[i].Level, Scenes[i].RoomId);
			}

			Debug.Console(TraceLevel, this, new string('*', 80));
		}


		#region DebugLevels

		/// <summary>
		/// Trace level (0)
		/// </summary>
		public uint TraceLevel { get; set; }

		/// <summary>
		/// Debug level (1)
		/// </summary>
		public uint DebugLevel { get; set; }

		/// <summary>
		/// Verbose Level (2)
		/// </summary>        
		public uint VerboseLevel { get; set; }

		/// <summary>
		/// Resets debug levels for this device instancee
		/// </summary>
		/// <example>
		/// devjson:1 {"deviceKey":"{deviceKey}", "methodName":"ResetDebugLevels", "params":[]}
		/// </example>
		public void ResetDebugLevels()
		{
			TraceLevel = 0;
			DebugLevel = 1;
			VerboseLevel = 2;
		}

		/// <summary>
		/// Sets the debug levels for this device instance
		/// </summary>
		/// <example>
		/// devjson:1 {"deviceKey":"{deviceKey}", "methodName":"SetDebugLevels", "params":[{level, 0-2}]}
		/// </example>
		/// <param name="level"></param>
		public void SetDebugLevels(uint level)
		{
			TraceLevel = level;
			DebugLevel = level;
			VerboseLevel = level;
		}

		#endregion
	}
}

