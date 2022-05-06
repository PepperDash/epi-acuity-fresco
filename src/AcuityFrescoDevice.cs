using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;


namespace PepperDashPluginAcuityFresco
{
    /// <summary>
    /// Plugin device
    /// </summary>
    public class AcuityFrescoDevice : EssentialsBridgeableDevice
    {
        private const string CommsDelimiter = "\n";
        private readonly IBasicCommunication _comms;
        private readonly GenericCommunicationMonitor _commsMonitor;

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

            Scenes = config.Scenes;

            SceneSelectDirectFeebacks = new Dictionary<int, BoolFeedback>();
            SceneSelectFeedback = new IntFeedback(() => (int)_activeScene);

            InitializeSceneSelectDirectFeedback(Scenes);

            _comms = comms;
            _commsMonitor = new GenericCommunicationMonitor(this, _comms, config.PollTimeMs, config.WarningTimeoutMs, config.ErrorTimeoutMs, Poll);
            _commsMonitor.StatusChange += OnCommunicationMonitorStatusChange;
            //DeviceManager.AddDevice(_commsMonitor);

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
        /// Use the custom activiate to connect the device and start the comms monitor.
        /// This method will be called when the device is built.
        /// </summary>
        /// <returns></returns>
        public override bool CustomActivate()
        {
            // Essentials will handle the connect method to the device                       
            _comms.Connect();
            // Essentialss will handle starting the comms monitor
            _commsMonitor.Start();

            return base.CustomActivate();
        }

        /// <summary>
        /// Initialize plugin device
        /// </summary>
        //public override void Initialize()
        //{
        //    // Essentials will handle the connect method to the device                       
        //    _comms.Connect();
        //    _commsMonitor.StatusChange +=
        //        (sender, args) => Debug.Console(DebugLevel, this, "Communication monitor state: {0}", _commsMonitor.Status);
        //    // Essentialss will handle starting the comms monitor
        //    _commsMonitor.Start();
        //}

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
                var sceneSelectJoin = (uint)(joinMap.SceneSelectDirect.JoinNumber + sceneIndex);
                var sceneVisibleJoin = (uint)(joinMap.SceneButtonVisibility.JoinNumber + sceneIndex);
                var name = scene.Name;

                trilist.SetString(sceneSelectJoin, string.IsNullOrEmpty(name) ? string.Empty : name);
                trilist.SetBool(sceneVisibleJoin, string.IsNullOrEmpty(name));

                trilist.SetSigTrueAction(sceneSelectJoin, () => SelectScene(sceneIndex));
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

            // TODO [ ] Process device response
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
        /// 'scene {Scene ID, 1-36} {Level, 0-100%} [0 {room ID, A-X}]'
        /// '{}' are required params
        /// '[]' are optional params
        /// </remarks>
        public void SelectScene(int index)
        {
            if (index < 0 || index > Scenes.Count) return;

            Debug.Console(VerboseLevel, this, "SelectScene: index-'{0}'", index);

            var scene = Scenes[index];
            if (scene == null)
            {
                Debug.Console(DebugLevel, this, "SelectScene: invalid scene index-'{0}'", index);
                return;
            }

            var cmd = (string.IsNullOrEmpty(scene.RoomId))
                ? string.Format("scene {0} {1}", scene.Id, scene.Level)
                : string.Format("scene {0} {1} {2}", scene.Id, scene.Level, scene.RoomId);

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
        public uint TraceLevel = 0;

        /// <summary>
        /// Debug level (1)
        /// </summary>
        public uint DebugLevel = 1;

        /// <summary>
        /// Verbose Level (2)
        /// </summary>        
        public uint VerboseLevel = 2;

        private CTimer _debugTimer;
        private bool _debugTimerActive;

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

            if (_debugTimerActive)
                _debugTimer.Stop();

            if (!_debugTimer.Disposed)
                _debugTimer.Dispose();

            _debugTimerActive = _debugTimer != null;
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

            if (_debugTimer == null)
                _debugTimer = new CTimer(dt => ResetDebugLevels(), 900000); // 900,000 = 15-mins
            else
                _debugTimer.Reset();

            _debugTimerActive = _debugTimer != null;
        }

        #endregion
    }
}

