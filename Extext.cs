using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Extext
{
    namespace Ini
    {
        public class IniProperty
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public string Comment { get; set; }
            public IniProperty()
            {
                Key = string.Empty;
                Value = string.Empty;
                Comment = string.Empty;
            }
        }
        public class IniSection : IEnumerable<IniProperty>
        {
            public string Name { get; set; }
            private readonly Dictionary<string, IniProperty> properties;
            public IniSection()
            {
                Name = string.Empty;
                properties = new();
            }
            public IniProperty this[string key]
            {
                get
                {
                    try
                    {
                        return properties[key];
                    }
                    catch (KeyNotFoundException ex)
                    {
                        throw ex;
                    }
                }
            }
            public void Add(IniProperty property)
            {
                properties.Add(property.Key, property);
            }
            public IEnumerator<IniProperty> GetEnumerator()
            {
                return properties.Values.ToList().GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
        public class IniDocument : IEnumerable<IniSection>
        {
            private readonly Dictionary<string, IniSection> sections;
            public IniDocument()
            {
                sections = new();
            }
            public IniSection this[string key]
            {
                get
                {
                    try
                    {
                        return sections[key];
                    }
                    catch (KeyNotFoundException ex)
                    {
                        throw ex;
                    }
                }
            }
            public void Add(IniSection section)
            {
                sections.Add(section.Name, section);
            }
            public IEnumerator<IniSection> GetEnumerator()
            {
                return sections.Values.ToList().GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
        public static class IniSerializer
        {
            public static string Serialize(IniDocument document)
            {
                StringBuilder builder = new();
                foreach (var section in document)
                {
                    builder.AppendLine($"[{section.Name}]");
                    foreach (var property in section)
                    {
                        builder.Append($"{property.Key} = {property.Value}");
                        if (property.Comment.Length > 0)
                        {
                            builder.Append($" ;{property.Comment}");
                        }
                        builder.AppendLine();
                    }
                    builder.AppendLine();
                }
                builder.Remove(builder.Length - 3, 2);
                return builder.ToString();
            }
            public static IniDocument Deserialize(string data)
            {
                IniDocument document = new();
                using (var reader = new StringReader(data))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.Length == 0)
                        {
                            continue;
                        }
                        if (line.StartsWith(';'))
                        {
                            continue;
                        }
                        if (line.Contains('='))
                        {
                            throw new InvalidDataException();
                        }
                        int indexRightSquareBracket = 0;
                        if (line.StartsWith('[') && ((indexRightSquareBracket = line.IndexOf(']') - 1) != -1))
                        {
                            IniSection section = new()
                            {
                                Name = line.Substring(1, indexRightSquareBracket)
                            };
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (!line.Contains('='))
                                {
                                    break;
                                }
                                var lineSections = line.Split(';', 2);
                                var propertyFields = lineSections[0].Split('=', 2, StringSplitOptions.TrimEntries);
                                IniProperty property = new()
                                {
                                    Key = propertyFields[0],
                                    Value = propertyFields[1],
                                    Comment = (lineSections.Length < 2) ? string.Empty : lineSections[1]
                                };
                                //if (property.Value.StartsWith('\"') && property.Value.EndsWith('\"'))
                                //{
                                //    property.Value.Substring(1, property.Value.Length - 1);
                                //}
                                section.Add(property);
                            }
                            document.Add(section);
                        }
                    }
                }
                return document;
            }
        }
    }
}
