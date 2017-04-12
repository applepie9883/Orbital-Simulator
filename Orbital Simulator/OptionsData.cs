using Microsoft.Xna.Framework;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Orbital_Simulator
{
    public class OptionsData
    {
        public int OrbiterCount { get; set; }

        public int OrbiterHeight { get; set; }

        public int OrbiterWidth { get; set; }

        public bool BuddySystem { get; set; }

        public bool OrbiterOrbiter { get; set; }

        [XmlIgnore]
        public Color OrbiterColor { get; set; }

        [XmlElement("OrbiterColor")]
        public string OrbiterColorString
        {
            get
            {
                return OrbiterColor.ToString();
            }
            set
            {
                Color theColor = new Color();

                // Regex to find all matches of any single letter ("\w") followed by a colon (":") followed by from 0 to 3 digits ("\d{0,4}")
                Regex colorExpression = new Regex(@"\w:\d{1,4}");
                MatchCollection matches = colorExpression.Matches(value);

                foreach(Match match in matches)
                {
                    string matchValue = match.Value;

                    short valueShort = short.Parse(matchValue.Substring(matchValue.IndexOf(":") + 1));

                    if (valueShort > byte.MaxValue)
                    {
                        valueShort = byte.MaxValue;
                    }
                    else if (valueShort < byte.MinValue)
                    {
                        valueShort = byte.MinValue;
                    }

                    byte colorValue = (byte)valueShort;

                    if (matchValue.Contains("R"))
                    {
                        theColor.R = colorValue;
                    }
                    else if (matchValue.Contains("G"))
                    {
                        theColor.G = colorValue;
                    }
                    else if (matchValue.Contains("B"))
                    {
                        theColor.B = colorValue;
                    }
                    else if (matchValue.Contains("A"))
                    {
                        theColor.A = colorValue;
                    }
                }

                OrbiterColor = theColor;
            }
        }

        private OptionsData() { }

        public OptionsData(int orbiterCount, int orbiterHeight, int orbiterWidth, bool buddySystem, Color orbiterColor, bool orbitEachOther)
        {
            OrbiterCount = orbiterCount;
            OrbiterHeight = orbiterHeight;
            OrbiterWidth = orbiterWidth;
            BuddySystem = buddySystem;
            OrbiterColor = orbiterColor;
            OrbiterOrbiter = orbitEachOther;
        }

        public static OptionsData LoadDataFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            OptionsData data;

            using (StreamReader reader = new StreamReader(filePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(OptionsData));
                data = (OptionsData)serializer.Deserialize(reader);
            }

            return data;
        }

        public void WriteDataToFile(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                XmlSerializer serializer = new XmlSerializer(GetType());
                serializer.Serialize(writer, this);
            }
        }
    }
}
