namespace WDBJsonTool.DataStructures
{
    // Represents a single record from the WDB file
    public class WDBRecord : Dictionary<string, object>
    {
        // Add any common properties or methods for a WDB record if needed
    }

    // Represents a section (e.g., header, metadata) of the WDB file
    public class WDBSection : Dictionary<string, object>
    {
        // Add any common properties or methods for a WDB section if needed
    }

    // Represents the entire parsed WDB file data
    public class WDBFile
    {
        public Dictionary<string, WDBSection> Sections { get; set; } = new Dictionary<string, WDBSection>();
        public List<WDBRecord> Records { get; set; } = new List<WDBRecord>();
        public string WDBName { get; set; }
        // Potentially other top-level metadata from the WDB file
    }
}
