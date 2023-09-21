﻿using MetaFrm.Service;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MetaFrm.ApiServer.RabbitMQ
{
    /// <summary>
    /// RabbitMQData
    /// </summary>
    [Serializable()]
    [DataContract(Namespace = "https://www.metafrm.net/")]
    public class RabbitMQData
    {
        /// <summary>
        /// DateTime
        /// </summary>
        [DataMember()]
        [JsonInclude]
        public DateTime DateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// ServiceData
        /// </summary>
        [DataMember()]
        [JsonInclude]
        public ServiceData? ServiceData { get; set; }

        /// <summary>
        /// Response
        /// </summary>
        [DataMember()]
        [JsonInclude]
        public Response? Response { get; set; }
    }
}