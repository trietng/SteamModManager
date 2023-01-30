using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

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
            public IniProperty(string key, string value)
            { 
                Key = key;
                Value = value;
                Comment = string.Empty;
            }
            public IniProperty(string key, string value, string comment)
            {
                Key = key;
                Value = value;
                Comment = comment;
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
            public IniSection(string name)
            {
                Name = name;
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
    namespace Compression
    {
        public static class ZipArchiveExtensions
        {
            public static void CreateEntryFromAny(this ZipArchive archive, string sourceName, string entryName, CompressionLevel compressionLevel = CompressionLevel.Optimal)
            {
                var fileName = Path.GetFileName(sourceName);
                if (File.GetAttributes(sourceName).HasFlag(FileAttributes.Directory))
                {
                    archive.CreateEntryFromDirectory(sourceName, Path.Combine(entryName, fileName), compressionLevel);
                }
                else
                {
                    var x = archive.CreateEntryFromFile(sourceName, Path.Combine(entryName, fileName), compressionLevel);
                }
            }
            /// <summary>
            /// <p>Adds a directory from the file system to the archive under the specified entry name.</p>
            /// </summary>
            /// <exception cref="ArgumentException">sourceFileName is a zero-length string, contains only whitespace, or contains one or more
            /// invalid characters as defined by InvalidPathChars. -or- entryName is a zero-length string.</exception>
            /// <exception cref="ArgumentNullException">sourceFileName or entryName is null.</exception>
            /// <exception cref="PathTooLongException">In sourceFileName, the specified path, file name, or both exceed the system-defined maximum length.
            /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
            /// <exception cref="DirectoryNotFoundException">The specified sourceFileName is invalid, (for example, it is on an unmapped drive).</exception>
            /// <exception cref="IOException">An I/O error occurred while opening the file specified by sourceFileName.</exception>
            /// <exception cref="UnauthorizedAccessException">sourceFileName specified a directory.
            /// -or- The caller does not have the required permission.</exception>
            /// <exception cref="FileNotFoundException">The file specified in sourceFileName was not found. </exception>
            /// <exception cref="NotSupportedException">sourceFileName is in an invalid format or the ZipArchive does not support writing.</exception>
            /// <exception cref="ObjectDisposedException">The ZipArchive has already been closed.</exception>
            ///
            /// <param name="destination">The zip archive to add the file to.</param>
            /// <param name="sourceFileName">The path to the file on the file system to be copied from. The path is permitted to specify relative
            /// or absolute path information. Relative path information is interpreted as relative to the current working directory.</param>
            /// <param name="entryName">The name of the entry to be created.</param>
            /// <param name="compressionLevel">The level of the compression (speed/memory vs. compressed size trade-off).</param>
            /// <returns>A wrapper for the newly created entry.</returns>
            public static void CreateEntryFromDirectory(this ZipArchive archive, string sourceDirectoryName, string entryName, CompressionLevel compressionLevel = CompressionLevel.Optimal)
            {
                string[] entries = Directory.GetFileSystemEntries(sourceDirectoryName);
                foreach (var entry in entries)
                {
                    archive.CreateEntryFromAny(entry, entryName, compressionLevel);
                }
            }
        }
    }
}
