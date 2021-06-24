﻿using System.Text.Json.Serialization;

namespace Snyk.Code.Library.SnykCode
{
    /// <summary>
    /// Data transfer object for file content pair: file path hash - file content.
    /// </summary>
    public class CodeFile
    {
        /// <summary>
        /// Gets or sets a value indicating whether file path hash.
        /// </summary>
        [JsonPropertyName("fileHash")]
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether file content.
        /// </summary>
        [JsonPropertyName("fileContent")]
        public string Content { get; set; }
    }
}
