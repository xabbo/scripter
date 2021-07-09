using System;

namespace b7.Scripter.Model
{
    public record ScriptModel
    {
        public int DocumentId { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }

        public ScriptModel()
        {
            Path =
            Name =
            Group = string.Empty;
        }
    }
}
