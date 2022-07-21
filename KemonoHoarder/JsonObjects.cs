using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace KemonoHoarder
{
    public partial class Creator
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("indexed")]
        public string Indexed { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("service")]
        public string Service { get; set; }

        [JsonProperty("updated")]
        public string Updated { get; set; }
    }
    public partial class Post
    {
        [JsonProperty("added")]
        public string Added { get; set; }

        [JsonProperty("attachments")]
        public List<File> Attachments { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("edited")]
        public string Edited { get; set; }

        [JsonProperty("embed")]
        public Embed Embed { get; set; }

        [JsonProperty("file")]
        public File File { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("published")]
        public DateTime Published { get; set; }

        [JsonProperty("service")]
        public string Service { get; set; }

        [JsonProperty("shared_file")]
        public bool SharedFile { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("user")]
        public long User { get; set; }
    }

    public partial class File
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }
    }

    public partial class Embed
    {
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("subject", NullValueHandling = NullValueHandling.Ignore)]
        public string Subject { get; set; }

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public Uri Url { get; set; }
    }
}
