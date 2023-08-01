using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using Serilog;
using Shared.Logger;

namespace RepackerRoot;

public class XmlParser
{
    private const string _channel = "channel";
    private const string _programme = "programme";
    private const string _id = "id";

    private HashSet<string>? _channels;

    public void ParseGzip(string sourceFile, string resultFile, string channelsFile)
    {
        InitChannels(channelsFile);

        using FileStream fileStream = File.OpenRead(sourceFile);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = XmlReader.Create(gzipStream, GetReaderSettings());
        using var writer = XmlWriter.Create(resultFile, GetWriterSettings());

        Parse(reader, writer);
    }

    public void Parse(string sourceFile, string resultFile, string channelsFile)
    {
        InitChannels(channelsFile);

        using var reader = XmlReader.Create(sourceFile, GetReaderSettings());
        using var writer = XmlWriter.Create(resultFile, GetWriterSettings());

        Parse(reader, writer);
    }

    private void InitChannels(string channelsFile)
    {
        using var reader = new StreamReader(channelsFile);

        _channels = new();

        string line;

        while ((line = reader.ReadLine()!) != null)
        {
            _channels.Add(line);
        }
    }

    private void Parse(XmlReader reader, XmlWriter writer)
    {
        try
        {
            WriteDeclaration(writer, reader);
            WriteDocType(writer, reader);
            WriteTvOpen(writer, reader);

            // TODO: if indentation problem fixed, this may become obsolete;
            // after tv tag opened, go to first element node, ignoring any data
            // may follow, because no meaningful data is expected (spaces) and
            // line feed will be inserted just before writing first appropriate
            // node (indentation "trick" used)
            if (TryMoveToFirstNode(reader))
            {
                TryWriteElement(writer, reader);

                // traversing the rest of XML
                while (reader.Read())
                {
                    if (IsTag(reader))
                    {
                        TryWriteElement(writer, reader);
                    }
                    else
                    {
                        WriteFlatData(writer, reader);
                    }
                }
            }

            writer.WriteEndDocument();
        }
        catch (Exception ex)
        {
            Log.Fatal("Failed to parse XML.");
            LogHelpers.LogMessage(ex, LogKind.Fatal);
            throw;
        }
    }

    private static bool TryMoveToFirstNode(XmlReader reader)
    {
        while (reader.Read())
        {
            if (IsTag(reader))
            {
                return true;
            }
        }

        return false;
    }

    #region General

    private static XmlReaderSettings GetReaderSettings()
    {
        return new()
        {
            DtdProcessing = DtdProcessing.Parse,
            IgnoreComments = true
        };
    }

    // TODO: fix if possible indentation - sometimes it does NOT work
    // and some formatting tricks are used to provide expected output
    private static XmlWriterSettings GetWriterSettings()
    {
        return new()
        {
            // avoid writing BOM
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            // that is taken from source XML
            OmitXmlDeclaration = true,
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace
        };
    }

    private static bool IsTag(XmlReader reader)
    {
        return reader.NodeType == XmlNodeType.Element;
    }

    private static bool IsTvNode(XmlReader reader)
    {
        return reader.Name.Equals("tv", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Writing

    private static void WriteDeclaration(XmlWriter writer, XmlReader reader)
    {
        while (reader.Read() && !TryWriteDeclaration(writer, reader))
        {
            WriteFlatData(writer, reader);
        }

        static bool TryWriteDeclaration(XmlWriter writer, XmlReader reader)
        {
            if (reader.NodeType == XmlNodeType.XmlDeclaration)
            {
                writer.WriteRaw($"<?xml {reader.Value} ?>");
                return true;
            }

            return false;
        }
    }

    private static void WriteDocType(XmlWriter writer, XmlReader reader)
    {
        while (reader.Read() && !TryWriteDocType(writer, reader))
        {
            WriteFlatData(writer, reader);
        }

        static bool TryWriteDocType(XmlWriter writer, XmlReader reader)
        {
            if (reader.NodeType == XmlNodeType.DocumentType)
            {
                writer.WriteRaw(
                    $"<!DOCTYPE {reader.Name} SYSTEM \"{reader.GetAttribute("SYSTEM")}\">");
                return true;
            }

            return false;
        }
    }

    private static void WriteTvOpen(XmlWriter writer, XmlReader reader)
    {
        while (reader.Read() && !TryWriteTvOpen(writer, reader))
        {
            WriteFlatData(writer, reader);
        }

        static bool TryWriteTvOpen(XmlWriter writer, XmlReader reader)
        {
            if (IsTag(reader) && IsTvNode(reader))
            {
                WriteOpeningNode(writer, reader);
                return true;
            }

            return false;
        }
    }

    private bool TryWriteElement(XmlWriter writer, XmlReader reader)
    {
        if (IsTargetDataTag(reader))
        {
            // TODO: if indentation problem fixed, avoid writing linefeed as it's a trick
            writer.WriteWhitespace("\n");
            writer.WriteNode(reader, defattr: false);
            return true;
        }
        else
        {
            reader.Skip();
            return false;
        }
    }

    private static void WriteOpeningNode(XmlWriter writer, XmlReader reader)
    {
        writer.WriteStartElement(
            reader.Prefix, reader.LocalName, reader.NamespaceURI);

        writer.WriteAttributes(reader, defattr: false);
    }

    private static void WriteFlatData(XmlWriter writer, XmlReader reader)
    {
        switch (reader.NodeType)
        {
            case XmlNodeType.Whitespace:

                writer.WriteWhitespace(reader.Value);
                break;

            case XmlNodeType.Text:

                writer.WriteValue(reader.Value);
                break;

            default:

                // the only case where this should be executed is closing of tv tag,
                // which is handled later, otherwise something not expected
                if (!(reader.NodeType == XmlNodeType.EndElement && IsTvNode(reader)))
                {
                    Log.Warning("Unexpected node: type '{1}', name '{0}', value '{2}'.", reader.NodeType, reader.Name, reader.Value);
                }

                break;
        }
    }

    #endregion

    #region Data tags

    // target tags (nodes): channel, programme
    private bool IsTargetDataTag(XmlReader reader)
    {
        return IsTargetProgrammeTag(reader) || IsTargetChannelTag(reader);
    }

    // node type is not checked and this can be flaw, though whole tag checking
    // is used in class basing on verifying node type is valid at the first place
    private bool IsTargetChannelTag(XmlReader reader)
    {
        return reader.Name == _channel && HasTargetAttribute(reader, _id);
    }

    private bool IsTargetProgrammeTag(XmlReader reader)
    {
        return reader.Name == _programme && HasTargetAttribute(reader, _channel);
    }

    private bool HasTargetAttribute(XmlReader reader, string attributeName)
    {
        bool result = false;

        if (reader.HasAttributes && reader.MoveToAttribute(attributeName))
        {
            if (_channels!.Contains(reader.Value))
            {
                result = true;
            }

            // Move back to the element node
            reader.MoveToElement();
        }

        return result;
    }

    #endregion
}
