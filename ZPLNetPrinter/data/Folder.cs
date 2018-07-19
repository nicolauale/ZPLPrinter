using System;
using System.Xml.Serialization;

namespace ZPLNetPrinter
{
    [Serializable()]
    public class Folder
    {
        [System.Xml.Serialization.XmlElement("Name")]
        public string Name { get; set; }

        [System.Xml.Serialization.XmlElement("Printer")]
        public string Printer { get; set; }

        [System.Xml.Serialization.XmlElement("Port")]
        public string Port { get; set; }

        [System.Xml.Serialization.XmlElement("Method")]
        public string Method { get; set; }

    }

    [Serializable()]
    [System.Xml.Serialization.XmlRoot("FolderCollection")]
    public class FolderCollection
    {
        [XmlArray("Folders")]
        [XmlArrayItem("Folder", typeof(Folder))]
        public Folder[] Folder { get; set; }

    }

}
