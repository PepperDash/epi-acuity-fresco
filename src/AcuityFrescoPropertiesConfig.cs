using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Lighting;

namespace PepperDashPluginAcuityFresco
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	[ConfigSnippet("{\"devices\":[]}")]
	public class AcuityFrescoPropertiesConfig
	{
		/// <summary>
		/// JSON control object
		/// </summary>		
		[JsonProperty("control")]
		public EssentialsControlPropertiesConfig Control { get; set; }

		/// <summary>
		/// Serializes the poll time value
		/// </summary>
		[JsonProperty("pollTimeMs")]
		public long PollTimeMs { get; set; }

		/// <summary>
		/// Serializes the warning timeout value
		/// </summary>
		[JsonProperty("warningTimeoutMs")]
		public long WarningTimeoutMs { get; set; }

		/// <summary>
		/// Serializes the error timeout value
		/// </summary>
		[JsonProperty("errorTimeoutMs")]
		public long ErrorTimeoutMs { get; set; }

		/// <summary>
		/// Scenes
		/// </summary>
		[JsonProperty("scenes")]
        public List<AcuityFrescoScene> Scenes { get; set; }
	}

    /// <summary>
    /// 
    /// </summary>
    public class AcuityFrescoScene : LightingScene
    {
        /// <summary>
        /// Room ID used for scene recall
        /// </summary>
        [JsonProperty("roomId")]
        public string RoomId { get; set; }

        /// <summary>
        /// Optional - can be used to recall a scene with the 0-100% state
        /// </summary>
        [JsonProperty("level")]
        public uint Level { get; set; }
    }
}