using PepperDash.Essentials.Core;

namespace PepperDashPluginAcuityFresco
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
    public class AcuityFrescoBridgeJoinMap : JoinMapBaseAdvanced
	{
        /*
        GenericLightingJoinMap
        
         [JoinName("IsOnline")] 
         * JoinNumber = 1
         * JoinSpan = 1
         * JoinCapabilities = eJoinCapabilities.ToSIMPL
         * JoinType = eJoinType.Digital

        [JoinName("SelectScene")]
         * JoinNumber = 1
         * JoinSpan = 1
         * JoinCapabilities = eJoinCapabilities.FromSIMPL
         * JoinType = eJoinType.Digital

        [JoinName("SelectSceneDirect")]
         * JoinNumber = 11
         * JoinSpan = 10
         * JoinCapabilities = eJoinCapabilities.ToFromSIMPL
         * JoinType = eJoinType.DigitalSerial

        [JoinName("ButtonVisibility")]
         * JoinNumber = 41
         * JoinSpan = 10
         * JoinCapabilities = eJoinCapabilities.ToSIMPL
         * JoinType = eJoinType.Digital

        ** NOT USED **
        [JoinName("IntegrationIdSet")]
         * JoinNumber = 1
         * JoinSpan = 1
         * JoinCapabilities = eJoinCapabilities.FromSIMPL
         * JoinType = eJoinType.Serial
        */

		#region Digital

        [JoinName("IsOnline")]
        public JoinDataComplete IsOnline = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device is online feedback",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("SceneSelectDirect")]
        public JoinDataComplete SceneSelectDirect = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 10
            },
            new JoinMetadata
            {
                Description = "Device direct scene select, feedback, and names",                
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.DigitalSerial
            });

        [JoinName("SceneButtonVisibility")]
        public JoinDataComplete SceneButtonVisibility = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 41,
                JoinSpan = 10
            },
            new JoinMetadata
            {
                Description = "Device scene button visibility feedback",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

		#endregion


		#region Analog		
		
        [JoinName("CommunicationMonitorStatus")]
        public JoinDataComplete CommunicationMonitorStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device communication monitor status feedback",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("SocketStatus")]
        public JoinDataComplete SocketStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device socket status feedback",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("SceneSelect")]
        public JoinDataComplete SceneSelect = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 10,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device scene select by index with feedback",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

		#endregion


		#region Serial

        [JoinName("DeviceName")]
        public JoinDataComplete DeviceName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

		#endregion

		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
		/// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
		public AcuityFrescoBridgeJoinMap(uint joinStart)
			: base(joinStart, typeof(AcuityFrescoBridgeJoinMap))
		{
		}
	}
}
