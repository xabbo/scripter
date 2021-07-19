using System;

namespace b7.Scripter.Model
{
    public record ScriptModel
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }

        public ScriptModel()
        {
            FileName =
            Name =
            Group = string.Empty;
        }
    }
}
