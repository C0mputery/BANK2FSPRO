using System.Xml.Linq;

namespace BANK2FSPRO;

internal static class XmlBuilder {
    public static XDocument CreateDocument(params object[] content) {
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("objects",
                new XAttribute("serializationModel", "Studio.02.02.00"),
                content
            )
        );
    }

    public static XElement Object(string className, Guid id, params object[] content) {
        return new XElement("object",
            new XAttribute("class", className),
            new XAttribute("id", id.AsFmodStringFormat()),
            content
        );
    }

    public static XElement Property(string name, object value) {
        return new XElement("property",
            new XAttribute("name", name),
            new XElement("value", value)
        );
    }

    public static XElement MultiProperty(string name, IEnumerable<string> values) {
        return new XElement("property",
            new XAttribute("name", name),
            values.Select(v => new XElement("value", v))
        );
    }

    public static XElement Relationship(string name, Guid destination) {
        return new XElement("relationship",
            new XAttribute("name", name),
            new XElement("destination", destination.AsFmodStringFormat())
        );
    }

    public static XElement Relationship(string name, IEnumerable<Guid> destinations) {
        return new XElement("relationship",
            new XAttribute("name", name),
            destinations.Select(d => new XElement("destination", d.AsFmodStringFormat()))
        );
    }

    public static void Save(XDocument document, string path) {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        document.Save(path);
    }
}
