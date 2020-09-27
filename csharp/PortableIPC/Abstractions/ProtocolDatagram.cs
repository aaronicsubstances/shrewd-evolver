using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PortableIPC.Abstractions
{
    public class ProtocolDatagram
    {
        public const short OpCodeOpen = 1;
        public const short OpCodeData = 2;
        public const short OpCodeAck = 3;
        public const short OpCodeClose = 4;
        public const short OpCodeError = 5;
        public const short OpCodeCloseAll = 6;

        public const int SessionIdLength = 50;

        // opCode, sessionId, sequence number, null terminator are always present.
        private const int MinDatagramSize = 2 + SessionIdLength + 2 + 1;

        private static readonly byte NullTerminator = 0;

        private const string OptionNameDataLength = "data_length";
        private const string OptionNameIdleTimeoutMillis = "idle_timeout_millis";
        private const string OptionNameErrorCode = "error_code";
        private const string OptionNameErrorMessage = "error_message";

        public short OpCode { get; set; }
        public string SessionId { get; set; }
        public short SequenceNumber { get; set; }
        public Dictionary<string, List<string>> RemainingOptions { get; set; }
        public byte[] DataBytes { get; set; }
        public int DataOffset { get; set; }
        public int DataLength { get; set; }
        public int? ExpectedDataLength { get; set; }
        public long? IdleTimeoutMillis { get; set; }
        public int? ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

        public static ProtocolDatagram Parse(byte[] rawBytes, int offset, int length)
        {
            if (length < MinDatagramSize)
            {
                throw CreateException("datagram too small to be valid");
            }

            var parsedDatagram = new ProtocolDatagram();
            int endOffset = offset + length;

            parsedDatagram.OpCode = ReadInt16BigEndian(rawBytes, offset);
            offset += 2;

            parsedDatagram.SessionId = ConvertBytesToString(rawBytes, offset, SessionIdLength);
            offset += SessionIdLength;

            parsedDatagram.SequenceNumber = ReadInt16BigEndian(rawBytes, offset);
            offset += 2;

            // Now read options until we encounter null terminator for all options, 
            // which is equivalent to empty string option name 
            string optionName = null;
            while (optionName != "")
            {
                // look for null terminator.
                int nullTerminatorIndex = -1;
                for (int i = offset; i < endOffset; i++)
                {
                    if (rawBytes[i] == NullTerminator)
                    {
                        nullTerminatorIndex = i;
                        break;
                    }
                }
                if (nullTerminatorIndex == -1)
                {
                    throw CreateException("null terminator for all options not found");
                }

                var optionNameOrValue = ConvertBytesToString(rawBytes, offset, nullTerminatorIndex);
                offset = nullTerminatorIndex + 1;

                if (optionName == null)
                {
                    optionName = optionNameOrValue;
                }
                else
                {
                    // Identify known options first. In case of repetition, first one wins.
                    bool knownOptionEncountered = true;
                    switch (optionName)
                    {
                        case OptionNameDataLength:
                            if (parsedDatagram.ExpectedDataLength == null)
                            {
                                parsedDatagram.ExpectedDataLength = ParseOptionAsInt32(optionName, optionNameOrValue);
                            }
                            break;
                        case OptionNameIdleTimeoutMillis:
                            if (parsedDatagram.IdleTimeoutMillis == null)
                            {
                                parsedDatagram.IdleTimeoutMillis = ParseOptionAsInt64(optionName, optionNameOrValue);
                            }
                            break;
                        case OptionNameErrorCode:
                            if (parsedDatagram.ErrorCode == null)
                            {
                                parsedDatagram.ErrorCode = ParseOptionAsInt32(optionName, optionNameOrValue);
                            }
                            break;
                        case OptionNameErrorMessage:
                            if (parsedDatagram.ErrorMessage == null)
                            {
                                parsedDatagram.ErrorMessage = optionNameOrValue;
                            }
                            break;
                        default:
                            knownOptionEncountered = false;
                            break;
                    }
                    if (!knownOptionEncountered)
                    {
                        if (parsedDatagram.RemainingOptions == null)
                        {
                            parsedDatagram.RemainingOptions = new Dictionary<string, List<string>>();
                        }
                        List<string> optionValues;
                        if (parsedDatagram.RemainingOptions.ContainsKey(optionName))
                        {
                            optionValues = parsedDatagram.RemainingOptions[optionName];
                        }
                        else
                        {
                            optionValues = new List<string>();
                            parsedDatagram.RemainingOptions.Add(optionName, optionValues);
                        }
                        optionValues.Add(optionNameOrValue);
                    }
                    optionName = null;
                }
            }

            parsedDatagram.DataBytes = rawBytes;
            parsedDatagram.DataOffset = offset;
            parsedDatagram.DataLength = endOffset - offset;
            
            // Validate

            // Use data_length option to detect truncation of datagrams by network.
            if (parsedDatagram.ExpectedDataLength != null)
            {
                if (parsedDatagram.DataLength != parsedDatagram.ExpectedDataLength.Value)
                {
                    throw CreateException($"data length check error! Expected {parsedDatagram.ExpectedDataLength} " +
                        $"bytes but received {parsedDatagram.DataLength}");
                }
            }
            // Don't validate op code, to allow for extensions to protocol.
            // Instead let SessionHandlers handle that.
            
            return parsedDatagram;
        }

        public byte[] ToRawDatagram()
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    WriteInt16BigEndian(writer, OpCode);
                    byte[] sessionId = ConvertStringToBytes(SessionId);
                    if (sessionId.Length != SessionIdLength)
                    {
                        throw CreateException($"Received invalid session id: {SessionId} produces {sessionId.Length} bytes");
                    }
                    writer.Write(sessionId);
                    WriteInt16BigEndian(writer, SequenceNumber);

                    // write out all options, starting with known ones.
                    var knownOptions = new Dictionary<string, string>();
                    if (ExpectedDataLength != null)
                    {
                        knownOptions.Add(OptionNameDataLength, ExpectedDataLength.ToString());
                    }
                    if (IdleTimeoutMillis != null)
                    {
                        knownOptions.Add(OptionNameIdleTimeoutMillis, IdleTimeoutMillis.ToString());
                    }
                    if (ErrorCode != null)
                    {
                        knownOptions.Add(OptionNameErrorCode, ErrorCode.ToString());
                    }
                    if (ErrorMessage != null)
                    {
                        knownOptions.Add(OptionNameErrorMessage, ErrorMessage);
                    }
                    foreach (var kvp in knownOptions)
                    {
                        var optionNameBytes = ConvertStringToBytes(kvp.Key);
                        writer.Write(optionNameBytes);
                        writer.Write(NullTerminator);
                        var optionValueBytes = ConvertStringToBytes(kvp.Value);
                        writer.Write(optionValueBytes);
                        writer.Write(NullTerminator);
                    }
                    if (RemainingOptions != null)
                    {
                        foreach (var kvp in RemainingOptions)
                        {
                            var optionNameBytes = ConvertStringToBytes(kvp.Key);
                            foreach (var optionValue in kvp.Value)
                            {
                                writer.Write(optionNameBytes);
                                writer.Write(NullTerminator);
                                var optionValueBytes = ConvertStringToBytes(optionValue);
                                writer.Write(optionValueBytes);
                                writer.Write(NullTerminator);
                            }
                        }
                    }

                    writer.Write(NullTerminator);
                    if (DataBytes != null)
                    {
                        writer.Write(DataBytes, DataOffset, DataLength);
                    }
                }
                return ms.ToArray();
            }
        }

        internal static Exception CreateException(string message)
        {
            return new Exception(message);
        }

        internal static int ParseOptionAsInt32(string optionName, string optionValue)
        {
            if (int.TryParse(optionValue, out int intVal))
            {
                return intVal;
            }
            throw CreateException($"Received invalid value for option {optionName}: {optionValue}");
        }

        internal static long ParseOptionAsInt64(string optionName, string optionValue)
        {
            if (long.TryParse(optionValue, out long intVal))
            {
                return intVal;
            }
            throw CreateException($"Received invalid value for option {optionName}: {optionValue}");
        }

        internal static byte[] ConvertStringToBytes(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        internal static string ConvertBytesToString(byte[] data, int offset, int length)
        {
            return Encoding.UTF8.GetString(data, offset, length);
        }

        internal static void WriteInt16BigEndian(BinaryWriter writer, short v)
        {
            writer.Write((byte)(0xff & (v >> 8)));
            writer.Write((byte)(0xff & v));
        }

        internal static void WriteInt32BigEndian(BinaryWriter writer, int v)
        {
            writer.Write((byte)(0xff & (v >> 24)));
            writer.Write((byte)(0xff & (v >> 16)));
            writer.Write((byte)(0xff & (v >> 8)));
            writer.Write((byte)(0xff & v));
        }

        internal static short ReadInt16BigEndian(byte[] rawBytes, int offset)
        {
            byte a = rawBytes[offset];
            byte b = rawBytes[offset + 1];
            int v = (a << 8) | (b & 0xff);
            return (short)v;
        }

        internal static int ReadInt32BigEndian(byte[] rawBytes, int offset)
        {
            byte a = rawBytes[offset];
            byte b = rawBytes[offset + 1];
            byte c = rawBytes[offset + 2];
            byte d = rawBytes[offset + 3];
            int v = ((a & 0xff) << 24) | ((b & 0xff) << 16) |
                ((c & 0xff) << 8) | (d & 0xff);
            return v;
        }
    }
}
