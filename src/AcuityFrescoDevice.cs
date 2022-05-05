using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Lighting;


namespace PepperDashPluginAcuityFresco
{
	/// <summary>
	/// Plugin device
	/// </summary>
    public class AcuityFrescoDevice : LightingBase
	{
		private const string CommsDelimiter = "\n";
	    public readonly List<AcuityFrescoScene> _scenes;
		private readonly IBasicCommunication _comms;
		private readonly GenericCommunicationMonitor _commsMonitor;        

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

			_scenes = config.Scenes;

			_comms = comms;
			_commsMonitor = new GenericCommunicationMonitor(this, _comms, config.PollTimeMs, config.WarningTimeoutMs, config.ErrorTimeoutMs, Poll);						
			
			var commsGather = new CommunicationGather(_comms, CommsDelimiter);						
			commsGather.LineReceived += Handle_LineRecieved;			

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
            Debug.Console(DebugLevel, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(DebugLevel, this, "Linking to Bridge Type {0}", GetType().Name);

		    LinkLightingToApi(this, trilist, joinStart, joinMapKey, bridge);
		}		

		#endregion Overrides of EssentialsBridgeableDevice

        /// <summary>
        /// Scene select
        /// </summary>
        /// <remarks>
        /// 'scene {Scene ID, 1-36} {Level, 0-100%} [0 {room ID, A-X}]'
        /// '{}' are required params
        /// '[]' are optional params
        /// </remarks>
	    public override void SelectScene(LightingScene scene)
        {
            if (scene == null)
            {
                Debug.Console(DebugLevel, this, "scene is null");
                return;
            }

            var s = scene as AcuityFrescoScene;
            if(s == null) return;

            var cmd = (string.IsNullOrEmpty(s.RoomId))
                ? string.Format("scene {0} {1}", s.ID, s.Level)
                : string.Format("scene {0} {1} {2}", s.ID, s.Level, s.RoomId);

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
            var index = 0;
            foreach (var scene in _scenes)
            {
                Debug.Console(TraceLevel, this, "Scene '{0}': Id-'{1}', Level-'{2}', Room Id-'{3}'", index, scene.ID, scene.Level, scene.RoomId);
                index ++;
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
	    /// Error Level (2)
	    /// </summary>        
	    public uint ErrorLevel = 2;

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
            ErrorLevel = 2;

            if (_debugTimerActive)           
                _debugTimer.Stop();                
            
            if(!_debugTimer.Disposed)
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
            ErrorLevel = level;

            if(_debugTimer == null)
                _debugTimer = new CTimer(dt => ResetDebugLevels(), 900000); // 900,000 = 15-mins
            else
                _debugTimer.Reset();

            _debugTimerActive = _debugTimer != null;
	    }

	    #endregion
	}
}

