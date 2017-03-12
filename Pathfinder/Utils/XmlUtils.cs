using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Xaml.Media.Animation;

namespace Pathfinder.Utils
{
    public class XmlUtils {
        public static Dictionary<String, String> GetAttributeMap(IXmlNode xmlNode)
        {
            var attributes = xmlNode.Attributes;
            Dictionary<String, String> attributeMap = attributes.ToDictionary(attribute => attribute.NodeName, attribute => attribute.InnerText);
            return attributeMap;
        }

        public static Dictionary<String, String> GetNodeValueMap(IXmlNode xmlNode)
        {
            var childNodes = xmlNode.ChildNodes;
            Dictionary<String, String> nodeValueMap = childNodes.ToDictionary(node => node.LocalName.ToString(), node => node.NodeValue.ToString());
            return nodeValueMap;
        } 
    }
}